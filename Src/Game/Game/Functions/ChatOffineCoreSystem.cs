using Game.Client;
using System.Linq;
using PangyaAPI;
using Connector.DataBase;
using PangyaAPI.PangyaPacket;

namespace Game.Functions
{
   public class ChatOffineCoreSystem
    {
        public void PlayerSendChatOffline(GPlayer player)
        {
            using (var _db = DbContextFactory.Create())
            {
                var msg_user = _db.Database.SqlQuery<UserMessageData>(
                    "CALL ProcGetUserMessage(@p0)", (int)player.GetUID).ToList();
                
                if (msg_user.Count > 0)
                {
                    player.Response.Write(new byte[] { 0xB2, 0x00 });
                    player.Response.Write((long)2);
                    player.Response.Write(msg_user.Count);
                    foreach (var data in msg_user)
                    {
                        player.Response.Write(data.uid);
                        player.Response.Write((ushort)data.ID_MSG);
                        player.Response.WriteStr(data.Nickname, 22);
                        player.Response.WriteStr(data.Message, 64);
                        player.Response.WriteStr(data.reg_date.ToString(), 17);
                    }
                    player.SendResponse();
                }
            }
        }

        public void PlayerResponseChatOffline(GPlayer player, Packet packet)
        {
            if (!packet.ReadUInt32(out uint From_ID)) { return; }
            if (!packet.ReadPStr(out string Messange)) { return; }

            using (var _db = DbContextFactory.Create())
            {
                _db.Database.ExecuteSqlCommand(
                    "CALL ProcAddUserMessage(@p0, @p1, @p2)", 
                    (int)player.GetUID, (int)From_ID, Messange);
            }

            player.LoadStatistic();

            player.Response.Write(new byte[] { 0x95, 0x00, 0x11, 0x01 });
            player.Response.Write(0);
            player.Response.Write((long)player.GetPang);
            player.SendResponse();
        }
    }
    
    public class UserMessageData
    {
        public int uid { get; set; }
        public int ID_MSG { get; set; }
        public string Nickname { get; set; }
        public string Message { get; set; }
        public System.DateTime reg_date { get; set; }
    }
}
