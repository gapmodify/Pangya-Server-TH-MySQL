using PangyaAPI;
using PangyaAPI.PangyaClient;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using Login.MainServer;
using System;

namespace Login
{
    public class Program
    {
        #region Fields
        public static LoginServer Server;
        #endregion

        static void Main()
        {
            Console.Title = $"Pangya Fresh UP ! LoginServer";

            Server = new LoginServer();

            Server.ServerStart();

            Server.OnPacketReceived += Server_OnPacketReceived;
            //Escuta contínuamente entradas no console (Criar comandos para o Console)
            for (;;)
            {
                var comando = Console.ReadLine().Split(new char[] { ' ' }, 2);
		
		    Server.RunCommand(comando);
            }
        }

        private static void Server_OnPacketReceived(Player player, Packet packet)
        {
            var Client = (LPlayer)player;

            WriteConsole.WriteLine($"[PACKET_RECEIVED] ID: 0x{packet.Id:X4} ({(PangyaPacketsEnum)packet.Id}) from {Client.GetAddress}", ConsoleColor.Cyan);

            Client.HandleRequestPacket((PangyaPacketsEnum)packet.Id, packet);
        }
    }
}
