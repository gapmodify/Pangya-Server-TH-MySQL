using PangyaAPI;
using PangyaAPI.PangyaClient;
using PangyaAPI.PangyaPacket;
using Messenger.Client;
using Messenger.Defines;
using Messenger.MainServer;
using System;
using System.Configuration;
using System.Reflection;

namespace Messenger
{
    class Program
    {
        #region Fields 
        public static MessengerServer _server;
        #endregion
        static void Main()
        {
            //Inicia servidor
            _server = new MessengerServer();
            _server.ServerStart();
            _server.OnClientConnected += player =>
            {
                player.Server = _server;
            };
            _server.OnPacketReceived += Server_OnPacketReceived;
            //Escuta contínuamente entradas no console (Criar comandos para o Console)
            for (; ; )
            {
                var comando = Console.ReadLine().Split(new char[] { ' ' }, 2);
                _server.RunCommand(comando);
            }
        }

        private static void Server_OnPacketReceived(Player player, Packet packet)
        {
            var Client = (MPlayer)player;

            Client.HandleRequestPacket((PangyaEnums)packet.Id, packet);
        }
    }
}
