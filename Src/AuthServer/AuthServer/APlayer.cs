using Connector.Table;
using PangyaAPI.PangyaClient;
using System;
using System.Net.Sockets;

namespace AuthServer
{
    public class APlayer : Player
    {
        public APlayer(TcpClient tcp) : base(tcp)
        {
            PlayerLoad();
        }

        private void PlayerLoad()
        {
            try
            {
                using (var dbMember = new DB_pangya_member())
                {
                    var member = dbMember.SelectByUID((int)GetUID);

                    if (member != null)
                    {
                        this.GetLogin = member.Username;
                        this.GetNickname = member.Nickname;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[APLAYER_LOAD_ERROR]: {ex.Message}");
            }
        }
    }
}