using Connector.DataBase;
using Connector.Table;
using PangyaAPI.Auth;
using PangyaAPI.BinaryModels;
using PangyaAPI.PangyaClient;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
namespace PangyaAPI.Server
{
    public abstract partial class TcpServer
    {
        #region Delegates
        public delegate void ConnectedEvent(Player player);
        public delegate void PacketReceivedEvent(Player player, Packet packet);
        #endregion

        #region Events
        /// <summary>
        /// This event occurs when ProjectG connects to the server
        /// </summary>
        public event ConnectedEvent OnClientConnected;

        /// <summary>
        /// This event occurs when the server receives a Packet from ProjectG
        /// </summary>
        public event PacketReceivedEvent OnPacketReceived;

        #endregion

        #region Fields

        /// <summary>
        /// List of connected players
        /// </summary>
        public GenericDisposableCollection<Player> Players = new GenericDisposableCollection<Player>();

        public uint NextConnectionId { get; set; } = 1;

        public TcpListener _server;

        public bool _isRunning;

        public AuthClient AuthServer;

        public ServerSettings Data;

        public bool ShowLog { get; set; }

        public DateTime EndTime { get; set; }

        public bool OpenServer = false;

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Send key to player
        /// </summary>
        protected abstract void SendKey(Player player);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcp"></param>
        protected abstract Player OnConnectPlayer(TcpClient tcp);
        protected abstract void ServerExpection(Player Client, Exception Ex);

        public abstract void DisconnectPlayer(Player Player);

        public abstract void ServerStart();

        public abstract Player GetClientByConnectionId(UInt32 ConnectionId);

        public abstract Player GetPlayerByNickname(string Nickname);

        public abstract Player GetPlayerByUsername(string Username);

        public abstract Player GetPlayerByUID(UInt32 UID);

        public abstract bool GetPlayerDuplicate(UInt32 UID);
        public abstract bool PlayerDuplicateDisconnect(UInt32 UID);

        public abstract void RunCommand(string[] Command);
        #endregion

        #region Constructor
        public TcpServer()
        {
            EndTime = new DateTime(2563, 07, 30, 0, 17, 0);
        }

        #endregion

        #region Private Methods

        #region AuthServer

        protected abstract AuthClient AuthServerConstructor();

        protected abstract void OnAuthServerPacketReceive(AuthClient client, AuthPacket packet);


        /// <summary>
        /// Connect to AuthServer
        /// </summary>
        public bool ConnectToAuthServer(AuthClient client)
        {
            AuthServer = client;
            AuthServer.OnDisconnect += OnAuthServerDisconnected;
            AuthServer.OnPacketReceived += AuthServer_OnPacketReceived;
            return AuthServer.Connect();
        }

        /// <summary>
        /// Called when packet is received from AuthServer
        /// </summary>
        private void AuthServer_OnPacketReceived(AuthClient authClient, AuthPacket packet)
        {
            OnAuthServerPacketReceive(authClient, packet);
        }

        /// <summary>
        /// Called when not connected to AuthServer
        /// </summary>
        private void OnAuthServerDisconnected()
        {
            Console.WriteLine("Server stopped");
            Console.WriteLine("Cannot connect to AuthServer!");
            Console.ReadKey();
            Environment.Exit(1);
        }

        #endregion

        public void UpdateServer()
        {
            using (var db = new DB_pangya_server())
            {
                db.UpdateUsersOnline((int)Data.UID, Players.Count);
            }
        }

        /// <summary>
        /// Wait for connections
        /// </summary>
        public void HandleWaitConnections()
        {
            WriteConsole.WriteLine("[SERVER_LISTENER] Waiting for connections...", ConsoleColor.Cyan);
            
            while (_isRunning)
            {
                try
                {
                    WriteConsole.WriteLine("[SERVER_LISTENER] Ready to accept new client...", ConsoleColor.Gray);
                    
                    // Start accepting new connections (when player connects)
                    TcpClient newClient = _server.AcceptTcpClient();

                    WriteConsole.WriteLine($"[SERVER_LISTENER] ✓ New client connected! Creating handler thread...", ConsoleColor.Green);

                    // Client connected
                    // Create Thread for handling communication (one thread per client)
                    Thread t = new Thread(new ParameterizedThreadStart(HandlePlayer));
                    t.Start(newClient);
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        WriteConsole.WriteLine($"[SERVER_LISTENER] Error accepting client: {ex.Message}", ConsoleColor.Red);
                    }
                }
            }
            
            WriteConsole.WriteLine("[SERVER_LISTENER] Server stopped listening", ConsoleColor.Yellow);
        }

        /// <summary>
        /// Handle client communication
        /// </summary>
        private void HandlePlayer(object obj)
        {
            // Get client from parameter
            TcpClient tcpClient = (TcpClient)obj;

            var Player = OnConnectPlayer(tcpClient);

            var thread = new Thread(new ThreadStart(Player.RefreshData));
            thread.Start();

            // Call OnClientConnected event
            this.OnClientConnected?.Invoke(Player);

            while (Player.Connected)
            {
                try
                {
                    byte[] message = ReceivePacket(tcpClient.GetStream());

                    if (message.Length >= 5)
                    {
                        if (Player.Connected)
                        {
                            var packet = new Packet(message, Player.GetKey);
                            if (ShowLog)
                            {
                                packet.Log();
                            }
                            // Call OnPacketReceived event
                            OnPacketReceived?.Invoke(Player, packet: packet);
                        }
                    }
                    else
                    {
                        if (Player.Connected)
                        {
                            DisconnectPlayer(Player);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ServerExpection(Player, ex);
                }
            }
            if (Player.Connected)
                DisconnectPlayer(Player);
        }

        protected byte[] ReceivePacket(NetworkStream Stream)
        {
            int bytesRead = 0;
            byte[] message, messageBufferRead = new byte[500000]; // Buffer size to read
            try
            {
                if (Stream != null && Stream.CanRead)
                {
                    // Read data from client
                    bytesRead = Stream.Read(messageBufferRead, 0, messageBufferRead.Length);
                }
                // Variable to store received data
                message = new byte[bytesRead];

                // Copy received data
                Buffer.BlockCopy(messageBufferRead, 0, message, 0, bytesRead);

                return message;
            }
            catch
            {
                return new byte[0];
            }
        }

        #endregion

        #region Public Methods

        public void SendToAll(byte[] Data)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].SendResponse(Data);
            }
        }

        public void Notice(string message)
        {
            var response = new PangyaBinaryWriter(new MemoryStream());

            response.Write(new byte[] { 0x42, 0x00 });
            response.WritePStr("Notice: " + message);
            SendToAll(response.GetBytes());
            Console.WriteLine("Notice sent successfully");
        }

        public void ShowHelp()
        {
            Console.WriteLine(Environment.NewLine);
            WriteConsole.WriteLine("Welcome to Py-Server!" + Environment.NewLine);

            WriteConsole.WriteLine("Available console commands:" + Environment.NewLine);

            WriteConsole.WriteLine("help        | Show console command list");
            WriteConsole.WriteLine("testdb      | Test database connection and show sample data");
            WriteConsole.WriteLine("resetlogon  | Reset all stuck login states (Logon=0)");
            WriteConsole.WriteLine("topnotice   | Show message to online players");
            WriteConsole.WriteLine("kickuser    | Disconnect by UserName");
            WriteConsole.WriteLine("kicknick    | Disconnect by Nick");
            WriteConsole.WriteLine("kickuid     | Disconnect by UID");
            WriteConsole.WriteLine("clear       | Clear console screen");
            WriteConsole.WriteLine("cls         | Clear console screen");
            WriteConsole.WriteLine("quit        | Close server");
            WriteConsole.WriteLine("start       | Open server");
            WriteConsole.WriteLine("stop        | Close server");

            Console.WriteLine(Environment.NewLine);
        }
        #endregion
    }
}
