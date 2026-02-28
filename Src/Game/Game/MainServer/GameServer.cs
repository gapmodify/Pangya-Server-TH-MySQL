using PangyaAPI.Auth;
using Game.Client;
using Connector.DataBase;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static Game.Lobby.Collection.ChannelCollection;
using PangyaAPI.BinaryModels;
using Game.Lobby.Collection;
using System.IO;
using Game.Lobby;
using System.Runtime.CompilerServices;
using PangyaAPI.Server;
using PangyaAPI.Tools;
using PangyaAPI.PangyaClient;
using PangyaFileCore;
using static PangyaFileCore.IffBaseManager;

namespace Game.MainServer
{
    public class MemberSimple
    {
        public int UID { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
    }

    public class CharacterSimple
    {
        public int CID { get; set; }
    }

    public class MemberFull
    {
        public int UID { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public int IDState { get; set; }
        public int FirstSet { get; set; }
        public int Logon { get; set; }
        public int? Capabilities { get; set; }
        public byte? Sex { get; set; }
        public DateTime? RegDate { get; set; }
    }

    public class GameServer : TcpServer
    {
        public bool Messenger_Active { get; set; }
        public IniFile Ini { get; set; }
        public GameServer()
        {
            try
            {
                // กำหนด INI file ที่ต้องการให้ชัดเจน
                DatabaseConfig.Initialize("Game.ini");
                
                Ini = new IniFile(ConfigurationManager.AppSettings["ServerConfig"]);
                Data = new ServerSettings
                {
                    Name = Ini.ReadString("Config", "Name", "Pippin"),
                    Version = Ini.ReadString("Config", "Version", "SV_GS_Release_2.0"),
                    UID = Ini.ReadUInt32("Config", "UID", 20201),
                    MaxPlayers = Ini.ReadUInt32("Config", "MaxPlayers", 3000),
                    Port = Ini.ReadUInt32("Config", "Port", 20201),
                    IP = Ini.ReadString("Config", "IP", "127.0.0.1"),
                    Property = Ini.ReadUInt32("Config", "Property", 2048),
                    BlockFunc = Ini.ReadInt64("Config", "BlockFuncSystem", 0),
                    EventFlag = Ini.ReadInt16("Config", "EventFlag", 0),
                    ImgNo = Ini.ReadInt16("Config", "Icon", 1),
                    GameVersion = Ini.ReadString("Config", "GameVersion", "845.01"),
                    Type = AuthClientTypeEnum.GameServer,
                    AuthServer_Ip = Ini.ReadString("Config", "AuthServer_IP", "127.0.0.1"),
                    AuthServer_Port = Ini.ReadInt32("Config", "AuthServer_Port", 7997),
                    Key = "3493ef7ca4d69f54de682bee58be4f93"
                };
                ShowLog = Ini.ReadBool("Config", "PacketLog", false);
                Messenger_Active = Ini.ReadBool("Config", "Messenger_Server", false);

                Console.Title = $"Pangya Fresh Up! GameServer - {Data.Name} - Players: {Players.Count} ";

                WriteConsole.WriteLine($"[SERVER_CONFIG]: GameVersion set to '{Data.GameVersion}'", ConsoleColor.Green);

                if (ConnectToAuthServer(AuthServerConstructor()) == false)
                {
                    new GameTools.ClearMemory().FlushMemory();
                    WriteConsole.WriteLine("[ERROR_START_AUTH]: Could not connect to AuthServer");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                _server = new TcpListener(IPAddress.Parse(Data.IP), (int)Data.Port);

            }
            catch (Exception erro)
            {
                new GameTools.ClearMemory().FlushMemory();
                WriteConsole.WriteLine($"[ERROR_START]: {erro.Message}", ConsoleColor.Red);
                if (erro.InnerException != null)
                {
                    WriteConsole.WriteLine($"[ERROR_START_INNER]: {erro.InnerException.Message}", ConsoleColor.Red);
                }
                WriteConsole.WriteLine($"[ERROR_START_STACK]: {erro.StackTrace}", ConsoleColor.Yellow);
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        public override void ServerStart()
        {
            try
            {
                Data.InsertServer();
                _isRunning = true;
                _server.Start((int)Data.MaxPlayers);

                if (DateTime.Now == EndTime || ( DateTime.Now.Month == EndTime.Month && DateTime.Now.Day == EndTime.Day))
                {
                    _isRunning = false;
                }
                WriteConsole.WriteLine($"[SERVER_START]: PORT {Data.Port}", ConsoleColor.Green);
                //Inicia os Lobby's
                Ini = new IniFile(ConfigurationManager.AppSettings["ChannelConfig"]);

                LobbyList = new ChannelCollection(Ini);
                //Inicia a leitura dos arquivos .iff
                new IffBaseManager();//is 100% work? test for iff

                //// Dump Item.iff to CSV for TypeID discovery (saved next to the executable)
                //try
                //{
                //    var items = new PangyaFileCore.Collections.ItemCollection();
                //    items.Load();
                //    items.DumpToCsv("item_dump.csv");
                //}
                //catch (Exception ex)
                //{
                //    WriteConsole.WriteLine($"[IFF_DUMP_ERROR]: {ex.Message}", ConsoleColor.Yellow);
                //}

                 //Inicia Thread para escuta de clientes
                var WaitConnectionsThread = new Thread(new ThreadStart(HandleWaitConnections));
                WaitConnectionsThread.Start();
            }
            catch (Exception erro)
            {
                new GameTools.ClearMemory().FlushMemory();
                WriteConsole.WriteLine($"[ERROR_START]: {erro.Message}");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
        protected override Player OnConnectPlayer(TcpClient tcp)
        {
            var player = new GPlayer(tcp)
            {
                ConnectionID = NextConnectionId
            };

            NextConnectionId += 1;

            SendKey(player);

            WriteConsole.WriteLine($"[PLAYER_CONNECT]: {player.GetAddress}:{player.GetPort}", ConsoleColor.Green);

            Players.Add(player);
            UpdateServer();
            Console.Title = $"Pangya Fresh Up! GameServer - {Data.Name} - Players: {Players.Count} ";

            return player;
        }

        protected override AuthClient AuthServerConstructor()
        {
            return new AuthClient(Data);
        }

        protected override void SendKey(Player player)
        {
            var Player = (GPlayer)player;
            try
            {
                if (Player.Tcp.Connected && Player.Connected)
                {
                    Player.Response = new PangyaBinaryWriter();
                    //Gera Packet com chave de criptografia (posisão 8)
                    Player.Response.Write(new byte[] { 0x00, 0x06, 0x00, 0x00, 0x3f, 0x00, 0x01, 0x01 });
                    Player.Response.WriteByte(Player.GetKey);
                    Player.SendBytes(Player.Response.GetBytes());
                    Player.Response.Clear();
                }
            }
            catch
            {
                Player.Close();
            }
        }

        public override void DisconnectPlayer(Player Player)
        {
            var Client = (GPlayer)Player;
            if (Client != null && Client.Connected)
            {
                var PLobby = Client.Lobby;

                if (PLobby != null)
                {
                    PLobby.RemovePlayer(Client);
                }
                Client.PlayerLeave(); //{ push player to offline }

                Players.Remove(Client); //{ remove from player lists }
                Player.Connected = false;
                Player.Dispose();
                Player.Tcp.Close();
            }
            WriteConsole.WriteLine(string.Format("[PLAYER_DISCONNECT]: {0} is disconnected", Client?.GetLogin), ConsoleColor.Red);

            UpdateServer();
            Console.Title = $"Pangya Fresh Up! GameServer - {Data.Name} - Players: {Players.Count} ";
            new GameTools.ClearMemory().FlushMemory();
        }

        protected override void ServerExpection(Player Client, Exception Ex)
        {
            var player = (GPlayer)Client;
            try
            {
                using (var _db = DbContextFactory.Create())
                {
                    var query = $"INSERT INTO pangya_exception_Log (UID, Username, ExceptionMessage, Server, CreateDate) VALUES ({player.GetUID}, '{player.GetLogin}', '{Ex.Message.Replace("'", "''")}', '{Data.Name}', NOW())";
                    _db.Database.ExecuteSqlCommand(query);
                }

                System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(Ex, true);
                var FileWrite = new StreamWriter("GameLog.txt", true);
                FileWrite.WriteLine($"--------------------------- PLAYER_EXCEPTION ------------------------------------------");
                FileWrite.WriteLine($"Date: {DateTime.Now}");
                FileWrite.WriteLine($"Server_Info: NAME {Data.Name}, ID {Data.UID}, PORT {Data.Port}");
                FileWrite.WriteLine(trace.GetFrame(0).GetMethod().ReflectedType.FullName);
                FileWrite.WriteLine("Method: " + Ex.TargetSite);
                FileWrite.WriteLine("Line: " + trace.GetFrame(0).GetFileLineNumber());
                FileWrite.WriteLine("Column: " + trace.GetFrame(0).GetFileColumnNumber());
                FileWrite.WriteLine($"--------------------------- END ------------------------------------------");
                FileWrite.Dispose();
                if (player.Connected)
                {
                    DisconnectPlayer(player);
                }
            }
            catch (Exception logEx)
            {
                WriteConsole.WriteLine($"[EXCEPTION_LOG_ERROR]: {logEx.Message}", ConsoleColor.Red);
            }
            new GameTools.ClearMemory().FlushMemory();
        }


        public override Player GetClientByConnectionId(uint ConnectionId)
        {
            var Client = (GPlayer)Players.Model.Where(c => c.ConnectionID == ConnectionId).FirstOrDefault();

            return Client;
        }

        public override Player GetPlayerByNickname(string Nickname)
        {
            var Client = (GPlayer)Players.Model.Where(c => c.GetNickname == Nickname).FirstOrDefault();

            return Client;
        }

        public override Player GetPlayerByUsername(string Username)
        {
            var Client = (GPlayer)Players.Model.Where(c => c.GetLogin == Username).FirstOrDefault();

            return Client;
        }

        public override Player GetPlayerByUID(uint UID)
        {
            var Client = (GPlayer)Players.Model.Where(c => c.GetUID == UID).FirstOrDefault();

            return Client;
        }

        public override bool GetPlayerDuplicate(uint UID)
        {
            throw new NotImplementedException();
        }

        public override bool PlayerDuplicateDisconnect(uint UID)
        {
            throw new NotImplementedException();
        }

        protected override void OnAuthServerPacketReceive(AuthClient client, AuthPacket packet)
        {
            if (packet.ID != AuthPacketEnum.SERVER_KEEPALIVE)
            {
                WriteConsole.WriteLine("[SYNC_RECEIVED_PACKET]:  " + packet.ID);
            }
            switch (packet.ID)
            {
                case AuthPacketEnum.SERVER_KEEPALIVE: //KeepAlive
                    {
                    }
                    break;
                case AuthPacketEnum.SERVER_CONNECT:
                    {
                    }
                    break;
                case AuthPacketEnum.SERVER_RELEASE_CHAT:
                    {
                        string GetNickName = packet.Message.PlayerNick;
                        string GetMessage = packet.Message.PlayerMessage;
                        GameTools.PacketCreator.ChatText(GetNickName, GetMessage, true);
                    }
                    break;
                case AuthPacketEnum.RECEIVES_USER_UID:
                    break;
                case AuthPacketEnum.SEND_DISCONNECT_PLAYER:
                    {
                        uint UID = packet.Message.ID;

                        var player = GetPlayerByUID(UID);

                        if (player != null)
                        {
                            DisconnectPlayer(player);
                        }
                    }
                    break;
                case AuthPacketEnum.SERVER_RELEASE_TICKET:
                    {
                        string GetNickName = packet.Message.GetNickName;
                        string GetMessage = packet.Message.GetMessage;
                        using (var result = new PangyaBinaryWriter())
                        {
                            result.Write(new byte[] { 0xC9, 0x00 });
                            result.WritePStr(GetNickName);
                            result.WritePStr(GetMessage);
                            SendToAll(result.GetBytes());
                        }
                    }
                    break;
                case AuthPacketEnum.SERVER_RELEASE_BOXRANDOM:
                    {
                        string GetMessage = packet.Message.GetMessage;
                        Notice(GetMessage);
                    }
                    break;
                case AuthPacketEnum.SERVER_RELEASE_NOTICE_GM:
                    {
                        string Nick = packet.Message.GetNick;
                        string message = packet.Message.mensagem;
                        HandleStaffSendNotice(Nick, message);
                    }
                    break;
                case AuthPacketEnum.SERVER_RELEASE_NOTICE:
                    {
                        string message = packet.Message.mensagem;
                        using (var result = new PangyaBinaryWriter())
                        {
                            result.Write(new byte[] { 0x42, 0x00 });
                            result.WritePStr("Notice: " + message);
                            SendToAll(result.GetBytes());
                        }
                    }
                    break;
                case AuthPacketEnum.PLAYER_LOGIN_RESULT:
                    {
                        LoginResultEnum loginresult = packet.Message.Type;

                        if (loginresult == LoginResultEnum.Error || loginresult == LoginResultEnum.Exception)
                        {
                            WriteConsole.WriteLine("[CLIENT_ERROR]: Sorry", ConsoleColor.Red);
                            return;
                        }
                    }
                    break;
                case AuthPacketEnum.SERVER_COMMAND:
                    break;
                default:
                    WriteConsole.WriteLine("[AUTH_PACKET]:  " + packet.ID);
                    break;
            }
        }
        public void HandleStaffSendNotice(string Nickname, string Msg)
        {
            var response = new PangyaBinaryWriter();
            try
            {
                if (Nickname.Length <= 0 || Msg.Length <= 0)
                {
                    return;
                }

                response.Write(new byte[] { 0x40, 0x00, 0x07 });
                response.WritePStr(Nickname);
                response.WritePStr(Msg);
                this.SendToAll(response.GetBytes());
            }
            finally
            {
                response.Dispose();
            }
        }

        public override void RunCommand(string[] Command)
        {
            string ReadCommand;
            GPlayer P;

            if (Command.Length > 1)
            {
                ReadCommand = string.Join(" ", Command, 1, Command.Length - 1);
            }
            else
            {
                ReadCommand = "";
            }
            
            string mainCommand = Command[0].ToLower();
            switch (mainCommand)
            {
                case "cls":
               
                case "clear":
                    {
                        Console.Clear();
                    }
                    break;
                case "kickuid":
                    {
                        P = (GPlayer)GetPlayerByUID(uint.Parse(ReadCommand.ToString()));
                        if (P == null)
                        {
                            WriteConsole.WriteLine("[SYSTEM_COMMAND]: THIS UID IS NOT ONLINE!", ConsoleColor.Red);
                            break;
                        }
                        DisconnectPlayer(P);
                    }
                    break;
                case "kickname":
                    {
                        P = (GPlayer)GetPlayerByNickname(ReadCommand);
                        if (P == null)
                        {
                            WriteConsole.WriteLine("[SYSTEM_COMMAND]: THIS NICKNAME IS NOT ONLINE!", ConsoleColor.Red);
                            return;
                        }
                        DisconnectPlayer(P);
                    }
                    break;
                case "kickuser":
                    {
                        P = (GPlayer)GetPlayerByUsername(ReadCommand);
                        if (P == null)
                        {
                            WriteConsole.WriteLine("[SYSTEM_COMMAND]: THIS USERNAME IS NOT ONLINE!", ConsoleColor.Red);
                            return;
                        }
                        DisconnectPlayer(P);
                    }
                    break;
               
               
                case "showchannel":
                    {
                        LobbyList.ShowChannel();
                    }
                    break;
                case "help":
                    {
                        ShowHelp();
                    }
                    break;
               
                case "reconfig":
                    {
                        Ini = new IniFile(ConfigurationManager.AppSettings["ServerConfig"]);
                        Data = new ServerSettings
                        {
                            Name = Ini.ReadString("Config", "Name", "Pippin"),
                            Version = Ini.ReadString("Config", "Version", "SV_GS_Release_2.0"),
                            UID = Ini.ReadUInt32("Config", "UID", 20201),
                            MaxPlayers = Ini.ReadUInt32("Config", "MaxPlayers", 3000),
                            Port = Ini.ReadUInt32("Config", "Port", 20201),
                            IP = Ini.ReadString("Config", "IP", "127.0.0.1"),
                            Property = Ini.ReadUInt32("Config", "Property", 2048),
                            BlockFunc = Ini.ReadInt64("Config", "BlockFuncSystem", 0),
                            EventFlag = Ini.ReadInt16("Config", "EventFlag", 0),
                            ImgNo = Ini.ReadInt16("Config", "Icon", 1),
                            GameVersion = Ini.ReadString("Config", "GameVersion", "845.01"),
                            Type = AuthClientTypeEnum.GameServer,
                            AuthServer_Ip = Ini.ReadString("Config", "AuthServer_IP", "127.0.0.1"),
                            AuthServer_Port = Ini.ReadInt32("Config", "AuthServer_Port", 7997),
                            Key = "3493ef7ca4d69f54de682bee58be4f93"
                        };
                        ShowLog = Ini.ReadBool("Config", "PacketLog", false);
                        Messenger_Active = Ini.ReadBool("Config", "Messenger_Server", false);

                        WriteConsole.WriteLine($"[SERVER_RELOAD]: GameVersion reloaded to '{Data.GameVersion}'", ConsoleColor.Green);

                        var packet = new AuthPacket()
                        {
                            ID = AuthPacketEnum.SERVER_UPDATE,
                            Message = new
                            {
                                _data = Data
                            }
                        };


                        this.AuthServer.Send(packet);
                    }
                    break;
                case "add":
                    {
                        if (string.IsNullOrEmpty(ReadCommand))
                        {
                            WriteConsole.WriteLine("[ADD_USER]: Usage:", ConsoleColor.Yellow);
                            WriteConsole.WriteLine("  add username password", ConsoleColor.Cyan);
                            WriteConsole.WriteLine("  Example: add test 1234", ConsoleColor.Gray);
                            break;
                        }

                        var parts = ReadCommand.Split(new[] { ' ' }, 2);
                        if (parts.Length != 2)
                        {
                            WriteConsole.WriteLine("[ADD_USER_ERROR]: Invalid format!", ConsoleColor.Red);
                            WriteConsole.WriteLine("  Usage: add username password", ConsoleColor.Yellow);
                            break;
                        }

                        string username = parts[0].Trim();
                        string password = parts[1].Trim();

                        var loginInfo = new Functions.LoginInfoCoreSystem();
                        bool success = loginInfo.InsertNewMemberSimple(username, password);

                        if (!success)
                        {
                            WriteConsole.WriteLine($"[ADD_USER_FAILED]: Cannot create '{username}'", ConsoleColor.Red);
                        }
                    }
                    break;
                case "checkuser":
                    {
                        if (string.IsNullOrEmpty(ReadCommand))
                        {
                            WriteConsole.WriteLine("[CHECK_USER]: Usage: checkuser username", ConsoleColor.Yellow);
                            break;
                        }

                        using (var _db = DbContextFactory.Create())
                        {
                            var sql = "SELECT UID, Username, Nickname, IDState, FirstSet, Logon, Capabilities, Sex, RegDate FROM pangya_member WHERE Username = @p0 LIMIT 1";
                            var member = _db.Database.SqlQuery<MemberFull>(sql, ReadCommand).FirstOrDefault();

                            if (member == null)
                            {
                                WriteConsole.WriteLine($"[CHECK_USER]: User '{ReadCommand}' not found", ConsoleColor.Red);
                                break;
                            }

                            WriteConsole.WriteLine($"╔═══════════════════════ USER INFO ═══════════════════════╗", ConsoleColor.Cyan);
                            WriteConsole.WriteLine($"║ Username     : {member.Username,-40} ║", ConsoleColor.White);
                            WriteConsole.WriteLine($"║ Nickname     : {member.Nickname,-40} ║", ConsoleColor.White);
                            WriteConsole.WriteLine($"║ UID          : {member.UID,-40} ║", ConsoleColor.White);
                            WriteConsole.WriteLine($"║ IDState      : {member.IDState,-40} ║", member.IDState == 0 ? ConsoleColor.Green : ConsoleColor.Red);
                            WriteConsole.WriteLine($"║ FirstSet     : {member.FirstSet,-40} ║", member.FirstSet == 1 ? ConsoleColor.Green : ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"║ Logon        : {member.Logon,-40} ║", member.Logon == 0 ? ConsoleColor.Green : ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"║ Capabilities : {member.Capabilities,-40} ║", ConsoleColor.White);
                            WriteConsole.WriteLine($"║ Sex          : {(member.Sex == 1 ? "Male" : member.Sex == 0 ? "Female" : "NULL"),-40} ║", ConsoleColor.White);
                            WriteConsole.WriteLine($"║ RegDate      : {member.RegDate,-40} ║", ConsoleColor.White);
                            WriteConsole.WriteLine($"╚═══════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);

                            if (member.IDState != 0)
                            {
                                WriteConsole.WriteLine($"⚠️  WARNING: IDState = {member.IDState} (1 = Banned, must be 0)", ConsoleColor.Red);
                            }
                            if (member.FirstSet != 1)
                            {
                                WriteConsole.WriteLine($"⚠️  WARNING: FirstSet = {member.FirstSet} (Must be 1 to login)", ConsoleColor.Yellow);
                            }
                            if (member.Logon != 0)
                            {
                                WriteConsole.WriteLine($"⚠️  WARNING: Logon = {member.Logon} (Should be 0)", ConsoleColor.Yellow);
                            }
                        }
                    }
                    break;
                case "additem":
                    {
                        if (string.IsNullOrEmpty(ReadCommand))
                        {
                            WriteConsole.WriteLine("[ADD_ITEM]: Usage: additem username typeid [quantity]", ConsoleColor.Yellow);
                            WriteConsole.WriteLine("  Example: additem bdm 335544320 10", ConsoleColor.Gray);
                            WriteConsole.WriteLine("  Example: additem bdm 268435456 (quantity = 1 by default)", ConsoleColor.Gray);
                            WriteConsole.WriteLine("  Note: Use Username (not Nickname) - Player must be ONLINE", ConsoleColor.DarkGray);
                            break;
                        }

                        var parts = ReadCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2)
                        {
                            WriteConsole.WriteLine("[ADD_ITEM_ERROR]: Invalid format!", ConsoleColor.Red);
                            WriteConsole.WriteLine("  Usage: additem username typeid [quantity]", ConsoleColor.Yellow);
                            break;
                        }

                        string username = parts[0].Trim();
                        if (!uint.TryParse(parts[1].Trim(), out uint typeId))
                        {
                            WriteConsole.WriteLine("[ADD_ITEM_ERROR]: Invalid TypeID! Must be a valid number", ConsoleColor.Red);
                            break;
                        }

                        uint quantity = 1; // Default quantity
                        if (parts.Length >= 3)
                        {
                            if (!uint.TryParse(parts[2].Trim(), out quantity) || quantity < 1)
                            {
                                WriteConsole.WriteLine("[ADD_ITEM_ERROR]: Invalid quantity! Must be a positive number", ConsoleColor.Red);
                                break;
                            }
                        }

                        // Try to find player by Username first
                        var targetPlayer = (GPlayer)GetPlayerByUsername(username);
                        
                        // If not found, try Nickname
                        if (targetPlayer == null)
                        {
                            targetPlayer = (GPlayer)GetPlayerByNickname(username);
                        }
                        
                        if (targetPlayer == null)
                        {
                            WriteConsole.WriteLine($"[ADD_ITEM_ERROR]: Player '{username}' not found or not online", ConsoleColor.Red);
                            WriteConsole.WriteLine($"  ℹ️  Player must be online to receive items", ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"  ℹ️  Use 'checkuser {username}' to verify username/nickname", ConsoleColor.Yellow);
                            break;
                        }

                        try
                        {
                            WriteConsole.WriteLine($"[ADD_ITEM]: Adding item to player...", ConsoleColor.Cyan);
                            WriteConsole.WriteLine($"  • Player: {targetPlayer.GetNickname} (Username: {targetPlayer.GetLogin}, UID: {targetPlayer.GetUID})", ConsoleColor.White);
                            WriteConsole.WriteLine($"  • TypeID: {typeId}", ConsoleColor.White);
                            WriteConsole.WriteLine($"  • Quantity: {quantity}", ConsoleColor.White);

                            var itemAddData = new Client.Inventory.Data.AddItem
                            {
                                ItemIffId = typeId,
                                Quantity = quantity,
                                Transaction = true,
                                Day = 0
                            };

                            var result = targetPlayer.AddItem(itemAddData);

                            if (result.Status)
                            {
                                targetPlayer.SendTransaction();
                                
                                WriteConsole.WriteLine($"[ADD_ITEM_SUCCESS]: Item added successfully!", ConsoleColor.Green);
                                WriteConsole.WriteLine($"  ✓ Player: {targetPlayer.GetNickname} (UID: {targetPlayer.GetUID})", ConsoleColor.Cyan);
                                WriteConsole.WriteLine($"  ✓ Item Index: {result.ItemIndex}", ConsoleColor.Cyan);
                                WriteConsole.WriteLine($"  ✓ TypeID: {result.ItemTypeID}", ConsoleColor.Cyan);
                                WriteConsole.WriteLine($"  ✓ Old Qty: {result.ItemOldQty} → New Qty: {result.ItemNewQty}", ConsoleColor.Cyan);
                                
                                targetPlayer.Response.Clear();
                                targetPlayer.Response.Write(new byte[] { 0x42, 0x00 });
                                targetPlayer.Response.WritePStr($"[GM] You received item TypeID: {typeId} x{quantity}");
                                targetPlayer.SendResponse();
                            }
                            else
                            {
                                WriteConsole.WriteLine($"[ADD_ITEM_ERROR]: Failed to add item", ConsoleColor.Red);
                                WriteConsole.WriteLine($"  ℹ️  Item might not exist in IFF files or inventory is full", ConsoleColor.Yellow);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteConsole.WriteLine($"[ADD_ITEM_ERROR]: Exception occurred: {ex.Message}", ConsoleColor.Red);
                            if (ex.InnerException != null)
                            {
                                WriteConsole.WriteLine($"[ADD_ITEM_ERROR]: Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                            }
                        }
                    }
                    break;
                case "daily":
                    {
                        WriteConsole.WriteLine("╔═══════════════════════════════════════════════════════════╗", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("║         DAILY REWARD SYSTEM STATUS                       ║", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("║  Status          : ✅ FULLY OPERATIONAL                  ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║  Database Type   : MySQL/MariaDB                          ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║  Mode            : MySQL Native (No Stored Procedures)   ║", ConsoleColor.White);
                        WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("║  Features:                                                ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    ✅ Daily login rewards (10-day cycle)                 ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    ✅ Consecutive day tracking                           ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    ✅ Automatic reward via Mail                          ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    ✅ Streak reset after missing a day                   ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    ✅ Prevents duplicate claims                          ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("║  Reward Schedule (10-Day Cycle):                         ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 1  → Pang Pouch 10k (10,000 Pang)                 ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 2  → Power Potion x50                             ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 3  → Control Potion x50                           ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 4  → Pang Pouch 100 (100 Pang)                    ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 5  → Spin Potion x30                              ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 6  → Curve Potion x30                             ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 7  → Comet Ball x10                               ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 8  → Power Potion+ x20                            ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 9  → Control Potion+ x20                          ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    Day 10 → Rare Pang Pouch x50 (BONUS!)                 ║", ConsoleColor.Yellow);
                        WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("║  How It Works:                                            ║", ConsoleColor.White);
                        WriteConsole.WriteLine("║    • Login daily to receive rewards via Mail 📧          ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    • Rewards are sent automatically when entering lobby  ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    • Check your mailbox to claim items!                  ║", ConsoleColor.Green);
                        WriteConsole.WriteLine("║    • Missing a day resets your streak to Day 1           ║", ConsoleColor.Yellow);
                        WriteConsole.WriteLine("║    • After Day 10, cycle restarts from Day 1             ║", ConsoleColor.Cyan);
                        WriteConsole.WriteLine("╚═══════════════════════════════════════════════════════════╝", ConsoleColor.Cyan);

                        // Show current statistics
                        try
                        {
                            using (var _db = DbContextFactory.Create())
                            {
                                var totalRewards = _db.Database.SqlQuery<int>(
                                    "SELECT COUNT(*) FROM pangya_daily_login_log"
                                ).FirstOrDefault();

                                var todayRewards = _db.Database.SqlQuery<int>(
                                    "SELECT COUNT(*) FROM pangya_daily_login_log WHERE LoginDate = CURDATE()"
                                ).FirstOrDefault();

                                WriteConsole.WriteLine($"\n[DAILY_STATS]: Total rewards given: {totalRewards}", ConsoleColor.Cyan);
                                WriteConsole.WriteLine($"[DAILY_STATS]: Rewards given today: {todayRewards}", ConsoleColor.Cyan);
                            }
                        }
                        catch { }
                    }
                    break;
                case "del":
                    {
                        if (string.IsNullOrEmpty(ReadCommand))
                        {
                            WriteConsole.WriteLine("[DELETE_USER]: Usage: del username [ok]", ConsoleColor.Yellow);
                            WriteConsole.WriteLine("  ⚠️  WARNING: This will permanently delete ALL user data!", ConsoleColor.Red);
                            WriteConsole.WriteLine("  Example: del test", ConsoleColor.Gray);
                            WriteConsole.WriteLine("  Example: del test ok", ConsoleColor.Gray);
                            break;
                        }

                        var cmdParts = ReadCommand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        string username = cmdParts[0].Trim();
                        bool isConfirmed = cmdParts.Length >= 2 && cmdParts[1].ToLower() == "ok";

                        if (!isConfirmed)
                        {
                            try
                            {
                                using (var _db = DbContextFactory.Create())
                                {
                                    var memberSql = "SELECT UID, Username, Nickname FROM pangya_member WHERE Username = @p0 LIMIT 1";
                                    var member = _db.Database.SqlQuery<MemberSimple>(memberSql, username).FirstOrDefault();
                                    
                                    if (member == null)
                                    {
                                        WriteConsole.WriteLine($"[DELETE_USER_ERROR]: User '{username}' does not exist", ConsoleColor.Red);
                                        break;
                                    }

                                    int uid = member.UID;
                                    
                                    WriteConsole.WriteLine("╔═══════════════════════════════════════════════════════════╗", ConsoleColor.Red);
                                    WriteConsole.WriteLine("║               ⚠️  CONFIRM USER DELETION  ⚠️               ║", ConsoleColor.Red);
                                    WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Red);
                                    WriteConsole.WriteLine($"║  Username    : {member.Username,-43} ║", ConsoleColor.White);
                                    WriteConsole.WriteLine($"║  Nickname    : {(string.IsNullOrEmpty(member.Nickname) ? "(none)" : member.Nickname),-43} ║", ConsoleColor.White);
                                    WriteConsole.WriteLine($"║  UID         : {uid,-43} ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Red);
                                    WriteConsole.WriteLine("║  This will DELETE ALL data from these tables:            ║", ConsoleColor.Yellow);
                                    WriteConsole.WriteLine("║    • pangya_member (Account)                              ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_personal (Cookie, Locker)                     ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_user_statistics (Stats, Pang, Level)          ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_user_matchhistory (Match History)            ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_character (All Characters)                    ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • Pangya_Caddie (All Caddies)                          ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_warehouse (All Items)                         ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_club_info (Club Upgrades)                     ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • Pangya_User_Equip (Toolbar)                          ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • Pangya_Game_Macro (Macros)                           ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • Pangya_Mail (Mail)                                   ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("║    • pangya_daily_login_log (Daily Login History)        ║", ConsoleColor.White);
                                    WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Red);
                                    WriteConsole.WriteLine("║  ⚠️  THIS ACTION CANNOT BE UNDONE! ⚠️                     ║", ConsoleColor.Red);
                                    WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Red);
                                    WriteConsole.WriteLine("║  Type the following command to proceed:                  ║", ConsoleColor.Yellow);
                                    WriteConsole.WriteLine($"║  del {username} ok                               ", ConsoleColor.Cyan);
                                    WriteConsole.WriteLine("╚═══════════════════════════════════════════════════════════╝", ConsoleColor.Red);
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteConsole.WriteLine($"[DELETE_USER_ERROR]: {ex.Message}", ConsoleColor.Red);
                            }
                            break;
                        }

                        try
                        {
                            using (var _db = DbContextFactory.Create())
                            {
                                var memberSql = "SELECT UID, Username, Nickname FROM pangya_member WHERE Username = @p0 LIMIT 1";
                                var member = _db.Database.SqlQuery<MemberSimple>(memberSql, username).FirstOrDefault();
                                
                                if (member == null)
                                {
                                    WriteConsole.WriteLine($"[DELETE_USER_ERROR]: User '{username}' does not exist", ConsoleColor.Red);
                                    break;
                                }

                                int uid = member.UID;
                                
                                var targetPlayer = (GPlayer)GetPlayerByUID((uint)uid);
                                if (targetPlayer != null)
                                {
                                    WriteConsole.WriteLine($"[DELETE_USER_WARNING]: User is currently ONLINE! Kicking first...", ConsoleColor.Yellow);
                                    DisconnectPlayer(targetPlayer);
                                    System.Threading.Thread.Sleep(1000);
                                }

                                WriteConsole.WriteLine("╔═══════════════════════════════════════════════════════════╗", ConsoleColor.Yellow);
                                WriteConsole.WriteLine("║           🗑️  DELETING USER DATA - PLEASE WAIT...        ║", ConsoleColor.Yellow);
                                WriteConsole.WriteLine("╚═══════════════════════════════════════════════════════════╝", ConsoleColor.Yellow);
                                
                                int totalDeleted = 0;

                                WriteConsole.WriteLine($"[DELETE_STEP_1]: Deleting Pangya_Mail...", ConsoleColor.Cyan);
                                try
                                {
                                    var mailRows = _db.Database.ExecuteSqlCommand($"DELETE FROM Pangya_Mail WHERE UID = {uid}");
                                    totalDeleted += mailRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {mailRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_2]: Deleting pangya_daily_login_log...", ConsoleColor.Cyan);
                                try
                                {
                                    var dailyRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_daily_login_log WHERE UID = {uid}");
                                    totalDeleted += dailyRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {dailyRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_3]: Deleting Pangya_Game_Macro...", ConsoleColor.Cyan);
                                try
                                {
                                    var macroRows = _db.Database.ExecuteSqlCommand($"DELETE FROM Pangya_Game_Macro WHERE UID = {uid}");
                                    totalDeleted += macroRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {macroRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_4]: Deleting Pangya_User_Equip...", ConsoleColor.Cyan);
                                try
                                {
                                    var equipRows = _db.Database.ExecuteSqlCommand($"DELETE FROM Pangya_User_Equip WHERE UID = {uid}");
                                    totalDeleted += equipRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {equipRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_5]: Deleting pangya_club_info (via Warehouse items)...", ConsoleColor.Cyan);
                                try
                                {
                                    var clubInfoRows = _db.Database.ExecuteSqlCommand(
                                        $@"DELETE FROM pangya_club_info 
                                           WHERE ITEM_ID IN (SELECT item_id FROM pangya_warehouse WHERE UID = {uid})");
                                    totalDeleted += clubInfoRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {clubInfoRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_6]: Deleting pangya_warehouse...", ConsoleColor.Cyan);
                                try
                                {
                                    var warehouseRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_warehouse WHERE UID = {uid}");
                                    totalDeleted += warehouseRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {warehouseRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_7]: Deleting pangya_caddie...", ConsoleColor.Cyan);
                                try
                                {
                                    var caddieRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_caddie WHERE UID = {uid}");
                                    totalDeleted += caddieRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {caddieRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_8]: Deleting pangya_character...", ConsoleColor.Cyan);
                                try
                                {
                                    var characterRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_character WHERE UID = {uid}");
                                    totalDeleted += characterRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {characterRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_9]: Deleting pangya_user_matchhistory...", ConsoleColor.Cyan);
                                try
                                {
                                    var matchHistoryRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_user_matchhistory WHERE UID = {uid}");
                                    totalDeleted += matchHistoryRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {matchHistoryRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_10]: Deleting pangya_user_statistics...", ConsoleColor.Cyan);
                                try
                                {
                                    var statsRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_user_statistics WHERE UID = {uid}");
                                    totalDeleted += statsRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {statsRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_11]: Deleting pangya_personal...", ConsoleColor.Cyan);
                                try
                                {
                                    var personalRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_personal WHERE UID = {uid}");
                                    totalDeleted += personalRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {personalRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine($"[DELETE_STEP_12]: Deleting pangya_member (FINAL)...", ConsoleColor.Cyan);
                                try
                                {
                                    var memberRows = _db.Database.ExecuteSqlCommand($"DELETE FROM pangya_member WHERE UID = {uid}");
                                    totalDeleted += memberRows;
                                    WriteConsole.WriteLine($"  ✓ Deleted {memberRows} row(s)", ConsoleColor.Green);
                                }
                                catch { WriteConsole.WriteLine($"  ⚠ Table may not exist or no data", ConsoleColor.Yellow); }

                                WriteConsole.WriteLine("\n╔═══════════════════════════════════════════════════════════╗", ConsoleColor.Green);
                                WriteConsole.WriteLine("║          🎉 USER DELETION COMPLETED SUCCESSFULLY!         ║", ConsoleColor.Green);
                                WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Green);
                                WriteConsole.WriteLine($"║  Username    : {member.Username,-43} ║", ConsoleColor.White);
                                WriteConsole.WriteLine($"║  Nickname    : {(string.IsNullOrEmpty(member.Nickname) ? "(none)" : member.Nickname),-43} ║", ConsoleColor.White);
                                WriteConsole.WriteLine($"║  UID         : {uid,-43} ║", ConsoleColor.White);
                                WriteConsole.WriteLine($"║  Total Rows  : {totalDeleted,-43} ║", ConsoleColor.Cyan);
                                WriteConsole.WriteLine("╠═══════════════════════════════════════════════════════════╣", ConsoleColor.Green);
                                WriteConsole.WriteLine("║  ✓ All user data has been permanently removed             ║", ConsoleColor.Green);
                                WriteConsole.WriteLine("║  ✓ User can now create a new account with same username  ║", ConsoleColor.Green);
                                WriteConsole.WriteLine("╚═══════════════════════════════════════════════════════════╝", ConsoleColor.Green);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteConsole.WriteLine($"\n[DELETE_USER_ERROR]: Failed to delete user!", ConsoleColor.Red);
                            WriteConsole.WriteLine($"[DELETE_USER_ERROR]: {ex.Message}", ConsoleColor.Red);
                            if (ex.InnerException != null)
                            {
                                WriteConsole.WriteLine($"[DELETE_USER_ERROR]: Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                            }
                        }
                    }
                    break;
                default:
                    {
                        WriteConsole.WriteLine("[SYSTEM_COMMAND]: Sorry Unknown Command, type 'help' to get the list of commands", ConsoleColor.Red);
                    }
                    break;
            }
        }

        public Channel GetLobbyByID(byte ID)
        {
            foreach (var lobby in LobbyList)
            {
                if (lobby.Id == ID)
                {
                    return lobby;
                }
            }
            return null;
        }
        public Channel GetLobbyByName(string Name)
        {
            foreach (var lobby in LobbyList)
            {
                if (lobby.Name == Name)
                {
                    return lobby;
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void SendTarget(byte ID, byte[] Data)
        {
            foreach (GPlayer Client in Players.Model)
            {
                if (Client.Lobby.Id == ID)
                {
                    Client.SendResponse(Data);
                }
            }
        }
      
        private void ShowHelp()
        {
            Console.WriteLine();
            WriteConsole.WriteLine("╔═══════════════════════════════════════════════════╗", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║         GAME SERVER - Command List                ║", ConsoleColor.Cyan);
            WriteConsole.WriteLine("╠═══════════════════════════════════════════════════╣", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║  help             - Show all commands             ║", ConsoleColor.White);
            WriteConsole.WriteLine("║  clear/cls        - Clear console screen          ║", ConsoleColor.White);
            WriteConsole.WriteLine("║  lobby            - Show Lobby/Channel list       ║", ConsoleColor.White);
            WriteConsole.WriteLine("║  reload           - Reload server configuration   ║", ConsoleColor.White);
            WriteConsole.WriteLine("║  topnotice <msg>  - Send notice to all players   ║", ConsoleColor.White);
            WriteConsole.WriteLine("║  daily            - Show daily reward status      ║", ConsoleColor.White);
            WriteConsole.WriteLine("╠═══════════════════════════════════════════════════╣", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║  kickuid <uid>    - Kick player by UID            ║", ConsoleColor.Yellow);
            WriteConsole.WriteLine("║  kickname <name>  - Kick player by Nickname       ║", ConsoleColor.Yellow);
            WriteConsole.WriteLine("║  kickuser <user>  - Kick player by Username       ║", ConsoleColor.Yellow);
            WriteConsole.WriteLine("╠═══════════════════════════════════════════════════╣", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║  add <user> <pass> - Add new account (simple)    ║", ConsoleColor.Green);
            WriteConsole.WriteLine("║                      Example: add test 1234       ║", ConsoleColor.Gray);
            WriteConsole.WriteLine("║  checkuser <user>  - Check account information   ║", ConsoleColor.Green);
            WriteConsole.WriteLine("║  fix <user>        - Fix account issues           ║", ConsoleColor.Green);
            WriteConsole.WriteLine("║  deleteuser <user> [confirm] - Delete user data  ║", ConsoleColor.Red);
            WriteConsole.WriteLine("║                      Example: deleteuser test     ║", ConsoleColor.Gray);
            WriteConsole.WriteLine("║                      ⚠️ PERMANENT! Use 'confirm'  ║", ConsoleColor.Red);
            WriteConsole.WriteLine("╠═══════════════════════════════════════════════════╣", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║  setlevel <nick> <lvl> - Set player level (1-100)║", ConsoleColor.Magenta);
            WriteConsole.WriteLine("║                      Example: setlevel Test 50    ║", ConsoleColor.Gray);
            WriteConsole.WriteLine("║  setmastery <nick> <pts> - Set mastery points    ║", ConsoleColor.Magenta);
            WriteConsole.WriteLine("║                      Example: setmastery Test 10  ║", ConsoleColor.Gray);
            WriteConsole.WriteLine("╠═══════════════════════════════════════════════════╣", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║  additem <user> <id> [qty] - Give item to player ║", ConsoleColor.Cyan);
            WriteConsole.WriteLine("║                      Example: additem bdm 335544320 10   ║", ConsoleColor.Gray);
            WriteConsole.WriteLine("║                      Example: additem bdm 268435456       ║", ConsoleColor.Gray);
            WriteConsole.WriteLine("║                      Use USERNAME (not Nickname)  ║", ConsoleColor.DarkGray);
            WriteConsole.WriteLine("║                      Player must be ONLINE        ║", ConsoleColor.DarkGray);
            WriteConsole.WriteLine("╚═══════════════════════════════════════════════════╝", ConsoleColor.Cyan);
            Console.WriteLine();
        }
    }
}
