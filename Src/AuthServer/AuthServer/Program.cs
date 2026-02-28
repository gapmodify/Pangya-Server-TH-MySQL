using Connector.DataBase;
using Connector.Table;
using PangyaAPI.Auth;
using PangyaAPI.Server;
using PangyaAPI.Tools;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace AuthServer
{
    internal class Program
    {
        public static AuthServer Server;

        public static string AuthKey { get; set; }

        private static void Main()
        {
            Console.Title = "Pangya Fresh Up! AuthServer - Starting...";

            try
            {
                // ✅ สร้าง Auth.ini อัตโนมัติถ้ายังไม่มี (AuthServer รันก่อนสุด)
                DatabaseConfig.Initialize("Auth.ini");

                Console.WriteLine($"[SERVER_INIT]: Database engine = {DatabaseConfig.GetDbEngine().ToUpper()}");

                // ทดสอบการเชื่อมต่อ
                using (var testDb = DbContextFactory.Create())
                {
                    testDb.Database.Connection.Open();
                    Console.WriteLine($"[DB_SUCCESS]: Connected to database using {testDb.Database.Connection.GetType().Name}");
                    testDb.Database.Connection.Close();
                }

                Console.WriteLine("[DB_READY]: Database connection verified");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DB_ERROR]: Failed to connect to database");
                Console.WriteLine($"[DB_ERROR]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DB_ERROR]: Inner: {ex.InnerException.Message}");
                }
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.Title = "Pangya Fresh Up! AuthServer";

            AuthKey = "3493ef7ca4d69f54de682bee58be4f93";

            Server = new AuthServer();
            Server.Start();
            Server.OnPacketReceived += TcpServer_OnPacketReceived;

            // ✅ ใช้ค่าจาก Server.Data ที่อ่านมาแล้ว แทนที่จะอ่านใหม่
            try
            {
                int serverUID = (int)Server.Data.UID;
                string serverName = Server.Data.Name;
                string serverIP = Server.Data.IP;
                int serverPort = (int)Server.Data.Port;
                int maxPlayers = (int)Server.Data.MaxPlayers;

                WriteConsole.WriteLine($"[SERVER_INIT]: Registering AuthServer to database...", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[SERVER_INIT]: UID={serverUID}, Port={serverPort}, MaxPlayers={maxPlayers}", ConsoleColor.Cyan);

                using (DB_pangya_server dbServer = new DB_pangya_server())
                {
                    if (!dbServer.ExistsByServerID(serverUID))
                    {
                        // Insert new entry
                        var serverData = new ServerData
                        {
                            ServerID = serverUID,
                            Name = serverName,
                            IP = serverIP,
                            Port = serverPort,
                            MaxUser = maxPlayers,
                            UsersOnline = 0,
                            Property = 0,
                            BlockFunc = 0,
                            ImgNo = 0,
                            ImgEvent = 0,
                            ServerType = 3,
                            Active = 1
                        };
                        dbServer.Insert(serverData);
                        WriteConsole.WriteLine($"[SERVER_INIT]: ✓ AuthServer registered in pangya_server (UID:{serverUID}, Port:{serverPort})", ConsoleColor.Green);
                    }
                    else
                    {
                        // Update existing entry
                        dbServer.UpdateActive(serverUID, 1);
                        var serverData = dbServer.SelectByServerID(serverUID);
                        serverData.Name = serverName;
                        serverData.IP = serverIP;
                        serverData.Port = serverPort;
                        serverData.MaxUser = maxPlayers;
                        serverData.UsersOnline = 0;
                        dbServer.Update(serverData);
                        WriteConsole.WriteLine($"[SERVER_INIT]: ✓ AuthServer updated in pangya_server (UID:{serverUID}, Port:{serverPort})", ConsoleColor.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[SERVER_INIT]: ⚠ Could not update pangya_server: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("[SERVER_READY]: AuthServer is ready to accept connections");

            for (; ; )
            {
                var comando = Console.ReadLine()?.Split(new char[] { ' ' }, 2);
                if (comando == null || comando.Length == 0) continue;

                switch (comando[0].ToLower())
                {
                    case "": break;
                    case "notice":
                        if (comando.Length > 1)
                        {
                            Server.Send(AuthClientTypeEnum.GameServer, new AuthPacket()
                            {
                                ID = AuthPacketEnum.SERVER_RELEASE_NOTICE,
                                Message = new { mensagem = comando[1] }
                            });
                        }
                        break;

                    case "ticket":
                        if (comando.Length > 1)
                        {
                            Server.Send(AuthClientTypeEnum.GameServer, new AuthPacket()
                            {
                                ID = AuthPacketEnum.SERVER_RELEASE_TICKET,
                                Message = new { GetNickName = "ADMIN", GetMessage = comando[1] }
                            });
                        }
                        break;

                    case "quit":
                        Console.WriteLine("Server has been stopped!");
                        Environment.Exit(1);
                        break;

                    case "limpar":
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;

                    default:
                        Console.WriteLine("Invalid command");
                        break;
                }
            }
        }

        private static void TcpServer_OnPacketReceived(AuthClient client, AuthPacket packet)
        {
            try
            {
                WriteConsole.WriteLine($"[SYNC_CALL_PACKET]: [{packet.ID}, {client.Data.Name}]", ConsoleColor.Cyan);

                switch (packet.ID)
                {
                    case AuthPacketEnum.SERVER_KEEPALIVE:
                        client.Send(new AuthPacket() { ID = AuthPacketEnum.SERVER_KEEPALIVE });
                        break;

                    case AuthPacketEnum.SERVER_CONNECT:
                        HandleServerConnect(client);
                        break;

                    case AuthPacketEnum.RECEIVES_USER_UID:
                        HandlePlayerUID(packet);
                        break;

                    case AuthPacketEnum.SERVER_UPDATE:
                        HandleServerUpdate(client, packet);
                        break;

                    case AuthPacketEnum.DISCONNECT_PLAYER_ALL_ON_SERVERS:
                        HandlePlayerDisconnect(packet);
                        break;

                    case AuthPacketEnum.SERVER_RELEASE_CHAT:
                        HandleChat(packet);
                        break;

                    case AuthPacketEnum.SERVER_RELEASE_TICKET:
                    case AuthPacketEnum.SERVER_RELEASE_BOXRANDOM:
                    case AuthPacketEnum.SERVER_RELEASE_NOTICE_GM:
                    case AuthPacketEnum.SERVER_RELEASE_NOTICE:
                        Server.Send(AuthClientTypeEnum.GameServer, packet);
                        break;

                    case AuthPacketEnum.LOGIN_RESULT:
                        HandleLogin(client, packet);
                        break;

                    default:
                        WriteConsole.WriteLine($"[SYNC_REQUEST_PACKET_UNK]: {packet.ID}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[PACKET_HANDLER_ERROR]: Packet={packet?.ID}, Client={client?.Data?.Name}");
                Console.WriteLine($"[PACKET_HANDLER_ERROR]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[PACKET_HANDLER_ERROR_INNER]: {ex.InnerException.Message}");
                }
                Console.WriteLine($"[PACKET_HANDLER_ERROR_STACK]: {ex.StackTrace}");
                Console.ResetColor();
            }
        }

        private static void HandleServerConnect(AuthClient client)
        {
            var response = new AuthPacket();

            if (client.Data.Key != AuthKey)
            {
                response.Message = new { Success = false, Exception = "คีย์ยืนยันตัวตนไม่ถูกต้อง" };
                Server.Send(client, response);
                Server.DisconnectClient(client);
            }
            else
            {
                response.Message = new { Success = true };
                Server.Send(client, response);
            }
        }

        private static void HandleServerUpdate(AuthClient client, AuthPacket packet)
        {
            client.Data = new ServerSettings()
            {
                UID = packet.Message._data.UID,
                Type = packet.Message._data.Type,
                AuthServer_Ip = packet.Message._data.AuthServer_Ip,
                AuthServer_Port = packet.Message._data.AuthServer_Port,
                Port = packet.Message._data.Port,
                MaxPlayers = packet.Message._data.MaxPlayers,
                IP = packet.Message._data.IP,
                Key = packet.Message._data.Key,
                Name = packet.Message._data.Name,
                BlockFunc = packet.Message._data.BlockFunc,
                EventFlag = packet.Message._data.EventFlag,
                GameVersion = packet.Message._data.GameVersion,
                ImgNo = packet.Message._data.ImgNo,
                Property = packet.Message._data.Property,
                Version = packet.Message._data.Version,
            };
            client.Data.Update();
        }

        private static void HandleChat(AuthPacket packet)
        {
            byte Type = packet.Message.IsGM;
            if (Type == 15 || Type == 4)
            {
                var response = new AuthPacket
                {
                    ID = AuthPacketEnum.SERVER_RELEASE_CHAT,
                    Message = new { PlayerNick = packet.Message.GetNickName, PlayerMessage = packet.Message.GetMessage }
                };
                Server.Send(AuthClientTypeEnum.GameServer, response);
            }
        }

        private static void HandleLogin(AuthClient client, AuthPacket packet)
        {
            int UID = packet.Message.ID;
            var check = Server.Players.Model.Where(c => c.GetUID == UID);

            if (check.Any())
            {
                var result = new AuthPacket()
                {
                    ID = AuthPacketEnum.PLAYER_LOGIN_RESULT,
                    Message = new { Type = LoginResultEnum.Sucess }
                };
                Server.Send(client, result);
            }
            else
            {
                var result = new AuthPacket()
                {
                    ID = AuthPacketEnum.PLAYER_LOGIN_RESULT,
                    Message = new { Type = LoginResultEnum.Error }
                };
                Server.Send(client, result);
                HandlePlayerDisconnect(packet);
            }
        }

        private static void HandlePlayerUID(AuthPacket packet)
        {
            int UID = packet.Message.ID;
            var check = Server.Players.Model.Where(c => c.GetUID == UID);

            if (check.Any())
            {
                Server.Players.Remove(check.First());
            }

            try
            {
                Console.WriteLine($"[PLAYER_UID_DEBUG]: Loading player data for UID: {UID}");

                using (var dbMember = new DB_pangya_member())
                {
                    var result = dbMember.SelectByUID(UID);

                    if (result != null)
                    {
                        Console.WriteLine($"[PLAYER_UID_SUCCESS]: Loaded player '{result.Username}' (UID:{result.UID}, Nickname:{result.Nickname})");

                        var player = new APlayer(null)
                        {
                            GetUID = (uint)result.UID,
                            GetLogin = result.Username,
                            GetNickname = result.Nickname
                        };
                        Server.Players.Add(player);
                        Console.WriteLine($"[PLAYER_UID_SUCCESS]: Player added to AuthServer player list");
                    }
                    else
                    {
                        Console.WriteLine($"[PLAYER_UID_WARNING]: No player found with UID: {UID}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[PLAYER_UID_ERROR]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[PLAYER_UID_ERROR_INNER]: {ex.InnerException.Message}");
                }
                Console.WriteLine($"[PLAYER_UID_ERROR_STACK]: {ex.StackTrace}");
                Console.ResetColor();
            }
        }

        private static void HandlePlayerDisconnect(AuthPacket packet)
        {
            int UID = packet.Message.ID;
            var check = Server.Players.Model.Where(c => c.GetUID == UID);

            if (check.Any())
            {
                Server.Players.Remove(check.First());
                Console.WriteLine($"[DISCONNECT_DEBUG]: Removed player UID:{UID} from AuthServer list");
            }

            try
            {
                Console.WriteLine($"[DISCONNECT_DEBUG]: Updating Logon status for UID:{UID}");

                using (DB_pangya_member dbMember = new DB_pangya_member())
                {
                    int rowsAffected = dbMember.UpdateLogonStatus(UID, 0);

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine($"[DISCONNECT_SUCCESS]: Set Logon=0 for UID:{UID}");
                        packet.ID = AuthPacketEnum.SEND_DISCONNECT_PLAYER;
                        Server.SendToAll(packet);
                    }
                    else
                    {
                        Console.WriteLine($"[DISCONNECT_WARNING]: No rows updated for UID:{UID}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DISCONNECT_ERROR]: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[DISCONNECT_ERROR_INNER]: {ex.InnerException.Message}");
                }
                Console.WriteLine($"[DISCONNECT_ERROR_STACK]: {ex.StackTrace}");
                Console.ResetColor();
            }
        }
    }

    // MemberData is now imported from Connector.Table.DB_pangya_member

    // Helper class to read Auth.ini
    internal class IniReader
    {
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);

        private string _filePath;

        public IniReader(string filename)
        {
            if (!System.IO.File.Exists(filename))
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
                if (!System.IO.File.Exists(fullPath))
                {
                    throw new Exception($"File not exist: {filename}");
                }
                _filePath = fullPath;
            }
            else
            {
                _filePath = System.IO.Path.GetFullPath(filename);
            }
            
            WriteConsole.WriteLine($"[INI_READER]: Loading from {_filePath}", ConsoleColor.Cyan);
        }

        public string ReadString(string section, string key, string defaultValue)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, sb, 255, _filePath);
            string result = sb.ToString();
            
            WriteConsole.WriteLine($"[INI_READER]: [{section}] {key} = '{result}'", ConsoleColor.DarkGray);
            
            return result;
        }

        public int ReadInt32(string section, string key, int defaultValue)
        {
            string value = ReadString(section, key, defaultValue.ToString());
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}