using Connector.DataBase;
using Game.Client;
using System.Linq;
using System;
using Game.MainServer;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class MessengerQueryResult
    {
        public string Name { get; set; }
        public int ServerID { get; set; }
        public int MaxUser { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
    }

    public class MessengerServerCoreSystem
    {
        public void PlayerCallMessengerServer(GPlayer PL)
        {
            // เช็คว่าเปิด Messenger Server หรือไม่
            if (Program._server.Messenger_Active)
            {
                WriteConsole.WriteLine($"[MESSENGER_CALL]: Messenger is ACTIVE - Sending packets to {PL.GetNickname}", ConsoleColor.Cyan);
                PL.SendResponse(new byte[] { 0xF1, 0x00, 0x00 });
                PL.SendResponse(new byte[] { 0x35, 0x01 });
            }
            else
            {
                WriteConsole.WriteLine($"[MESSENGER_CALL]: Messenger is DISABLED for {PL.GetNickname}", ConsoleColor.Yellow);
            }
        }

        public void PlayerConnectMessengerServer(GPlayer PL)
        {
            try
            {
                if (!Program._server.Messenger_Active)
                {
                    WriteConsole.WriteLine($"[MESSENGER_CONNECT]: Messenger disabled - Sending empty list", ConsoleColor.Yellow);
                    PL.Response.Write(new byte[] { 0xFC, 0x00, 0x00 }); // No messenger servers
                    PL.SendResponse();
                    return;
                }

                WriteConsole.WriteLine($"[MESSENGER_CONNECT]: Querying Messenger servers for {PL.GetNickname}", ConsoleColor.Cyan);

                using (var db = DbContextFactory.Create())
                {
                    var sql = @"
                        SELECT Name, ServerID, MaxUser, IP, Port
                        FROM pangya_server
                        WHERE ServerType = 'Messenger' AND Active = 1";

                    var server = db.Database.SqlQuery<MessengerQueryResult>(sql).ToList();

                    WriteConsole.WriteLine($"[MESSENGER_CONNECT]: Found {server.Count} Messenger server(s)", ConsoleColor.Green);

                    PL.Response.Write(new byte[] { 0xFC, 0x00 });
                    PL.Response.Write((byte)server.Count);
                    
                    foreach (var servidor in server)
                    {
                        WriteConsole.WriteLine($"[MESSENGER_CONNECT]: Server: {servidor.Name} | {servidor.IP}:{servidor.Port}", ConsoleColor.Cyan);
                        PL.Response.WriteStr(servidor.Name, 40);
                        PL.Response.Write(servidor.ServerID);
                        PL.Response.Write(servidor.MaxUser);
                        PL.Response.Write(PL.Server.Players.Count);
                        PL.Response.WriteStr(servidor.IP, 18);
                        PL.Response.Write(servidor.Port);
                        PL.Response.Write(4096);
                        PL.Response.WriteZero(13);
                    }
                    PL.SendResponse();
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MESSENGER_SERVER_ERROR]: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[MESSENGER_SERVER_ERROR]: {ex.StackTrace}", ConsoleColor.Yellow);
                
                // Send empty messenger server list on error
                try
                {
                    PL.Response.Write(new byte[] { 0xFC, 0x00, 0x00 });
                    PL.SendResponse();
                }
                catch { }
            }
        }
    }
}