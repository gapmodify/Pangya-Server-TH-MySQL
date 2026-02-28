using PangyaAPI;
using PangyaAPI.Auth;
using PangyaAPI.PangyaClient;
using PangyaAPI.Server;
using PangyaAPI.Tools;
using Connector.DataBase;
using Connector.Table;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace Login.MainServer
{
    public class TestDbMemberResult
    {
        public int UID { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public int IDState { get; set; }
        public long FirstSet { get; set; }  // Changed from byte to long for MySQL
        public long Logon { get; set; }     // Changed from byte to long for MySQL
    }

    public class LoginServer : TcpServer
    {
        public LoginServer()
        {
            try
            {
                DatabaseConfig.Initialize("Login.ini");
                var Ini = new IniFile(ConfigurationManager.AppSettings["Config"]);
                Data = new ServerSettings
                {
                    Name = Ini.ReadString("Config", "Name", "LoginServer"),
                    Version = Ini.ReadString("Config", "Version", "SV_LS_Release_2.0"),
                    UID = Ini.ReadUInt32("Config", "UID", 10201),  // ✅ แก้เป็น 10201
                    MaxPlayers = Ini.ReadUInt32("Config", "MaxPlayers", 3000),
                    Port = Ini.ReadUInt32("Config", "Port", 10201),  // ✅ แก้เป็น 10201
                    IP = Ini.ReadString("Config", "IP", "127.0.0.1"),                    
                    Type = AuthClientTypeEnum.LoginServer,
                    AuthServer_Ip = Ini.ReadString("Config", "AuthServer_IP", "127.0.0.1"),
                    AuthServer_Port = Ini.ReadInt32("Config", "AuthServer_Port", 7997),
                    Key = "3493ef7ca4d69f54de682bee58be4f93"
                };
                ShowLog = Ini.ReadBool("Config", "PacketLog", false);
                
                Console.Title = $"Pangya Fresh Up! LoginServer - Players: {Players.Count}";
                
                if (ConnectToAuthServer(AuthServerConstructor()) == false)
                {
                    WriteConsole.WriteLine("[ERROR_START_AUTH]: Could not connect to AuthServer");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                _server = new TcpListener(IPAddress.Parse(Data.IP), (int)Data.Port);

                OpenServer = true; // ✅ เปิดเซิร์ฟเวอร์ตั้งแต่เริ่มต้น
            }
            catch (Exception erro)
            {
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
                this._isRunning = true;
                _server.Start((int)Data.MaxPlayers);
                WriteConsole.WriteLine($"[SERVER_START]: PORT {Data.Port}", ConsoleColor.Green);
                WriteConsole.WriteLine($"[SERVER_STATUS]: Server is open (OpenServer = {OpenServer})", ConsoleColor.Green);
                //Inicia Thread para escuta de clientes
                var WaitConnectionsThread = new Thread(new ThreadStart(HandleWaitConnections));
                WaitConnectionsThread.Start();
            }
            catch (Exception erro)
            {
                WriteConsole.WriteLine($"[ERROR_START]: {erro.Message}");
            }
        }
        protected override Player OnConnectPlayer(TcpClient tcp)
        {
            WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
            WriteConsole.WriteLine("[CONNECT_DEBUG] New client connection attempt...", ConsoleColor.Magenta);
            
            try
            {
                var player = new LPlayer(tcp)
                {
                    Server = this,
                    ConnectionID = NextConnectionId
                };

                NextConnectionId += 1;

                WriteConsole.WriteLine($"[CONNECT_DEBUG] ✓ LPlayer created, ConnectionID: {player.ConnectionID}", ConsoleColor.Green);

                SendKey(player);

                WriteConsole.WriteLine($"[PLAYER_CONNECT]: {player.GetAddress}:{player.GetPort} | KEY: {player.GetKey}", ConsoleColor.Green);
                
                Players.Add(player);
                UpdateServer();
                Console.Title = $"Pangya Fresh Up! LoginServer - Players: {Players.Count}";
                
                WriteConsole.WriteLine($"[CONNECT_DEBUG] ✓ Player added to list, Total: {Players.Count}", ConsoleColor.Green);
                WriteConsole.WriteLine("[CONNECT_DEBUG] ✓ Connection successful! Waiting for login packet...", ConsoleColor.Green);
                WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
               
                return player;
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine("[CONNECT_DEBUG] ✗✗✗ ERROR in OnConnectPlayer ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[CONNECT_DEBUG] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[CONNECT_DEBUG] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                throw;
            }
        }

        protected override AuthClient AuthServerConstructor()
        {
            return new AuthClient(Data);
        }

        protected override void SendKey(Player player)
        {
            var US = new byte[] { 0x00, 0x0B, 0x00, 0x00, 0x00, 0x00, player.GetKey, 0x00, 0x00, 0x00, 0x75, 0x27, 0x00, 0x00 };

            if (player.Tcp.Connected)
                //Envia packet com a chave
                player.SendBytes(US);
        }

        public override void DisconnectPlayer(Player Player)
        {
            var player = (LPlayer)Player;
            if (player.Connected)
            {
                player.Connected = false;
                
                // Reset Logon status to 0 when player disconnects
                if (player.GetUID > 0)
                {
                    try
                    {
                        using (var dbMember = new DB_pangya_member())
                        {
                            dbMember.UpdateLogonStatus((int)player.GetUID, 0);
                            WriteConsole.WriteLine($"[PLAYER_DISCONNECT] ✓ Reset Logon=0 for UID:{player.GetUID}", ConsoleColor.Yellow);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteConsole.WriteLine($"[PLAYER_DISCONNECT] ⚠ Could not reset Logon: {ex.Message}", ConsoleColor.Yellow);
                    }
                }
                
                player.Dispose();
                player.Tcp.Close();

                Players.Remove(player);
            }
            WriteConsole.WriteLine($"[PLAYER_DISCONNECT]: User {player?.GetLogin} disconnected");
            UpdateServer();
            Console.Title = $"Pangya Fresh Up! LoginServer - Players: {Players.Count}";
        }

        protected override void ServerExpection(Player Client, Exception Ex)
        {
            var player = (LPlayer)Client;
            
            if (player.Connected)
            {
                //AuthServer.Send(new AuthPacket() { ID = AuthPacketEnum.DISCONNECT_PLAYER_ALL_ON_SERVERS, Message = new { ID = player.GetUID } });
            }
        }

        public override Player GetClientByConnectionId(uint ConnectionId)
        {
            var Client = (LPlayer)Players.Model.Where(c => c.ConnectionID == ConnectionId).FirstOrDefault();

            return Client;
        }

        public override Player GetPlayerByNickname(string Nickname)
        {
            var Client = (LPlayer)Players.Model.Where(c => c.GetNickname == Nickname).FirstOrDefault();

            return Client;
        }

        public override Player GetPlayerByUsername(string Username)
        {
            var Client = (LPlayer)Players.Model.Where(c => c.GetLogin == Username).FirstOrDefault();

            return Client;
        }

        public override Player GetPlayerByUID(uint UID)
        {
            var Client = (LPlayer)Players.Model.Where(c => c.GetUID == UID).FirstOrDefault();

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
            switch (packet.ID)
            {
                case AuthPacketEnum.SERVER_KEEPALIVE: //KeepAlive
                    {
                    }
                    break;
                case AuthPacketEnum.SERVER_CONNECT:
                    {
                        bool result = packet.Message.Success;
                        if (result)
                        {
                            WriteConsole.WriteLine($"[SERVER_CONNECT]: Connected successfully!", System.ConsoleColor.Green);
                        }
                        else
                        {
                            WriteConsole.WriteLine($"[SERVER_CONNECT]: Connection failed!", System.ConsoleColor.Red);
                        }
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
                    }
                    break;
                case AuthPacketEnum.SERVER_RELEASE_BOXRANDOM:
                    break;
                case AuthPacketEnum.SERVER_RELEASE_NOTICE:
                    {
                        string message = packet.Message.mensagem;
                        this.Notice(message);

                        WriteConsole.WriteLine($"[SERVER_RELEASE_NOTICE]: {message}!");
                    }
                    break;
                case AuthPacketEnum.SERVER_COMMAND:
                default:
                    WriteConsole.WriteLine("[AUTH_PACKET]:  " + packet.ID);
                    break;
            }
        }

        public override void RunCommand(string[] Command)
        {
            string ReadCommand;
            LPlayer P;

            if (Command.Length > 1)
            {
                ReadCommand = Command[1];
            }
            else
            {
                ReadCommand = "";
            }
            switch (Command[0])
            {
                case "cls":
                case "limpar":
                case "clear":
                    {
                        Console.Clear();
                    }
                    break;
                case "resdb":
                case "resetdb":
                    {
                        WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Yellow);
                        WriteConsole.WriteLine("[RESET_DB] ⚠️ WARNING: This will DELETE ALL PLAYER DATA!", ConsoleColor.Red);
                        WriteConsole.WriteLine("[RESET_DB] Only 'pangya_item_daily' will be preserved.", ConsoleColor.Yellow);
                        WriteConsole.WriteLine("[RESET_DB] Type 'YES' to confirm:", ConsoleColor.Yellow);
                        WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Yellow);
                        
                        string confirm = Console.ReadLine();
                        
                        if (confirm?.ToUpper() == "YES")
                        {
                            try
                            {
                                WriteConsole.WriteLine("[RESET_DB] ⚡ Executing database reset...", ConsoleColor.Cyan);
                                
                                using (var db = DbContextFactory.Create())
                                {
                                    // Disable foreign key checks
                                    db.Database.ExecuteSqlCommand("SET FOREIGN_KEY_CHECKS = 0");
                                    
                                    // Delete player data tables
                                    WriteConsole.WriteLine("[RESET_DB] → Deleting player data...", ConsoleColor.Yellow);
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_member");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_user_statistics");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_personal");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_user_equip");
                                    
                                    // Delete character & equipment
                                    WriteConsole.WriteLine("[RESET_DB] → Deleting characters & equipment...", ConsoleColor.Yellow);
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_character");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_caddie");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_mascot");
                                    
                                    // Delete inventory
                                    WriteConsole.WriteLine("[RESET_DB] → Deleting inventory...", ConsoleColor.Yellow);
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_warehouse");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_club_info");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_card");
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_card_equip");
                                    
                                    // Delete game data
                                    WriteConsole.WriteLine("[RESET_DB] → Deleting game data...", ConsoleColor.Yellow);
                                    db.Database.ExecuteSqlCommand("DELETE FROM pangya_game_macro");
                                    
                                    // Reset AUTO_INCREMENT
                                    WriteConsole.WriteLine("[RESET_DB] → Resetting AUTO_INCREMENT...", ConsoleColor.Yellow);
                                    db.Database.ExecuteSqlCommand("ALTER TABLE pangya_member AUTO_INCREMENT = 1");
                                    db.Database.ExecuteSqlCommand("ALTER TABLE pangya_character AUTO_INCREMENT = 1");
                                    db.Database.ExecuteSqlCommand("ALTER TABLE pangya_caddie AUTO_INCREMENT = 1");
                                    db.Database.ExecuteSqlCommand("ALTER TABLE pangya_mascot AUTO_INCREMENT = 1");
                                    db.Database.ExecuteSqlCommand("ALTER TABLE pangya_warehouse AUTO_INCREMENT = 14");
                                    db.Database.ExecuteSqlCommand("ALTER TABLE pangya_card AUTO_INCREMENT = 1");
                                    
                                    // Re-enable foreign key checks
                                    db.Database.ExecuteSqlCommand("SET FOREIGN_KEY_CHECKS = 1");
                                    
                                    // Verify
                                    var memberCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM pangya_member").FirstOrDefault();
                                    var dailyItemCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM pangya_item_daily").FirstOrDefault();
                                    
                                    WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Green);
                                    WriteConsole.WriteLine("[RESET_DB] ✅ Database reset complete!", ConsoleColor.Green);
                                    WriteConsole.WriteLine($"[RESET_DB] → Members: {memberCount} (should be 0)", ConsoleColor.White);
                                    WriteConsole.WriteLine($"[RESET_DB] → Daily Items: {dailyItemCount} (preserved)", ConsoleColor.Cyan);
                                    WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Green);
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Red);
                                WriteConsole.WriteLine("[RESET_DB] ✗ Error during reset!", ConsoleColor.Red);
                                WriteConsole.WriteLine($"[RESET_DB] Error: {ex.Message}", ConsoleColor.Red);
                                if (ex.InnerException != null)
                                {
                                    WriteConsole.WriteLine($"[RESET_DB] Inner: {ex.InnerException.Message}", ConsoleColor.Yellow);
                                }
                                WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Red);
                            }
                        }
                        else
                        {
                            WriteConsole.WriteLine("[RESET_DB] ✗ Operation cancelled.", ConsoleColor.Yellow);
                        }
                    }
                    break;
                case "kickuid":
                    {
                        P = (LPlayer)GetPlayerByUID(uint.Parse(ReadCommand.ToString()));
                        if (P == null)
                        {
                            WriteConsole.WriteLine("[SYSTEM_COMMAND]: This UID is not online!", ConsoleColor.Red);
                            break;
                        }
                        DisconnectPlayer(P);
                    }
                    break;
                case "kickname":
                    {
                        P = (LPlayer)GetPlayerByNickname(ReadCommand);
                        if (P == null)
                        {
                            WriteConsole.WriteLine("[SYSTEM_COMMAND]: This nickname is not online!", ConsoleColor.Red);
                            return;
                        }
                        DisconnectPlayer(P);
                    }
                    break;
                case "kickuser":
                    {
                        P = (LPlayer)GetPlayerByUsername(ReadCommand);
                        if (P == null)
                        {
                            WriteConsole.WriteLine("[SYSTEM_COMMAND]: This username is not online!", ConsoleColor.Red);
                            return;
                        }
                        DisconnectPlayer(P);
                    }
                    break;
                case "topnotice":
                    {
                        if (ReadCommand.Length > 0)
                            Notice(ReadCommand);
                    }
                    break;
                case "stop":
                    OpenServer = false;
                    WriteConsole.WriteLine("[SYSTEM_COMMAND]: Server is now closed", ConsoleColor.Yellow);
                    break;
                case "start":
                    OpenServer = true;
                    WriteConsole.WriteLine("[SYSTEM_COMMAND]: Server is now open", ConsoleColor.Green);
                    break;
                case "comandos":
                case "commands":
                case "ajuda":
                case "help":
                    {
                        ShowHelp();
                    }
                    break;
                default:
                    {
                        WriteConsole.WriteLine("[SYSTEM_COMMAND]: Unknown command, type 'help' to see command list", ConsoleColor.Red);
                    }
                    break;
            }
        }
        
        private void ShowHelp()
        {
            WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            WriteConsole.WriteLine("  LoginServer Commands", ConsoleColor.Cyan);
            WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
            WriteConsole.WriteLine("  clear/cls         - Clear console", ConsoleColor.White);
            WriteConsole.WriteLine("  resdb/resetdb     - Reset database (keep daily items)", ConsoleColor.Yellow);
            WriteConsole.WriteLine("  kickuid <uid>     - Kick player by UID", ConsoleColor.White);
            WriteConsole.WriteLine("  kickname <nick>   - Kick player by nickname", ConsoleColor.White);
            WriteConsole.WriteLine("  kickuser <user>   - Kick player by username", ConsoleColor.White);
            WriteConsole.WriteLine("  topnotice <msg>   - Send notice to all players", ConsoleColor.White);
            WriteConsole.WriteLine("  stop              - Close server (reject new connections)", ConsoleColor.White);
            WriteConsole.WriteLine("  start             - Open server (accept connections)", ConsoleColor.White);
            WriteConsole.WriteLine("  help              - Show this help", ConsoleColor.White);
            WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Cyan);
        }
    }
}
