using Game.Client;
using System.Linq;
using PangyaAPI;
using PangyaAPI.BinaryModels;
using static Game.Lobby.Collection.ChannelCollection;
using static Game.GameTools.PacketCreator;
using Connector.DataBase;
using Connector.Table;
using Game.MainServer;
using System;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class GameLoginResult
    {
        public int Code { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public int Sex { get; set; }
        public int Capabilities { get; set; }
        public int Cookie { get; set; }
        public int PangLockerAmt { get; set; }
        public string LockerPwd { get; set; }
        public int AssistMode { get; set; }
    }

    public class EquipmentCheckData
    {
        public int CHARACTER_ID { get; set; }
        public int CADDIE { get; set; }
        public int CLUB_ID { get; set; }
        public int BALL_ID { get; set; }
    }

    public class LoginCoreSystem
    {
        public void PlayerLogin(GPlayer PL, Packet packet)
        {
            //GameServer GameServer;


            if (!packet.ReadPStr(out string UserID))
            {
                WriteConsole.WriteLine("[CLIENT_PLAYER]: USER UNKNOWN");
                PL.SendResponse(new byte[] { 0x76, 0x02, 0x2C, 0x01, 0x00, 0x00 }); // ## send code 300
                PL.Close();
                return;
            }

            if (!packet.ReadUInt32(out uint UID))
            {
                WriteConsole.WriteLine("[CLIENT_ERROR]: UID UNKNOWN");
                PL.SendResponse(new byte[] { 0x76, 0x02, 0x2C, 0x01, 0x00, 0x00 }); // ## send code 300
                PL.Close();
                return;
            }

            var PlayerCheck = Program._server.Players.Model.Where(c => c.GetUID == UID);

            if (PlayerCheck.Any())
            {
                WriteConsole.WriteLine("[CLIENT_CHECK]: PLAYER LOGIN DUPLICATE", ConsoleColor.Red);
                PL.SendResponse(new byte[] { 0x76, 0x02, 0x2C, 0x01, 0x00, 0x00 }); // ## send code 300
                PL.SetLogin(UserID);
                foreach (GPlayer pl in PlayerCheck)
                {
                    pl.Close();
                }
                return;
            }

            packet.Skip(6);

            if (!packet.ReadPStr(out string Code1))
            {
                WriteConsole.WriteLine("[CLIENT_ERROR]: AUTHLOGIN UNKNOWN");
                PL.SendResponse(new byte[] { 0x76, 0x02, 0x2C, 0x01, 0x00, 0x00 }); // ## send code 300
                PL.Close();
                return;
            }

            if (!packet.ReadPStr(out string Version))
            {
                WriteConsole.WriteLine("[CLIENT_ERROR]: Client Version Incompartible");
                PL.Send(new byte[] { 0x44, 0x00, 0x0B });
                PL.Close();
                return;
            }

            if (!Program.CheckVersion(Version))
            {
                WriteConsole.WriteLine("[CLIENT_ERROR]: Client Version Incompartible");
                PL.Send(new byte[] { 0x44, 0x00, 0x0B });
                PL.Close();
                return;
            }

            packet.Skip(8);

            if (!packet.ReadPStr(out string Code2))
            {
                WriteConsole.WriteLine("[CLIENT_ERROR]: AUTHGAME UNKNOWN");
                PL.SendResponse(new byte[] { 0x76, 0x02, 0x2C, 0x01, 0x00, 0x00 }); // ## send code 300
                PL.Close();
                return;
            }

           

            try
            {
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] ▶ Player login attempt...", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • Username: {UserID}", ConsoleColor.White);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • UID: {UID}", ConsoleColor.White);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • Auth1: {Code1}", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • Auth2: {Code2}", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • Version: {Version}", ConsoleColor.White);

                // Load member data
                MemberData member = null;
                PersonalData personal = null;
                
                using (var dbMember = new DB_pangya_member())
                {
                    member = dbMember.SelectByUID((int)UID);
                }

                if (member == null)
                {
                    WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✗ User not found in database!", ConsoleColor.Red);
                    PL.SendResponse(new byte[] { 0x76, 0x02, 0x2C, 0x01, 0x00, 0x00 });
                    PL.SetLogin(UserID);
                    PL.Close();
                    return;
                }

                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] ✓ User found! Username: {member.Username}", ConsoleColor.Green);
                    
                
                // Load or create personal data
                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Checking pangya_personal data...", ConsoleColor.Cyan);
                using (var dbPersonal = new DB_pangya_personal())
                {
                    personal = dbPersonal.SelectByUID((int)UID);
                    
                    if (personal == null)
                    {
                        WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ⚠ pangya_personal not found - Creating new entry...", ConsoleColor.Yellow);
                        personal = new PersonalData
                        {
                            UID = (int)UID,
                            CookieAmt = 1000000,
                            PangLockerAmt = 0,
                            LockerPwd = "0",
                            AssistMode = 0
                        };
                        dbPersonal.Insert(personal);
                        WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Created pangya_personal - Cookie: 1M, LockerPang: 0", ConsoleColor.Green);
                    }
                    else
                    {
                        WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] ✓ Personal data exists - Cookie: {personal.CookieAmt}, LockerPang: {personal.PangLockerAmt}", ConsoleColor.Green);
                    }
                }
                    
                
                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Setting player data...", ConsoleColor.Green);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] 📝 Nickname from DB: '{member.Nickname}'", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] 📝 Nickname bytes: {string.Join(", ", System.Text.Encoding.UTF8.GetBytes(member.Nickname ?? "").Select(b => $"0x{b:X2}"))}", ConsoleColor.Yellow);
                
                // ⚡ DEBUG: Check if all required data exists
                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ⚡ Checking required data in database...", ConsoleColor.Yellow);
                
                int characterCount = 0;
                using (var dbChar = new DB_pangya_character())
                {
                    characterCount = dbChar.SelectByUID((int)UID).Count;
                }
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • pangya_character: {characterCount} record(s)", characterCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);
                
                int caddieCount = 0;
                using (var dbCaddie = new DB_pangya_caddie())
                {
                    caddieCount = dbCaddie.SelectByUID((int)UID).Count;
                }
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • pangya_caddie: {caddieCount} record(s)", caddieCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);
                
                int warehouseCount = 0;
                using (var dbWarehouse = new DB_pangya_warehouse())
                {
                    warehouseCount = dbWarehouse.SelectByUID((int)UID).Count;
                }
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • pangya_warehouse: {warehouseCount} record(s)", warehouseCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);
                
                bool equipExists = false;
                using (var dbEquip = new DB_pangya_user_equip())
                {
                    equipExists = dbEquip.ExistsByUID((int)UID);
                }
                int equipCount = equipExists ? 1 : 0;
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • pangya_user_equip: {equipCount} record(s)", equipCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);
                
                bool statsExists = false;
                using (var dbStats = new DB_pangya_user_statistics())
                {
                    statsExists = dbStats.ExistsByUID((int)UID);
                }
                int statsCount = statsExists ? 1 : 0;
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • pangya_user_statistics: {statsCount} record(s)", statsCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);
                
                int macroCount = 0;
                using (var dbMacro = new DB_pangya_game_macro())
                {
                    var macro = dbMacro.SelectByUID((int)UID);
                    macroCount = macro != null ? 1 : 0;
                }
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   • pangya_game_macro: {macroCount} record(s)", macroCount > 0 ? ConsoleColor.Green : ConsoleColor.Red);
                
                
                if (characterCount == 0 || caddieCount == 0 || warehouseCount == 0 || equipCount == 0 || statsCount == 0)
                {
                    WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✗ MISSING DATA! Player cannot login - Required tables are empty!", ConsoleColor.Red);
                    WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] → This user needs to be created via Login Server first!", ConsoleColor.Yellow);
                }
                
                // ⚡ AUTO-FIX: Check and fix Pangya_User_Equip if values are 0
                if (equipCount > 0)
                {
                    WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] 🔧 Checking equipment validity...", ConsoleColor.Cyan);
                    
                    using (var dbEquip = new DB_pangya_user_equip())
                    {
                        var equipData = dbEquip.SelectByUID((int)UID);
                        if (equipData != null)
                        {
                            int charId = equipData.CHARACTER_ID;
                            int caddieId = equipData.CADDIE;
                            int clubId = equipData.CLUB_ID;
                            int ballId = equipData.BALL_ID;
                            
                            WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   → Current Equipment: Char={charId}, Caddie={caddieId}, Club={clubId}, Ball={ballId}", ConsoleColor.Yellow);
                            
                            bool needsFix = false;
                            
                            // Auto-fix CHARACTER_ID if 0
                            if (charId == 0 && characterCount > 0)
                            {
                                using (var dbChar = new DB_pangya_character())
                                {
                                    var firstChar = dbChar.SelectByUID((int)UID).FirstOrDefault();
                                    if (firstChar != null)
                                    {
                                        WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   ⚠️ CHARACTER_ID is 0! Auto-fixing to CID={firstChar.CID}", ConsoleColor.Yellow);
                                        equipData.CHARACTER_ID = firstChar.CID;
                                        needsFix = true;
                                    }
                                }
                            }
                            
                            // Auto-fix CADDIE if 0
                            if (caddieId == 0 && caddieCount > 0)
                            {
                                using (var dbCaddie = new DB_pangya_caddie())
                                {
                                    var firstCaddie = dbCaddie.SelectByUID((int)UID).FirstOrDefault();
                                    if (firstCaddie != null)
                                    {
                                        WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   ⚠️ CADDIE is 0! Auto-fixing to CID={firstCaddie.CID}", ConsoleColor.Yellow);
                                        equipData.CADDIE = firstCaddie.CID;
                                        needsFix = true;
                                    }
                                }
                            }
                            
                            // Auto-fix CLUB_ID if 0 (should always be 13 for starter club)
                            if (clubId == 0)
                            {
                                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG]   ⚠️ CLUB_ID is 0! Auto-fixing to 13 (starter club)", ConsoleColor.Yellow);
                                equipData.CLUB_ID = 13;
                                needsFix = true;
                            }
                            
                            if (needsFix)
                            {
                                dbEquip.Update(equipData);
                                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Equipment auto-fixed! Player should be able to login now.", ConsoleColor.Green);
                            }
                            else
                            {
                                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Equipment is valid - no fixes needed", ConsoleColor.Green);
                            }
                        }
                    }
                }
                
                
                PL.SetLogin(member.Username);
                PL.SetNickname(member.Nickname);
                PL.SetSex(member.Sex ?? 0);
                PL.SetCapabilities(member.Capabilities ?? 0);
                PL.SetUID(UID);
                PL.SetCookie((int)(personal.CookieAmt ?? 1000000));
                PL.LockerPang = (int)(personal.PangLockerAmt ?? 0);
                
                // Fix LockerPWD: use "0" for empty/null (needs setup), otherwise use actual value
                string lockerPwd = personal.LockerPwd;
                if (string.IsNullOrEmpty(lockerPwd))
                {
                    lockerPwd = "0";  // Needs setup
                }
                PL.LockerPWD = lockerPwd;
                
                PL.Assist = (int)(personal.AssistMode ?? 0);
                PL.SetAuthKey1(Code1);
                PL.SetAuthKey2(Code2);
                
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] ⚙️ ASSIST Mode loaded: {(PL.Assist == 0 ? "CLOSED" : "OPEN")}", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] 🔐 LockerPWD loaded: '{PL.LockerPWD}' {(PL.LockerPWD == "0" ? "(Needs setup)" : "(Already set)")}", ConsoleColor.Cyan);
                
                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Loading player statistics...", ConsoleColor.Green);
                PL.LoadStatistic();

                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Loading guild data...", ConsoleColor.Green);
                PL.LoadGuildData();

                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ▶ Sending post-login init packets...", ConsoleColor.Yellow);
                SendJunkPackets(PL);

                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✓ Sending player info...", ConsoleColor.Green);
                PlayerRequestInfo(PL, Version);

                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] 🎉 Login successful! Player '{member.Nickname}' (UID:{UID}) is ready!", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine("[GAME_LOGIN_DEBUG] ✗✗✗ EXCEPTION OCCURRED ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[GAME_LOGIN_DEBUG] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                PL.Close();
            }
            finally
            {
                packet.Dispose();
            }
        }

        void PlayerRequestInfo(GPlayer PL, string ServerVersion)
        {
            #region HandlePlayer
            PangyaBinaryWriter Reply;

            WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ━━━━━━ Starting PlayerRequestInfo ━━━━━━", ConsoleColor.Magenta);
            WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] Player: {PL.GetNickname} (UID: {PL.GetUID})", ConsoleColor.Cyan);
            WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] Server Version: {ServerVersion}", ConsoleColor.White);

            var Inventory = PL.Inventory;

            try
            {
                #region PlayerLogin
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Main Packet...", ConsoleColor.Yellow);
                PL.SendMainPacket(ServerVersion);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Main Packet sent", ConsoleColor.Green);
                #endregion

                #region PlayerCharacterInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Character Info...", ConsoleColor.Yellow);
                var charData = Inventory.ItemCharacter.Build();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Character packet size: {charData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(charData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Character Info sent", ConsoleColor.Green);
                #endregion

                #region PlayerCaddieInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Caddie Info...", ConsoleColor.Yellow);
                var caddieData = Inventory.ItemCaddie.Build();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Caddie packet size: {caddieData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(caddieData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Caddie Info sent", ConsoleColor.Green);
                #endregion

                #region PlayerWarehouseInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Warehouse Info...", ConsoleColor.Yellow);
                var warehouseData = Inventory.ItemWarehouse.Build();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Warehouse packet size: {warehouseData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(warehouseData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Warehouse Info sent", ConsoleColor.Green);
                #endregion

                #region PlayerMascotsInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Mascot Info...", ConsoleColor.Yellow);
                var mascotData = Inventory.ItemMascot.Build();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Mascot packet size: {mascotData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(mascotData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Mascot Info sent", ConsoleColor.Green);
                #endregion

                #region PlayerToolBarInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Toolbar (Equipment)...", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ⚙️  Equipment Check:", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]     • Character Index: {Inventory.CharacterIndex}", ConsoleColor.White);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]     • Caddie Index: {Inventory.CaddieIndex}", ConsoleColor.White);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]     • Club Index: {Inventory.ClubSetIndex}", ConsoleColor.White);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]     • Ball TypeID: {Inventory.BallTypeID}", ConsoleColor.White);
                
                // ✅ ตั้งค่า SelectedClubInLocker = ClubSetIndex (ไม้ที่ใส่อยู่)
                PL.SelectedClubInLocker = Inventory.ClubSetIndex;
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]     • ✓ Initialized SelectedClubInLocker = {PL.SelectedClubInLocker}", ConsoleColor.Green);
                
                if (Inventory.ClubSetIndex == 0)
                {
                    WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ⚠️ WARNING: Club Index is 0! Player has no club equipped!", ConsoleColor.Red);
                }
                if (Inventory.BallTypeID == 0)
                {
                    WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ⚠️ WARNING: Ball TypeID is 0! Player has no ball equipped!", ConsoleColor.Red);
                }
                
                var toolbarData = Inventory.GetToolbar();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Toolbar packet size: {toolbarData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(toolbarData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Toolbar sent", ConsoleColor.Green);
                #endregion

                #region PlayerLobbyListInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Lobby List...", ConsoleColor.Yellow);
                var lobbyListData = LobbyList.Build(true);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Lobby List packet size: {lobbyListData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(lobbyListData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Lobby List sent", ConsoleColor.Green);
                #endregion

                #region Map Rate
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Map Rate...", ConsoleColor.Yellow);
                var mapData = ShowLoadMap();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Map Rate packet size: {mapData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(mapData);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ✓ Map Rate sent", ConsoleColor.Green);
                #endregion

                #region PlayerAchievement
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Loading Achievements...", ConsoleColor.Yellow);
                PL.ReloadAchievement();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Achievement reloaded", ConsoleColor.Green);

                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Achievement Counter...", ConsoleColor.Yellow);
                PL.SendAchievementCounter();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Achievement Counter sent", ConsoleColor.Green);

                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Achievement Data...", ConsoleColor.Yellow);
                PL.SendAchievement();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Achievement Data sent", ConsoleColor.Green);
                #endregion

                #region Call Messeger Server
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Calling Messenger Server...", ConsoleColor.Yellow);
                new MessengerServerCoreSystem().PlayerCallMessengerServer(PL);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Messenger Server called", ConsoleColor.Green);
                #endregion

                #region PlayerCardInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Card Info...", ConsoleColor.Yellow);
                var cardData = Inventory.ItemCard.Build();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Card packet size: {cardData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(cardData);

                PL.SendResponse(new byte[] { 0x36, 0x01 });
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Card Info sent", ConsoleColor.Green);
                #endregion

                #region PlayerCardEquipInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Card Equip Info...", ConsoleColor.Yellow);
                var cardEquipData = Inventory.ItemCardEquip.Build();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → Card Equip packet size: {cardEquipData?.Length ?? 0} bytes", ConsoleColor.Gray);
                PL.SendResponse(cardEquipData);

                PL.SendResponse(new byte[] { 0x81, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 });
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Card Equip Info sent", ConsoleColor.Green);
                #endregion

                #region PlayerCookies
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Cookies ({PL.GetCookie})...", ConsoleColor.Yellow);
                PL.SendCookies();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Cookies sent", ConsoleColor.Green);
                #endregion

                #region TROPHY
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Trophies (6 packets)...", ConsoleColor.Yellow);
                Reply = new PangyaBinaryWriter();
                Reply.Write(Inventory.ItemTrophies.Build(5));
                PL.SendResponse(Reply.GetBytes());

                Reply = new PangyaBinaryWriter();
                Reply.Write(Inventory.ItemTrophies.Build(0));
                PL.SendResponse(Reply.GetBytes());

                Reply = new PangyaBinaryWriter();
                Reply.Write(Inventory.ItemTrophySpecial.Build(5));
                PL.SendResponse(Reply.GetBytes());

                Reply = new PangyaBinaryWriter();
                Reply.Write(Inventory.ItemTrophySpecial.Build(0));
                PL.SendResponse(Reply.GetBytes());

                Reply = new PangyaBinaryWriter();
                Reply.Write(Inventory.ItemTrophyGP.Build(5));
                PL.SendResponse(Reply.GetBytes());

                Reply = new PangyaBinaryWriter();
                Reply.Write(Inventory.ItemTrophyGP.Build(0));
                PL.SendResponse(Reply.GetBytes());
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ All Trophies sent", ConsoleColor.Green);
                #endregion

                #region StatisticInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Statistics...", ConsoleColor.Yellow);
                Reply = new PangyaBinaryWriter();
                Reply.Write(new byte[] { 0x58, 01, 0x00 });
                Reply.Write(PL.GetUID);
                Reply.Write(PL.Statistic());
                PL.SendResponse(Reply.GetBytes());
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Statistics sent", ConsoleColor.Green);
                #endregion

                #region MailGiftBox
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Sending Mail Popup...", ConsoleColor.Yellow);
                PL.SendMailPopup();
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Mail Popup sent", ConsoleColor.Green);
                #endregion

                #region ChatOffLine
                // PL.SendChatOffline();
                #endregion

                #region Check ASSIST Mode from Database
                // Load ASSIST state from database instead of checking inventory
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Loading ASSIST Mode from database...", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   → ASSIST Mode: {(PL.Assist == 0 ? "CLOSED" : "OPEN")} ({PL.Assist})", ConsoleColor.Cyan);
                #endregion

                #region PlayerGetMessengerServerInfo
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 📤 Connecting to Messenger Server...", ConsoleColor.Yellow);
                new MessengerServerCoreSystem().PlayerConnectMessengerServer(PL);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG]   ✓ Messenger Server connected", ConsoleColor.Green);
                #endregion
                
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] ━━━━━━ PlayerRequestInfo COMPLETE ━━━━━━", ConsoleColor.Magenta);
                WriteConsole.WriteLine($"[PLAYER_INFO_DEBUG] 🎉 All packets sent successfully! Player should now see lobby.", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[PLAYER_INFO_ERROR] ✗✗✗ EXCEPTION in PlayerRequestInfo ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[PLAYER_INFO_ERROR] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[PLAYER_INFO_ERROR] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[PLAYER_INFO_ERROR] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
            }
            #endregion
        }

        // Disabled: these packets are not required for core login flow and may trigger client error popups.
        // If needed later, re-enable selectively once the exact opcode expectations are confirmed.
        public void SendJunkPackets(GPlayer PL)
        {
            if (PL == null || !PL.Connected)
            {
                return;
            }

            PL.SendResponse(new byte[] { 0x44, 0x00, 0xD3, 0x00 });

            PL.SendResponse(ShowLoadServer(0x01));
            PL.SendResponse(ShowLoadServer(0x03));
            PL.SendResponse(ShowLoadServer(0x09));
            PL.SendResponse(ShowLoadServer(0x07));
            PL.SendResponse(ShowLoadServer(0x0B));
            PL.SendResponse(ShowLoadServer(0x0D));
            PL.SendResponse(ShowLoadServer(0x17));
            PL.SendResponse(ShowLoadServer(0x0F));
            PL.SendResponse(ShowLoadServer(0x13));
            PL.SendResponse(ShowLoadServer(0x1E));
            PL.SendResponse(ShowLoadServer(0x19));
            PL.SendResponse(ShowLoadServer(0x1B));
            PL.SendResponse(ShowLoadServer(0x12));
            PL.SendResponse(ShowLoadServer(0x14));

            new TutorialCoreSystem(PL);

            PL.SendResponse(ShowLoadServer(0x1D));
        }
    }
}
