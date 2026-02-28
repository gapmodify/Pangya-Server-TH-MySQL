using Game.Client;
using System;
using System.Linq;
using Game.Client.Inventory;
using PangyaAPI.BinaryModels;
using PangyaAPI;
using static Game.GameTools.Tools;
using static Game.GameTools.PacketCreator;
using Connector.DataBase;
using Game.Client.Inventory.Data;
using System.Text;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class MailBoxSystem
    {
        public void PlayerGetMailList(GPlayer PL, Packet packet, bool IsDel = false)
        {
            try
            {
                WriteConsole.WriteLine($"[MAIL_LIST]: Mail system not fully implemented for MySQL", ConsoleColor.Yellow);
                
                if (!packet.ReadInt32(out int PageSelect))
                {
                    return;
                }
                
                // Send empty mail list
                using (var Reply = new PangyaBinaryWriter())
                {
                    if (IsDel)
                    {
                        Reply.Write(new byte[] { 0x15, 0x02 });
                    }
                    else
                    {
                        Reply.Write(new byte[] { 0x11, 0x02 });
                    }
                    Reply.Write(0);
                    Reply.Write(PageSelect);
                    Reply.Write(1); // Total pages
                    Reply.Write(0); // No mails
                    PL.SendResponse(Reply.GetBytes());
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MAIL_LIST_EXCEPTION]: {ex.Message}", ConsoleColor.Red);
                try
                {
                    PL.SendResponse(new byte[] { 0x11, 0x02, 0x02, 0x00, 0x00, 0x00 });
                }
                catch { }
            }
        }

        public void PlayerDeleteMail(GPlayer PL, Packet packet)
        {
            try
            {
                WriteConsole.WriteLine($"[MAIL_DELETE]: Mail system not fully implemented for MySQL", ConsoleColor.Yellow);
                PL.SendResponse(new byte[] { 0x14, 0x02, 0x00, 0x00, 0x00, 0x00 });
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MAIL_DELETE_EXCEPTION]: {ex.Message}", ConsoleColor.Red);
                try
                {
                    PL.SendResponse(new byte[] { 0x14, 0x02, 0x02, 0x00, 0x00, 0x00 });
                }
                catch { }
            }
        }

        public void PlayerReadMail(GPlayer PL, Packet packet)
        {
            try
            {
                WriteConsole.WriteLine($"[MAIL_READ]: Mail system not fully implemented for MySQL", ConsoleColor.Yellow);
                
                if (!packet.ReadUInt32(out uint MailIndex))
                {
                    return;
                }
                
                PL.SendResponse(new byte[] { 0x12, 0x02, 0x02, 0x00, 0x00, 0x00 });
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MAIL_READ_EXCEPTION]: {ex.Message}", ConsoleColor.Red);
                try
                {
                    PL.SendResponse(new byte[] { 0x12, 0x02, 0x02, 0x00, 0x00, 0x00 });
                }
                catch { }
            }
        }

        public void PlayerReleaseItem(GPlayer PL, Packet packet)
        {
            try
            {
                WriteConsole.WriteLine($"[MAIL_RELEASE]: Mail system not fully implemented for MySQL", ConsoleColor.Yellow);
                
                if (!packet.ReadUInt32(out uint MailIndex))
                {
                    return;
                }
                
                PL.SendResponse(new byte[] { 0x14, 0x02, 0x00, 0x00, 0x00, 0x00 });
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MAIL_RELEASE_EXCEPTION]: {ex.Message}", ConsoleColor.Red);
                try
                {
                    PL.SendResponse(new byte[] { 0x14, 0x02, 0x02, 0x00, 0x00, 0x00 });
                }
                catch { }
            }
        }

        public void CheckUserForGift(GPlayer player, Packet packet)
        {
            try
            {
                WriteConsole.WriteLine($"[GIFT_CHECK]: Gift system not fully implemented for MySQL", ConsoleColor.Yellow);
                
                if (!packet.ReadByte(out byte Type))
                {
                    player.SendResponse(new byte[] { 0xA1, 0x00, 0x02 });
                    return;
                }
                
                player.SendResponse(new byte[] { 0xA1, 0x00, 0x02 });
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[GIFT_CHECK_EXCEPTION]: {ex.Message}", ConsoleColor.Red);
                try
                {
                    player.SendResponse(new byte[] { 0xA1, 0x00, 0x02 });
                }
                catch { }
            }
        }

        public void PlayerShowMailPopUp(GPlayer PL)
        {
            try
            {
                using (var Reply = new PangyaBinaryWriter())
                {
                    // Send empty mail popup for MySQL
                    Reply.Write(new byte[] { 0x10, 0x02 });
                    Reply.Write(0);
                    Reply.Write(0); // No mails
                    PL.SendResponse(Reply.GetBytes());
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MAIL_POPUP_ERROR]: {ex.Message}", ConsoleColor.Yellow);
                try
                {
                    PL.SendResponse(new byte[] { 0x10, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                }
                catch { }
            }
        }
    }
}
