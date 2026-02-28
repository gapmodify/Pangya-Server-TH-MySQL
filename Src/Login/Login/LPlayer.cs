using Connector.DataBase;
using Connector.Table;
using PangyaAPI.BinaryModels;
using PangyaAPI.PangyaClient;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Login
{
    // Helper classes for Character verification
    public class CharacterVerifyData
    {
        public string Nickname { get; set; }
        public byte Sex { get; set; }
    }

    public class LPlayer : Player
    {
        // TIS-620 encoding (Code page 874) for Thai characters
        private static readonly Encoding tis620 = Encoding.GetEncoding(874);

        public LPlayer(TcpClient tcp) : base(tcp)
        {
        }

        // Helper method to convert TIS-620 to UTF-8
        private string ConvertTIS620ToUTF8(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                // Read as Windows-1252 (default) then re-interpret as TIS-620
                byte[] inputBytes = Encoding.Default.GetBytes(input);
                string utf8String = tis620.GetString(inputBytes);

                WriteConsole.WriteLine($"[ENCODING_DEBUG] TIS-620→UTF-8: Original bytes count={inputBytes.Length}, Result='{utf8String}'", ConsoleColor.Yellow);

                return utf8String;
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[ENCODING_ERROR] Conversion failed: {ex.Message}", ConsoleColor.Red);
                return input;
            }
        }

        public void HandleRequestPacket(PangyaPacketsEnum PacketID, Packet ProcessPacket)
        {
            WriteConsole.WriteLine($"[PLAYER_REQUEST_PACKET] --> [{PacketID}, {this.GetLogin}]");
            switch (PacketID)
            {
                case PangyaPacketsEnum.PLAYER_LOGIN:
                    HandlePlayerLogin(ProcessPacket);
                    break;

                case PangyaPacketsEnum.PLAYER_SELECT_SERVER:
                    this.SendGameAuthKey();
                    break;

                case PangyaPacketsEnum.PLAYER_DUPLCATE_LOGIN:
                    this.HandleDuplicateLogin();
                    break;

                case PangyaPacketsEnum.PLAYER_SET_NICKNAME:
                    this.CreateCharacter(ProcessPacket);
                    break;

                case PangyaPacketsEnum.PLAYER_CONFIRM_NICKNAME:
                    this.NicknameCheck(ProcessPacket);
                    break;

                case PangyaPacketsEnum.PLAYER_SELECT_CHARACTER:
                    RequestCharacaterCreate(ProcessPacket);
                    break;

                case PangyaPacketsEnum.PLAYER_RECONNECT:
                    HandlePlayerReconnect(ProcessPacket);
                    break;

                case PangyaPacketsEnum.NOTHING:
                default:
                    {
                        StringBuilder sb = new StringBuilder();

                        for (int i = 0; i < ProcessPacket.GetRemainingData.Length; i++)
                        {
                            if ((i + 1) == ProcessPacket.GetRemainingData.Length)
                            {
                                sb.Append("0x" + ProcessPacket.GetRemainingData[i].ToString("X2") + "");
                            }
                            else
                            {
                                sb.Append("0x" + ProcessPacket.GetRemainingData[i].ToString("X2") + ", ");
                            }
                        }

                        WriteConsole.WriteLine("{Unknown Packet} -> " + sb.ToString(), ConsoleColor.Red);
                        Disconnect();
                    }
                    break;
            }
        }

        public bool SetAUTH_KEY_1(string Key1)
        {
            bool result;
            this.GetAuth1 = Key1;
            result = true;
            return result;
        }

        public bool SetAUTH_KEY_2(string Key2)
        {
            bool result;
            this.GetAuth2 = Key2;
            result = true;
            return result;
        }

        public bool SetLogin(string TLogin)
        {
            bool result;
            this.GetLogin = TLogin;
            result = true;
            return result;
        }

        public bool SetNickname(string TNickname)
        {
            bool result;
            this.GetNickname = TNickname;
            result = true;
            return result;
        }

        public bool SetSocket(TcpClient tcp)
        {
            bool result;
            Tcp = tcp;
            result = true;
            return result;
        }

        public bool SetUID(int TUID)
        {
            bool result;
            this.GetUID = (uint)TUID;
            result = true;
            return result;
        }

        public bool SetFirstLogin(byte First)
        {
            bool result;
            GetFirstLogin = First;
            result = true;
            return result;
        }

        private void HandleDuplicateLogin()
        {
            if (this.GetFirstLogin == 0)
            {
                Response.Clear();
                Response.Write(new byte[] { 0x0F, 0x00, 0x00 });
                Response.WritePStr(GetLogin);
                Send(Response.GetBytes());

                Response.Clear();
                Response.Write(new byte[] { 0x01, 0x00 });
                Response.WriteByte(0xD9);
                Response.WriteUInt32(uint.MaxValue);
                Send(Response.GetBytes());
                return;
            }

            if (this.GetFirstLogin == 1)
            {
                this.SendPlayerLoggedOnData();
            }
        }

        private void HandlePlayerLogin(Packet ClientPacket)
        {
            string Nickname, Auth1, Auth2;
            Byte Banned, FirstSet;
            int UID;

            WriteConsole.WriteLine("===========================================", ConsoleColor.Cyan);
            WriteConsole.WriteLine("[LOGIN_DEBUG] ▶ Starting login process...", ConsoleColor.Cyan);

            if (Program.Server.OpenServer == false)
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✗ Server is closed!", ConsoleColor.Red);
                Send(new byte[] { 0x01, 0x00, 0xE3, 0x48, 0xD2, 0x4D, 0x00 });
                Disconnect();
                return;
            }

            if (!ClientPacket.ReadPStr(out string User))
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✗ Failed to read username from packet", ConsoleColor.Red);
                return;
            }

            if (!ClientPacket.ReadPStr(out string Pwd))
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✗ Failed to read password from packet", ConsoleColor.Red);
                return;
            }

            WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Username: '{User}'", ConsoleColor.Green);
            WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Password length: {Pwd.Length} chars", ConsoleColor.Green);

            try
            {
                Auth1 = RandomAuth(7);
                Auth2 = RandomAuth(7);

                WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Generated Auth1: {Auth1}", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Generated Auth2: {Auth2}", ConsoleColor.Yellow);

                // Detect password type
                if (Pwd.Length == 32)
                {
                    WriteConsole.WriteLine("[LOGIN_DEBUG] → Detected MD5 password (US version)", ConsoleColor.Magenta);
                }
                else
                {
                    WriteConsole.WriteLine("[LOGIN_DEBUG] → Detected plain text password (TH version)", ConsoleColor.Magenta);
                }

                WriteConsole.WriteLine("[LOGIN_DEBUG] ⚡ Authenticating user via DB_pangya_member...", ConsoleColor.Cyan);

                MemberData member = null;
                using (var dbMember = new DB_pangya_member())
                {
                    member = dbMember.AuthenticateUser(User, Pwd);
                }

                // ✅ AUTO-REGISTER: If user doesn't exist, create new account automatically
                if (member == null)
                {
                    WriteConsole.WriteLine("[LOGIN_DEBUG] ⚠ User not found in database", ConsoleColor.Yellow);
                    WriteConsole.WriteLine("[LOGIN_DEBUG] ⚡ Auto-register mode: Creating new account...", ConsoleColor.Cyan);
                    
                    bool registerSuccess = InsertNewMember(User, Pwd);
                    
                    if (registerSuccess)
                    {
                        WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Auto-register successful! Authenticating new account...", ConsoleColor.Green);
                        
                        // Re-authenticate after creating account
                        using (var dbMember2 = new DB_pangya_member())
                        {
                            member = dbMember2.AuthenticateUser(User, Pwd);
                        }
                        
                        if (member == null)
                        {
                            WriteConsole.WriteLine("[LOGIN_DEBUG] ✗ Failed to authenticate newly created account!", ConsoleColor.Red);
                            Send(new byte[] { 0x01, 0x00, 0xE3, 0x5B, 0xD2, 0x4D, 0x00 });
                            Disconnect();
                            return;
                        }
                    }
                    else
                    {
                        WriteConsole.WriteLine("[LOGIN_DEBUG] ✗ Auto-register failed!", ConsoleColor.Red);
                        WriteConsole.WriteLine("[LOGIN_DEBUG] → Invalid username or password", ConsoleColor.Red);
                        Send(new byte[] { 0x01, 0x00, 0xE3, 0x5B, 0xD2, 0x4D, 0x00 });
                        Disconnect();
                        return;
                    }
                }

                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Authentication successful! Member data loaded:", ConsoleColor.Green);
                WriteConsole.WriteLine($"[LOGIN_DEBUG]   • UID: {member.UID}", ConsoleColor.White);
                WriteConsole.WriteLine($"[LOGIN_DEBUG]   • Username: {member.Username}", ConsoleColor.White);
                WriteConsole.WriteLine($"[LOGIN_DEBUG]   • Nickname: {member.Nickname}", ConsoleColor.White);
                WriteConsole.WriteLine($"[LOGIN_DEBUG]   • IDState: {member.IDState ?? 0} (0=Normal, >0=Banned)", ConsoleColor.White);
                WriteConsole.WriteLine($"[LOGIN_DEBUG]   • FirstSet: {member.FirstSet ?? 0} (0=NeedSetup, 1=Ready)", ConsoleColor.White);
                WriteConsole.WriteLine($"[LOGIN_DEBUG]   • Logon: {member.Logon ?? 0} (0=Offline, 1=Online)", ConsoleColor.White);

                // Check if user is banned
                Banned = member.IDState ?? 0;
                if (Banned > 0)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] ✗ Login failed: User is banned (IDState={Banned})", ConsoleColor.Red);
                    Response.Clear();
                    Response.Write(new byte[] { 0x01, 0x00, 0xE3, 0xF4, 0xD1, 0x4D, 0x00, });
                    Send(Response.GetBytes());
                    Disconnect();
                    return;
                }

                // Update Auth keys and increment LogonCount
                WriteConsole.WriteLine("[LOGIN_DEBUG] ⚡ Updating Auth keys and login counter...", ConsoleColor.Cyan);
                try
                {
                    using (var dbMember = new DB_pangya_member())
                    {
                        dbMember.UpdateLoginInfo(member.UID, Auth1, Auth2, GetAddress);
                    }

                    WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Auth keys and login counter updated successfully", ConsoleColor.Green);
                    WriteConsole.WriteLine($"[LOGIN_DEBUG]   • AuthKey_Login: {Auth1}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LOGIN_DEBUG]   • AuthKey_Game: {Auth2}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LOGIN_DEBUG]   • IPAddress: {GetAddress}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LOGIN_DEBUG]   • LogonCount incremented", ConsoleColor.Cyan);
                }
                catch (Exception authEx)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] ✗ Failed to update Auth keys: {authEx.Message}", ConsoleColor.Red);
                    WriteConsole.WriteLine("[LOGIN_DEBUG] → Continuing login without updating Auth keys...", ConsoleColor.Yellow);
                }

                FirstSet = member.FirstSet ?? 0;
                UID = member.UID;
                Nickname = member.Nickname;

                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ All validation checks passed!", ConsoleColor.Green);

                this.SetLogin(User);
                this.SetUID(UID);
                this.SetNickname(Nickname);
                this.SetAUTH_KEY_1(Auth1);
                this.SetAUTH_KEY_2(Auth2);
                this.SetFirstLogin(FirstSet);

                WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Player state set: UID={UID}, FirstSet={FirstSet}", ConsoleColor.Green);

                if ((member.Logon ?? 0) == 1)
                {
                    WriteConsole.WriteLine("[LOGIN_DEBUG] ⚠ User already logged in (Logon=1)", ConsoleColor.Yellow);
                    Send(new byte[] { 0x01, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00 });
                    return;
                }

                if (string.IsNullOrEmpty(Nickname))
                {
                    WriteConsole.WriteLine("[LOGIN_DEBUG] ⚠ Nickname is empty - Need to create character", ConsoleColor.Yellow);
                    Send(new byte[] { 0x01, 0x00, 0x0D9, 0x00, 0x00, 0x00, 0x00 });
                    return;
                }

                if (FirstSet == 0)
                {
                    WriteConsole.WriteLine("[LOGIN_DEBUG] ⚠ FirstSet=0 - Need character setup", ConsoleColor.Yellow);
                    Response.Clear();
                    Response.Write(new byte[] { 0x0F, 0x00, 0x00 });
                    Response.WritePStr(User);
                    Send(Response.GetBytes());

                    Response.Clear();
                    Response.Write(new byte[] { 0x01, 0x00 });
                    Response.WriteByte(0xD9);
                    Response.WriteUInt32(uint.MaxValue);
                    Send(Response.GetBytes());
                    return;
                }

                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ LOGIN SUCCESS! Proceeding to load player data...", ConsoleColor.Green);

                // Update Logon status to 1 (Online)
                try
                {
                    using (var dbMember = new DB_pangya_member())
                    {
                        dbMember.UpdateLogonStatus(member.UID, 1);
                    }
                    WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Set Logon=1 (user is now online)", ConsoleColor.Green);
                }
                catch (Exception logonEx)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] ⚠ Could not update Logon status: {logonEx.Message}", ConsoleColor.Yellow);
                }

                WriteConsole.WriteLine("===========================================", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✗✗✗ EXCEPTION OCCURRED ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                WriteConsole.WriteLine("===========================================", ConsoleColor.Cyan);
                Send(new byte[] { 0x01, 0x00, 0xE3, 0x6F, 0xD2, 0x4D, 0x00, });
                Disconnect();
                return;
            }

            //new login
            if (GetFirstLogin == 1)
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] → Loading player data (SendPlayerLoggedOnData)...", ConsoleColor.Cyan);
                this.SendPlayerLoggedOnData();
            }
        }

        private void SendPlayerLoggedOnData()
        {
            try
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
                WriteConsole.WriteLine("[LOGIN_DEBUG] Loading player data from database...", ConsoleColor.Cyan);

                // Query Capabilities and Level using DB classes
                WriteConsole.WriteLine("[LOGIN_DEBUG] ⚡ Query 1/3: Fetching Capabilities and Level...", ConsoleColor.Yellow);

                MemberData member = null;
                UserStatisticsData stats = null;

                using (var dbMember = new DB_pangya_member())
                {
                    member = dbMember.SelectByUID((int)GetUID);
                }

                using (var dbStats = new DB_pangya_user_statistics())
                {
                    stats = dbStats.SelectByUID((int)GetUID);
                }

                GetCapability = member?.Capabilities ?? 0;
                GetLevel = (byte)(stats?.Game_Level ?? 1);

                WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Capabilities: {GetCapability}", ConsoleColor.Green);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] ✓ Level: {GetLevel}", ConsoleColor.Green);

                WriteConsole.WriteLine("[LOGIN_DEBUG] 📤 Sending Auth1 key packet (0x0010)...", ConsoleColor.Magenta);
                Response.Clear();
                Response.WriteUInt16(0x0010);
                Response.WritePStr(GetAuth1);//AuthKeyLogin
                Send(Response.GetBytes());
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Auth1 packet sent", ConsoleColor.Green);

                WriteConsole.WriteLine("[LOGIN_DEBUG] 📤 Sending login data packet (0x01)...", ConsoleColor.Magenta);
                Response.Clear();
                Response.Write(new byte[] { 0x01, 0x00, 0x00, });
                Response.WritePStr(GetLogin);
                Response.WriteUInt32(GetUID);
                Response.WriteUInt32(GetCapability);//Capacity
                Response.WriteUInt32(GetLevel); // Level
                Response.WriteUInt32(10);
                Response.WriteUInt16(12);
                Response.WritePStr(GetNickname);
                Send(Response.GetBytes());
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Login data packet sent", ConsoleColor.Green);

                // ## GameServer
                WriteConsole.WriteLine("[LOGIN_DEBUG] ⚡ Query 2/3: Fetching Game Server list...", ConsoleColor.Yellow);
                byte[] Game = GameServerList();
                Send(Game);
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Game server list sent", ConsoleColor.Green);

                // ## Macro - ใช้ PascalCase ตาม SQL Schema
                WriteConsole.WriteLine("[LOGIN_DEBUG] ⚡ Query 3/3: Fetching player macros...", ConsoleColor.Yellow);
                Response.Clear();
                Response.Write(new byte[] { 0x06, 0x00 });

                using (var dbMacro = new DB_pangya_game_macro())
                {
                    var macros = dbMacro.SelectByUID((int)GetUID);

                    if (macros != null)
                    {
                        WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Macros loaded from database", ConsoleColor.Green);
                        Response.WriteStr(macros.Macro1 ?? "", 64);
                        Response.WriteStr(macros.Macro2 ?? "", 64);
                        Response.WriteStr(macros.Macro3 ?? "", 64);
                        Response.WriteStr(macros.Macro4 ?? "", 64);
                        Response.WriteStr(macros.Macro5 ?? "", 64);
                        Response.WriteStr(macros.Macro6 ?? "", 64);
                        Response.WriteStr(macros.Macro7 ?? "", 64);
                        Response.WriteStr(macros.Macro8 ?? "", 64);
                        Response.WriteStr(macros.Macro9 ?? "", 64);
                    }
                    else
                    {
                        WriteConsole.WriteLine("[LOGIN_DEBUG] ⚠ No macros found - sending empty", ConsoleColor.Yellow);
                        // Empty macros
                        for (int i = 0; i < 9; i++)
                        {
                            Response.WriteStr("", 64);
                        }
                    }
                }
                Send(Response.GetBytes());
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Macros packet sent", ConsoleColor.Green);

                // ## Messenger
                WriteConsole.WriteLine("[LOGIN_DEBUG] 📤 Sending Messenger server list...", ConsoleColor.Magenta);
                byte[] Messanger = MessangerServerList();
                Send(Messanger);
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Messenger server list sent", ConsoleColor.Green);

                WriteConsole.WriteLine("[LOGIN_DEBUG] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] 🎉 LOGIN COMPLETE! Player '{GetNickname}' (UID:{GetUID}) is ready!", ConsoleColor.Green);
                WriteConsole.WriteLine("[LOGIN_DEBUG] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine("[LOGIN_DEBUG] ✗✗✗ ERROR in SendPlayerLoggedOnData ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOGIN_DEBUG] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                WriteConsole.WriteLine("[LOGIN_DEBUG] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
                throw;
            }
        }

        private void NicknameCheck(Packet ClientPacket)
        {
            Byte Code;

            if (!ClientPacket.ReadPStr(out string Nickname))
            {
                return;
            }

            // Convert TIS-620 to UTF-8 for Thai character support
            string utf8Nickname = ConvertTIS620ToUTF8(Nickname);

            WriteConsole.WriteLine($"[NICKNAME_CHECK_DEBUG] Checking nickname: '{utf8Nickname}'", ConsoleColor.Cyan);

            // Check nickname availability using DB_pangya_member
            bool exists = false;
            using (var dbMember = new DB_pangya_member())
            {
                exists = dbMember.ExistsByNickname(utf8Nickname);
            }

            if (exists)
            {
                Code = 0; // Nickname already exists
                WriteConsole.WriteLine($"[NICKNAME_CHECK_DEBUG] Nickname exists", ConsoleColor.Yellow);
            }
            else if (string.IsNullOrWhiteSpace(utf8Nickname) || utf8Nickname.Length < 2)
            {
                Code = 2; // Invalid nickname
                WriteConsole.WriteLine($"[NICKNAME_CHECK_DEBUG] Invalid nickname (too short)", ConsoleColor.Yellow);
            }
            else
            {
                Code = 1; // Available
                WriteConsole.WriteLine($"[NICKNAME_CHECK_DEBUG] Nickname available!", ConsoleColor.Green);
            }

            if ((Code == 0) || (Code == 2))
            {
                Response.Clear();
                Response.Write(new byte[] { 0x0E, 0x00, 0x0C, 0x00, 0x00, 0x00, 0x21, 0xD2, 0x4D, 0x00 });
                this.Send(Response);
                return;
            }

            if (Code == 1)
            {
                Response.Clear();
                Response.Write(new byte[] { 0x0E, 0x00 });
                Response.WriteUInt32(0);
                Response.WritePStr(Nickname); // Send back original (TIS-620) for client display
                Send(Response.GetBytes());
            }
        }

        private void RequestCharacaterCreate(Packet ClientPacket)
        {
            if (!ClientPacket.ReadInt32(out int CHAR_TYPEID))
            {
                WriteConsole.WriteLine("[CHAR_CREATE_ERROR]: Failed to read CHAR_TYPEID", ConsoleColor.Red);
                return;
            }
            if (!ClientPacket.ReadUInt16(out ushort HAIR_COLOR))
            {
                WriteConsole.WriteLine("[CHAR_CREATE_ERROR]: Failed to read HAIR_COLOR", ConsoleColor.Red);
                return;
            }

            WriteConsole.WriteLine("=================================", ConsoleColor.Cyan);
            WriteConsole.WriteLine($"[CHAR_CREATE] ▶ Creating character for UID: {GetUID}", ConsoleColor.Cyan);

            try
            {
                // Get current player UID and nickname
                int uid = (int)GetUID;
                string utf8Nickname = GetNickname ?? string.Empty;

                WriteConsole.WriteLine($"[CHAR_CREATE] → UID: {uid}", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[CHAR_CREATE] → Nickname: '{utf8Nickname}'", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[CHAR_CREATE] → CharTypeID: {CHAR_TYPEID}", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[CHAR_CREATE] → HairColor: {HAIR_COLOR}", ConsoleColor.Yellow);

                // Determine Sex from CHAR_TYPEID
                byte sex = DetermineCharacterSex(CHAR_TYPEID);
                string sexStr = sex == 0 ? "Female" : "Male";
                WriteConsole.WriteLine($"[CHAR_CREATE] → Sex: {sexStr} ({sex})", ConsoleColor.Cyan);

                // Initialize all character data using helper method
                InitializeNewCharacterData(uid, CHAR_TYPEID, (int)HAIR_COLOR, utf8Nickname, sex);

                // Verify saved data
                using (var dbMember = new DB_pangya_member())
                {
                    var savedMember = dbMember.SelectByUID(uid);
                    if (savedMember != null)
                    {
                        WriteConsole.WriteLine($"[CHAR_CREATE_VERIFY]: ✓ Nickname: '{savedMember.Nickname}'", ConsoleColor.Magenta);
                        WriteConsole.WriteLine($"[CHAR_CREATE_VERIFY]: ✓ Sex: {(savedMember.Sex == 0 ? "Female" : "Male")} ({savedMember.Sex})", ConsoleColor.Magenta);
                    }
                }

                WriteConsole.WriteLine($"[CHAR_CREATE] ✓✓✓ Character created successfully! ✓✓✓", ConsoleColor.Green);
                WriteConsole.WriteLine($"[CHAR_CREATE] → Nickname: {utf8Nickname}", ConsoleColor.Green);
                WriteConsole.WriteLine($"[CHAR_CREATE] → Sex: {sexStr}", ConsoleColor.Green);

                // Update player state
                GetFirstLogin = 1;
                SetNickname(utf8Nickname);

                WriteConsole.WriteLine("=================================", ConsoleColor.Cyan);

                // Send player data to proceed to server selection
                WriteConsole.WriteLine("[CHAR_CREATE] 📤 Loading server selection screen...", ConsoleColor.Magenta);
                SendPlayerLoggedOnData();
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine("[CHAR_CREATE] ✗✗✗ EXCEPTION OCCURRED ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[CHAR_CREATE] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[CHAR_CREATE] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[CHAR_CREATE] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                WriteConsole.WriteLine("=================================", ConsoleColor.Cyan);

                // Send error packet to prevent client from hanging
                try
                {
                    Response.Clear();
                    Response.Write(new byte[] { 0x01, 0x00, 0xE3, 0x6F, 0xD2, 0x4D, 0x00 });
                    Send(Response.GetBytes());
                }
                catch (Exception sendEx)
                {
                    WriteConsole.WriteLine($"[CHAR_CREATE] → Failed to send error: {sendEx.Message}", ConsoleColor.Red);
                }

                Disconnect();
            }
        }

        // Helper method to initialize character data for new account
        private void InitializeNewCharacterData(int uid, int charTypeId, int hairColor, string nickname, byte sex)
        {
            WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ━━━ Initializing new character data ━━━", ConsoleColor.Cyan);

            try
            {
                // 1. Create Character
                using (var dbChar = new DB_pangya_character())
                {
                    var charData = new CharacterDbData
                    {
                        UID = uid,
                        TYPEID = charTypeId,
                        HAIR_COLOR = (byte)hairColor,
                        GIFT_FLAG = 0,
                        POWER = 0,
                        CONTROL = 0,
                        IMPACT = 0,
                        SPIN = 0,
                        CURVE = 0
                    };
                    dbChar.Insert(charData);
                    WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ✓ Character created", ConsoleColor.Green);
                }

                // 2. Update Member Info (FirstSet, Nickname, Sex)
                using (var dbMember = new DB_pangya_member())
                {
                    var member = dbMember.SelectByUID(uid);
                    if (member != null)
                    {
                        member.FirstSet = 1;
                        member.Nickname = nickname;
                        member.Sex = sex;
                        dbMember.Update(member);
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]: ✓ Member updated: Nickname='{nickname}', Sex={sex}", ConsoleColor.Green);
                    }
                }

                // 3. Initialize User Statistics
                using (var dbStats = new DB_pangya_user_statistics())
                {
                    if (!dbStats.ExistsByUID(uid))
                    {
                        var stats = new UserStatisticsData
                        {
                            UID = uid,
                            Pang = 30000000,
                            Game_Level = 70,
                            Game_Point = 999,
                            LadderPoint = 1000,
                            BestScore0 = 127,
                            BestScore1 = 127,
                            BestScore2 = 127,
                            BestScore3 = 127,
                            BESTSCORE4 = 127
                        };
                        dbStats.Insert(stats);
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ✓ User statistics initialized (Pang: 30M, Level: 70)", ConsoleColor.Green);
                    }
                    else
                    {
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ⏭ User statistics already exists, skipped", ConsoleColor.Gray);
                    }
                }

                // 4. Initialize Personal Data
                using (var dbPersonal = new DB_pangya_personal())
                {
                    if (!dbPersonal.ExistsByUID(uid))
                    {
                        var personal = new PersonalData
                        {
                            UID = uid,
                            LockerPwd = "0",
                            PangLockerAmt = 0,
                            CookieAmt = 10000000,
                            AssistMode = 1
                        };
                        dbPersonal.Insert(personal);
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ✓ Personal data initialized (Cookie: 10M, LockerPwd: '0')", ConsoleColor.Green);
                    }
                    else
                    {
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ⏭ Personal data already exists, skipped", ConsoleColor.Gray);
                    }
                }

                // 5. Initialize Starter Equipment (Club, Ball)
                int caddieId = InitializeStarterItems(uid);

                // 6. Initialize User Equip (Always recreate to ensure correct values)
                using (var dbEquip = new DB_pangya_user_equip())
                {
                    // Check if exists
                    bool equipExists = dbEquip.ExistsByUID(uid);

                    if (equipExists)
                    {
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ⚠ Updating existing user_equip", ConsoleColor.Yellow);
                        // Update existing record
                        var existingEquip = dbEquip.SelectByUID(uid);
                        if (existingEquip != null)
                        {
                            existingEquip.CADDIE = caddieId; // ✅ ใช้ CID จริง
                            existingEquip.CHARACTER_ID = 0;
                            existingEquip.CLUB_ID = 13;
                            existingEquip.BALL_ID = 335544320;
                            dbEquip.Update(existingEquip);
                        }
                    }
                    else
                    {
                        // Insert new record
                        var equip = new UserEquipData
                        {
                            UID = uid,
                            CADDIE = caddieId, // ✅ ใช้ CID จริง แทน TypeID
                            CHARACTER_ID = 0,
                            CLUB_ID = 13,
                            BALL_ID = 335544320
                        };
                        dbEquip.Insert(equip);
                    }
                    WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]: ✓ User equipment initialized (CADDIE={caddieId})", ConsoleColor.Green);
                }

                // 7. Initialize Macros (Always recreate)
                using (var dbMacro = new DB_pangya_game_macro())
                {
                    var existingMacro = dbMacro.SelectByUID(uid);

                    if (existingMacro != null)
                    {
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ⚠ Updating existing macros", ConsoleColor.Yellow);
                        // Update existing macros
                        existingMacro.Macro1 = "Pangya!";
                        existingMacro.Macro2 = "Pangya!";
                        existingMacro.Macro3 = "Pangya!";
                        existingMacro.Macro4 = "Pangya!";
                        existingMacro.Macro5 = "Pangya!";
                        existingMacro.Macro6 = "Pangya!";
                        existingMacro.Macro7 = "Pangya!";
                        existingMacro.Macro8 = "Pangya!";
                        existingMacro.Macro9 = "Pangya!";
                        existingMacro.Macro10 = "Pangya!";
                        dbMacro.Update(existingMacro);
                    }
                    else
                    {
                        // Insert new macros
                        var macroData = new GameMacroData
                        {
                            UID = uid,
                            Macro1 = "Pangya!",
                            Macro2 = "Pangya!",
                            Macro3 = "Pangya!",
                            Macro4 = "Pangya!",
                            Macro5 = "Pangya!",
                            Macro6 = "Pangya!",
                            Macro7 = "Pangya!",
                            Macro8 = "Pangya!",
                            Macro9 = "Pangya!",
                            Macro10 = "Pangya!"
                        };
                        dbMacro.Insert(macroData);
                    }
                    WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ✓ Default macros initialized", ConsoleColor.Green);
                }

                WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: ━━━ Character initialization complete! ━━━", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[CHAR_INIT_ERROR]: Failed to initialize character data: {ex.Message}", ConsoleColor.Red);
                throw;
            }
        }

        // Helper method to initialize starter items (Club, Balls, Club Info)
        // Returns CaddieID for equipment setup
        private int InitializeStarterItems(int uid)
        {
            WriteConsole.WriteLine("[CHAR_INIT_DEBUG]: → Adding starter items...", ConsoleColor.Yellow);

            try
            {
                // Set DateEnd to +10 years from now (permanent items)
                DateTime dateEnd = DateTime.Now.AddYears(10);

                using (var dbWarehouse = new DB_pangya_warehouse())
                {
                    // CLEANUP: Delete any broken records with item_id=0 (orphaned from previous
                    //          failed attempts)
                    WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]: ⚠ Cleaning up broken records (item_id=0) for UID:{uid}...", ConsoleColor.Yellow);
                    dbWarehouse.Delete(0, uid);

                    // Starter Club (Item 13 - Basic Club) with TypeID 268435456 (0x10000000) Check
                    // if club already exists
                    var existingClub = dbWarehouse.SelectByItemID(13, uid);
                    if (existingClub == null)
                    {
                        var clubItem = new WarehouseData
                        {
                            item_id = 13 // Fixed ID for Club
                            ,
                            UID = uid
                            ,
                            TYPEID = 268435456 // 0x10000000 - Basic Club
                            ,
                            C0 = 0
                            ,
                            C1 = 0
                            ,
                            C2 = 0
                            ,
                            C3 = 0
                            ,
                            C4 = 0
                            ,
                            DateEnd = dateEnd
                            ,
                            VALID = 1
                             ,
                            Flag = 0
                            ,
                            ItemType = 0
                        };
                        dbWarehouse.InsertWithItemID(clubItem); // Use InsertWithItemID for fixed ID
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Club (ID:13) added (Expires: {dateEnd:yyyy-MM-dd})", ConsoleColor.White);
                    }
                    else
                    {
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]:   • Club (ID:13) already exists, skipped", ConsoleColor.Gray);
                    }

                    // Check if Ball 1 already exists (TypeID 335544320)
                    var existingBall1 = dbWarehouse.SelectByUIDAndTypeID(uid, 335544320);
                    if (existingBall1 == null)
                    {
                        // Get next available item_id
                        int ball1ItemId = dbWarehouse.GetNextAvailableItemID(uid);

                        // Starter Ball 1 with TypeID 335544320 (0x14000000) - Normal Ball
                        var ball1 = new WarehouseData
                        {
                            item_id = ball1ItemId // Use next available ID
                            ,
                            UID = uid
                            ,
                            TYPEID = 335544320 // 0x14000000
                            ,
                            C0 = 1
                            ,
                            C1 = 0
                            ,
                            C2 = 0
                            ,
                            C3 = 0
                            ,
                            C4 = 0
                            ,
                            DateEnd = dateEnd
                            ,
                            VALID = 1
                            ,
                            Flag = 0
                            ,
                            ItemType = 0
                        };
                        dbWarehouse.InsertWithItemID(ball1);
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Ball 1 - Normal (ID:{ball1ItemId}) added", ConsoleColor.White);
                    }
                    else
                    {
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Ball 1 (ID:{existingBall1.item_id}) already exists, skipped", ConsoleColor.Gray);
                    }

                    // Check if Ball 2 already exists (TypeID 467664918)
                    var existingBall2 = dbWarehouse.SelectByUIDAndTypeID(uid, 467664918);
                    if (existingBall2 == null)
                    {
                        // Get next available item_id
                        int Assist = dbWarehouse.GetNextAvailableItemID(uid);

                        // Starter Ball 2 with TypeID 467664918 (0x1BDFFF96) - Assist Ball
                        var Assistball = new WarehouseData
                        {
                            item_id = Assist // Use next available ID
                            ,
                            UID = uid
                            ,
                            TYPEID = 467664918 // 0x1BDFFF96
                            ,
                            C0 = 1 // Assist Ball quantity
                            ,
                            C1 = 0
                            ,
                            C2 = 0
                            ,
                            C3 = 0
                            ,
                            C4 = 0
                            ,
                            DateEnd = dateEnd
                            ,
                            VALID = 1
                            ,
                            Flag = 0
                            ,
                            ItemType = 0
                        };

                        dbWarehouse.InsertWithItemID(Assistball);
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Ball 2 - Assist (ID:{Assist}) added", ConsoleColor.White);
                    }
                    else
                    {
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Ball 2 (ID:{existingBall2.item_id}) already exists, skipped", ConsoleColor.Gray);
                    }
                }

                // Initialize Starter Caddie (pang bag - TYPEID: 469762048 / 0x1C000000)
                int caddieId = 0;
                using (var dbCaddie = new DB_pangya_caddie())
                {
                    var existingCaddie = dbCaddie.SelectByUIDAndTypeID(uid, 469762048);
                    if (existingCaddie == null)
                    {
                        var caddieData = new CaddieData
                        {
                            UID = uid,
                            TYPEID = 469762048 // 0x1C000000 - Quma (pang bag)
                            ,
                            EXP = 0
                            ,
                            cLevel = 0
                            ,
                            SKIN_TYPEID = null
                            ,
                            RentFlag = 1 // 1 = Pang (permanent)
                            ,
                            END_DATE = dateEnd // +10 years
                            ,
                            SKIN_END_DATE = null
                            ,
                            TriggerPay = 0
                            ,
                            VALID = 1
                        };
                        caddieId = dbCaddie.Insert(caddieData);
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Caddie - Quma (CID:{caddieId}, TypeID:0x1C000000, Expires: {dateEnd:yyyy-MM-dd}) added", ConsoleColor.White);
                    }
                    else
                    {
                        caddieId = existingCaddie.CID;
                        WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]:   • Caddie - Quma (CID:{caddieId}) already exists, skipped", ConsoleColor.Gray);
                    }
                }

                // Initialize Club Info with 100,000 Club Points
                using (var dbClubInfo = new DB_pangya_club_info())
                {
                    if (!dbClubInfo.ExistsByItemID(13, uid))
                    {
                        var clubInfo = new ClubInfoData
                        {
                            ITEM_ID = 13,
                            UID = uid,
                            TYPEID = 268435456, // ✅ 0x10000000 - Basic Club TypeID
                            C0_SLOT = 0,
                            C1_SLOT = 0,
                            C2_SLOT = 0,
                            C3_SLOT = 0,
                            C4_SLOT = 0,
                            CLUB_POINT = 100000,
                            CLUB_WORK_COUNT = 0,
                            CLUB_SLOT_CANCEL = 0,
                            CLUB_POINT_TOTAL_LOG = 0,
                            CLUB_UPGRADE_PANG_LOG = 0
                        };
                        dbClubInfo.Insert(clubInfo);
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]:   • Club Info (TypeID:0x10000000, 100K points) added", ConsoleColor.White);
                    }
                    else
                    {
                        dbClubInfo.UpdateClubPoint(13, uid, 100000);
                        WriteConsole.WriteLine("[CHAR_INIT_DEBUG]:   • Club Info updated (100K points)", ConsoleColor.White);
                    }
                }

                WriteConsole.WriteLine($"[CHAR_INIT_DEBUG]: ✓ Starter items initialized (Club + 2 Balls + Caddie CID:{caddieId}, Expires: {dateEnd:yyyy-MM-dd}, Club Points: 100K)", ConsoleColor.Green);
                
                // ✅ Return caddieId for user_equip initialization
                return caddieId;
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[CHAR_INIT_ERROR]: Failed to add starter items: {ex.Message}", ConsoleColor.Red);
                throw;
            }
         }

        // Helper method to determine character sex from CHAR_TYPEID
        private byte DetermineCharacterSex(int charTypeId)
        {
            // Character TypeIDs mapping (based on PangYa Online character IDs) 0 = Female, 1 = Male
            /*
                67108864	Nuri
                67108865	Hana
                67108866	Arthur
                67108867	Cesillia
                67108868	Max
                67108869	Kooh
                67108870	Arin
                67108871	Kaz
                67108872	Lucia
                67108873	Nell
                67108874	Spika
                67108875	NuriR
                67108876	HanaR
                67108878	CesilliaR

             */
            switch (charTypeId)
            {
                // Male Characters
                case 67108864: // Nuri (Male)
                case 67108866: // Azer (Male)
                case 67108868: // Max (Male)
                case 67108871: // Kaz (Male)
                case 67108875: // NR (Nuri Rare - Male)
                    return 1; // Male

                // Female Characters
                case 67108870: // Arin (Female)
                case 67108865: // Hana (Female)
                case 67108876: // HR (Hana Rare - Female)
                case 67108867: // Cecilia (Female)
                case 67108869: // Kooh (Female)
                case 67108872: // Lucia (Female)
                case 67108873: // Nell (Female)
                case 67108874: // Spika (Female)
                case 67108878: // CR (Cecilia Rare - Female)
                    return 0; // Female

                // Default: Try to determine by TypeID pattern
                default:
                    WriteConsole.WriteLine($"[CHAR_SEX_WARNING]: Unknown character TypeID {charTypeId}, defaulting to Male", ConsoleColor.Yellow);
                    return 1; // Default to Male if unknown
            }
        }

        private void SendGameAuthKey()
        {
            WriteConsole.WriteLine($"[LOGIN_DEBUG] 🎮 Sending Game Auth Key (Auth2: {GetAuth2})...", ConsoleColor.Magenta);

            Response.Clear();
            Response.Write(new byte[] { 0x03, 0x00 });
            Response.WriteInt32(0);
            Response.WritePStr(GetAuth2);
            Send(Response.GetBytes());

            WriteConsole.WriteLine("[LOGIN_DEBUG] ✓ Game Auth Key sent - Player should connect to Game Server now", ConsoleColor.Green);
        }

        private void HandlePlayerReconnect(Packet packet)
        {
            packet.ReadPStr(out string Username);
            packet.ReadUInt32(out uint UID);
            packet.ReadPStr(out string AuthKey_Game);
            SetAUTH_KEY_1(RandomAuth(7));

            Response.Clear();
            Response.WriteUInt16(0x0010);
            Response.WritePStr(GetAuth1);//AuthKeyLogin
            Send(Response);

            byte[] Game = GameServerList();
            Send(Game);
        }

        private void CreateCharacter(Packet ClientPacket)
        {
            if (!ClientPacket.ReadPStr(out string Nickname))
            {
                return;
            }

            // Convert TIS-620 to UTF-8 for Thai character support
            string utf8Nickname = ConvertTIS620ToUTF8(Nickname);
            SetNickname(utf8Nickname);

            var check = utf8Nickname == GetNickname;

            Response.Clear();
            Response.Write(new byte[] { 0x01, 0x00,
                0xDA//US = D9, TH = DA
            });
            Send(Response.GetBytes());
        }

        private string RandomAuth(ushort Count)
        {
            return Guid.NewGuid().ToString()
                .ToUpper()
                .Replace("-", string.Empty).Substring(0, Count);
        }

        private byte[] GameServerList()
        {
            using (var result = new PangyaBinaryWriter())
            {
                try
                {
                    result.Write(new byte[] { 0x02, 0x00 });

                    using (var dbServer = new DB_pangya_server())
                    {
                        var servers = dbServer.SelectByType(1); // ServerType = 1 for Game Server

                        WriteConsole.WriteLine($"[LOGIN_DEBUG]   → Found {servers.Count} Game Server(s) in database", ConsoleColor.White);

                        result.WriteByte((byte)servers.Count);
                        foreach (var data in servers)
                        {
                            WriteConsole.WriteLine($"[LOGIN_DEBUG]     • {data.Name} (ID:{data.ServerID}) - {data.IP}:{data.Port} [{data.UsersOnline}/{data.MaxUser}]", ConsoleColor.Gray);
                            result.WriteStr(data.Name, 40);
                            result.WriteInt32(data.ServerID);
                            result.WriteInt32(data.MaxUser);
                            result.WriteInt32(data.UsersOnline);
                            result.WriteStr(data.IP, 18);
                            result.WriteInt32(data.Port);
                            result.WriteInt32(data.Property);
                            result.WriteUInt32(0);
                            result.WriteUInt16((ushort)data.ImgEvent);
                            result.WriteUInt16(0);
                            result.WriteInt32(100);
                            result.WriteUInt16(data.ImgNo);
                        }
                    }
                    return result.GetBytes();
                }
                catch (Exception ex)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] ✗ Error loading game servers: {ex.Message}", ConsoleColor.Red);
                    throw;
                }
            }
        }

        private byte[] MessangerServerList()
        {
            using (var result = new PangyaBinaryWriter())
            {
                try
                {
                    result.Write(new byte[] { 0x09, 0x00 });

                    using (var dbServer = new DB_pangya_server())
                    {
                        var servers = dbServer.SelectByType(2); // ServerType = 2 for Messenger Server

                        WriteConsole.WriteLine($"[LOGIN_DEBUG]   → Found {servers.Count} Messenger Server(s) in database", ConsoleColor.White);

                        result.WriteByte((byte)servers.Count);
                        foreach (var server in servers)
                        {
                            WriteConsole.WriteLine($"[LOGIN_DEBUG]     • {server.Name} (ID:{server.ServerID}) - {server.IP}:{server.Port}", ConsoleColor.Gray);
                            result.WriteStr(server.Name, 40);
                            result.WriteInt32(server.ServerID);
                            result.WriteInt32(server.MaxUser);
                            result.WriteInt32(server.UsersOnline);
                            result.WriteStr(server.IP, 18);
                            result.WriteInt32(server.Port);
                            result.WriteInt32(4096);
                            result.WriteZero(14);
                        }
                    }
                    return result.GetBytes();
                }
                catch (Exception ex)
                {
                    WriteConsole.WriteLine($"[LOGIN_DEBUG] ✗ Error loading messenger servers: {ex.Message}", ConsoleColor.Red);
                    throw;
                }
            }
        }

        public bool InsertNewMember(string username, string password)
        {
            try
            {
                WriteConsole.WriteLine($"[AUTO_REGISTER] Creating new account...", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[AUTO_REGISTER]   • Username: {username}", ConsoleColor.White);
                WriteConsole.WriteLine($"[AUTO_REGISTER]   • Password: {password}", ConsoleColor.White);
                
                using (var dbMember = new DB_pangya_member())
                {
                    // Double-check if username already exists
                    if (dbMember.ExistsByUsername(username))
                    {
                        WriteConsole.WriteLine($"[AUTO_REGISTER] ✗ Username '{username}' already exists!", ConsoleColor.Red);
                        return false;
                    }
                    
                    // Validate username (no special characters, minimum length)
                    if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                    {
                        WriteConsole.WriteLine($"[AUTO_REGISTER] ✗ Invalid username (too short, minimum 3 characters)", ConsoleColor.Red);
                        return false;
                    }
                    
                    // Create new member
                    var newMember = new MemberData
                    {
                        Username = username,
                        Password = password,
                        IDState = 0,      // 0 = Normal (not banned)
                        FirstSet = 0,     // 0 = Need character setup
                        LastLogonTime = null, // ✅ NULL = NEW PLAYER (ยังไม่เคย login เข้า Game Server)
                        Logon = 0,        // 0 = Offline
                        Capabilities = 0,
                        Sex = 0,          // Default: Female (will be set during char creation)
                        Nickname = null,  // Will be set during character creation
                        RegDate = DateTime.Now
                    };
                    
                    int newUID = dbMember.Insert(newMember);
                    
                    if (newUID > 0)
                    {
                        WriteConsole.WriteLine($"[AUTO_REGISTER] ✓ Account created successfully!", ConsoleColor.Green);
                        WriteConsole.WriteLine($"[AUTO_REGISTER]   • UID: {newUID}", ConsoleColor.White);
                        WriteConsole.WriteLine($"[AUTO_REGISTER]   • LastLogonTime: NULL (NEW PLAYER)", ConsoleColor.Yellow);
                        WriteConsole.WriteLine($"[AUTO_REGISTER]   • Status: Ready for character creation", ConsoleColor.Cyan);
                        return true;
                    }
                    else
                    {
                        WriteConsole.WriteLine($"[AUTO_REGISTER] ✗ Failed to insert member to database", ConsoleColor.Red);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[AUTO_REGISTER] ✗✗✗ EXCEPTION ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[AUTO_REGISTER] Error: {ex.Message}", ConsoleColor.Red);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[AUTO_REGISTER] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                return false;
            }
        }
    }
}