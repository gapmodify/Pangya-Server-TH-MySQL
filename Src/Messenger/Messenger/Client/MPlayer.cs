using PangyaAPI;
using PangyaAPI.PangyaClient;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using Connector.DataBase;
using Messenger.Client.Data;
using Messenger.CreatePacket;
using Messenger.Defines;
using System;
using System.Linq;
using System.Net.Sockets;

namespace Messenger.Client
{
    public class MessengerLoginResult
    {
        public byte Code { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public int GUILD_ID { get; set; }
    }

    public partial class MPlayer : Player
    {
        #region Handle Player

        public GuildData Guild;
        public ServerProcess ServerInfo { get; set; }
        
        public MPlayer(TcpClient tcp) : base(tcp)
        {
            // ไม่ต้องสร้าง _db ใหม่ เพราะ base class (Player) สร้างให้แล้ว
            Guild = new GuildData();
            ServerInfo = new ServerProcess();
        }

        void HandlePlayerLogin(Packet packet)
        {
            #region HandleLogin

            if (!packet.ReadUInt32(out uint UID))
            {
                SendResponse(new byte[] { 0x2F, 0x00, 0x00 });
                return;
            }

            if (!packet.ReadPStr(out string UserID))
            {
                SendResponse(new byte[] { 0x2F, 0x00, 0x00 });
                return;
            }

            try
            {
                // แทน USP_MESSENGER_LOGIN ด้วย raw SQL query
                var sql = @"
                    SELECT 
                        CASE 
                            WHEN m.Username = @p0 AND m.Nickname = @p1 THEN 0
                            ELSE 1
                        END as Code,
                        m.Username,
                        m.Nickname,
                        COALESCE(gm.GUILD_ID, 0) as GUILD_ID
                    FROM pangya_member m
                    LEFT JOIN pangya_guild_member gm ON m.UID = gm.GUILD_MEMBER_UID
                    WHERE m.UID = @p2
                    LIMIT 1";

                var Query = _db.Database.SqlQuery<MessengerLoginResult>(sql, UserID, UserID, (int)UID).FirstOrDefault();

                if (Query == null || Query.Code != 0)
                {
                    SendResponse(new byte[] { 0x2F, 0x00, 0x00 });
                    return;
                }
                
                this.SetLogin(Query.Username);
                this.SetNickname(Query.Nickname);
                this.SetUID(UID);
                this.SetGuildId((uint)Query.GUILD_ID);

                Send(PacketCreator.ShowLogin(UID));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MESSENGER_LOGIN_ERROR]: {ex.Message}");
                packet.Log();
                SendResponse(new byte[] { 0x2F, 0x00, 0x00 });
                return;
            }

            #endregion
        }

        void HandleServerData(Packet packet)
        {
            #region Handle Lobby Selected
            ServerInfo = (ServerProcess)packet.Read(new ServerProcess());
                       
            SendResponse(PacketCreator.ShowConnectionServer(GetUID, USER_STATUS.IS_ONLINE, ServerInfo));

            SendResponse(PacketCreator.ShowListFriends(Server.Players.Model));
            #endregion
        }

        void HandlePlayerConnected(Packet packet)
        {
            if (!packet.ReadByte(out byte Connected)) { return; }

            switch ((USER_STATUS)Connected)
            {
                case USER_STATUS.IS_ONLINE:
                    {
                        WriteConsole.WriteLine($"PLAYER's ONLINE [{GetNickname}/{(USER_STATUS)Connected}]", ConsoleColor.Green);
                    }
                    break;
                case USER_STATUS.IS_IDLE:
                    {
                        WriteConsole.WriteLine($"PLAYER's OFFLINE [{GetNickname}{(USER_STATUS)Connected}]", ConsoleColor.Red);
                    }
                    break;
                case USER_STATUS.IS_RECONNECT:
                    {
                        WriteConsole.WriteLine($"PLAYER's RECONNECT [{GetNickname} | {(USER_STATUS)Connected}]", ConsoleColor.White);

                        SendResponse(PacketCreator.ShowConnectionServer(GetUID, USER_STATUS.IS_RECONNECT, ServerInfo));
                    }
                    break;
                default:
                    {
                        packet.Log();
                    }
                    break;
            }
            
        }

        void HandlePlayerDisconnect()
        {
            Response.Write(new byte[] { 0x30, 0x00, 0x0F, 0x01 });
            Response.Write(GetUID);
            SendResponse();
        }


        void HandleFindFriend(Packet packet)
        {

            if (!packet.ReadPStr(out string Friend))
            {
                SendResponse(PacketCreator.ShowFindFriend(false, "", 0));
                return;
            }

            var search = (MPlayer)Server.GetPlayerByNickname(Friend);

            if (search == null)
            {
                SendResponse(PacketCreator.ShowFindFriend(false, "", 0));
                return;
            }
            SendResponse(PacketCreator.ShowFindFriend(true, search.GetNickname, search.GetUID));
        }

        void HandleAddFriend(Packet packet)
        {

            if (!packet.ReadUInt32(out uint Friend_ID))
            {
                SendResponse(new byte[] { 0x30, 0x00, 0x04, 0x01, 0x01, 0x00, 0x00, 0x00 });
                return;
            }

            if (!packet.ReadPStr(out string Friend_Nick))
            {
                SendResponse(new byte[]{ 0x30, 0x00, 0x04, 0x01, 0x01, 0x00, 0x00, 0x00 });
                return;
            }

            var GetFriend = (MPlayer)Server.GetPlayerByNickname(Friend_Nick);

            SendResponse(PacketCreator.ShowAddFriend(Friend_ID, Friend_Nick, GetFriend.ServerInfo));

            Response.Write(new byte[] { 0x30, 0x00, 0x09, 0x01 });
            Response.Write(ConnectionID);
            Response.Write(GetUID);
            SendResponse();
        }

        void HandleDeleteFriend(Packet packet)
        {
            if (!packet.ReadUInt32(out uint Friend_ID))
            {
                return;
            }

            if (!packet.ReadPStr(out string Friend_Nick))
            {
                return;
            }

            var GetFriend = (MPlayer)Server.GetPlayerByNickname(Friend_Nick);

            Response.Write(new byte[] { 0x30, 0x00, 0x0B, 0x01 });
            Response.Write(0);
            Response.Write(Friend_ID);

            GetFriend.Response = Response;

            SendResponse();
            GetFriend.SendResponse();
        }
        internal void Close()
        {
            Server.DisconnectPlayer(this);
        }

        #endregion
    }
}
