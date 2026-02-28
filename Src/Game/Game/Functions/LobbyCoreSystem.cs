using System;
using System.Linq;
using Game.Lobby;
using Game.Client;
using PangyaAPI;
using Connector.DataBase;
using Connector.Table;
using static Game.Lobby.Collection.ChannelCollection;
using static Game.GameTools.PacketCreator;
using static Game.GameTools.Tools;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class LobbyCoreSystem
    {

        public void PlayerSelectLobby(GPlayer player, Packet packet, bool RequestJoinGameList = false)
        {
            try
            {
                WriteConsole.WriteLine($"[LOBBY_SELECT]: Player {player.GetUID} selecting lobby...", ConsoleColor.Cyan);

                var lp = player.Lobby;

                //Lê Id do Lobby
                if (!packet.ReadByte(out byte lobbyId))
                {
                    WriteConsole.WriteLine($"[LOBBY_SELECT_ERROR]: Cannot read lobbyId", ConsoleColor.Red);
                    return;
                }

                WriteConsole.WriteLine($"[LOBBY_SELECT]: Player {player.GetUID} requesting LobbyID: {lobbyId}", ConsoleColor.Cyan);

                var lobby = LobbyList.GetLobby(lobbyId);

                // Remove from old lobby if exists
                if (lp != null)
                {
                    WriteConsole.WriteLine($"[LOBBY_SELECT]: Removing player {player.GetUID} from old lobby {lp.Id}", ConsoleColor.Yellow);
                    lp.RemovePlayer(player);
                }

                //Caso o lobby não existir
                if (lobby == null)
                {
                    WriteConsole.WriteLine($"[LOBBY_SELECT_ERROR]: Lobby {lobbyId} not found!", ConsoleColor.Red);
                    player.SendResponse(new byte[] { 0x95, 0x00, 0x02, 0x01, 0x00 });
                    return;
                }

                //Se estiver lotado
                if (lobby.IsFull)
                {
                    WriteConsole.WriteLine($"[LOBBY_SELECT_ERROR]: Lobby {lobbyId} is full!", ConsoleColor.Red);
                    player.SendResponse(new byte[] { 0x4E, 0x00, 0x02 });
                    return;
                }

                WriteConsole.WriteLine($"[LOBBY_SELECT]: Adding player {player.GetUID} to lobby {lobbyId}", ConsoleColor.Green);

                // ## add player
                if (lobby.AddPlayer(player))
                {
                    WriteConsole.WriteLine($"[LOBBY_SELECT_SUCCESS]: Player {player.GetUID} joined lobby {lobbyId}", ConsoleColor.Green);

                    if (RequestJoinGameList == false)
                    {
                        player.SendResponse(ShowEnterLobby(1));
                        player.SendResponse(new byte[] { 0xF6, 0x01, 0x00, 0x00, 0x00, 0x00 });
                    }

                    // ✅ AUTO DAILY LOGIN REWARD - Check if already claimed today
                    if (lp == null)
                    {
                        try
                        {
                            using (var dbMember = new DB_pangya_member())
                            {
                                // ✅ Check LastLogonTime from pangya_member
                                var memberData = dbMember.SelectByUID((int)player.GetUID);
                                
                                if (memberData != null && memberData.LastLogonTime.HasValue)
                                {
                                    // ✅ Existing player - Check daily reward
                                    DateTime lastLogin = memberData.LastLogonTime.Value;
                                    DateTime today = DateTime.Now.Date;
                                    DateTime lastLoginDate = lastLogin.Date;
                                    
                                    WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: Checking for {player.GetNickname} (UID: {player.GetUID})", ConsoleColor.Magenta);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]:   Status: EXISTING PLAYER", ConsoleColor.Cyan);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]:   Last Login: {lastLogin:yyyy-MM-dd HH:mm:ss}", ConsoleColor.Cyan);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]:   Today: {today:yyyy-MM-dd}", ConsoleColor.Cyan);
                                    
                                    // ✅ Same day check
                                    if (lastLoginDate == today)
                                    {
                                        WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ⏭ Already claimed today - SKIPPED", ConsoleColor.Yellow);
                                        WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                    }
                                    else
                                    {
                                        WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ✅ New day detected - Processing reward...", ConsoleColor.Green);
                                        
                                        // ✅ Run daily reward asynchronously
                                        System.Threading.Tasks.Task.Run(() =>
                                        {
                                            try
                                            {
                                                System.Threading.Thread.Sleep(1000);
                                                
                                                var dailyReward = new LoginDailyRewardSystem();
                                                dailyReward.PlayerDailyLoginItem(player);
                                                
                                                WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ✅ Reward completed for {player.GetNickname}", ConsoleColor.Green);
                                                
                                                // ✅ Update LastLogonTime to NOW
                                                try
                                                {
                                                    using (var dbMemberUpdate = new DB_pangya_member())
                                                    {
                                                        dbMemberUpdate.UpdateLastLogonTime((int)player.GetUID, DateTime.Now);
                                                        WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ✓ LastLogonTime updated to NOW()", ConsoleColor.Green);
                                                    }
                                                }
                                                catch (Exception updateEx)
                                                {
                                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ⚠ Failed to update LastLogonTime: {updateEx.Message}", ConsoleColor.Yellow);
                                                }
                                            }
                                            catch (Exception dailyEx)
                                            {
                                                WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ❌ Error: {dailyEx.Message}", ConsoleColor.Red);
                                                if (dailyEx.InnerException != null)
                                                {
                                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: Inner: {dailyEx.InnerException.Message}", ConsoleColor.Yellow);
                                                }
                                            }
                                            finally
                                            {
                                                WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                            }
                                        });
                                    }
                                }
                                else if (memberData != null && !memberData.LastLogonTime.HasValue)
                                {
                                    // ✅ NEW PLAYER - First time login ever!
                                    WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: 🎉 NEW PLAYER DETECTED!", ConsoleColor.Green);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]:   Player: {player.GetNickname} (UID: {player.GetUID})", ConsoleColor.Cyan);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]:   Status: FIRST TIME LOGIN", ConsoleColor.Yellow);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]:   Processing welcome reward...", ConsoleColor.Green);
                                    
                                    // ✅ Run new player welcome reward asynchronously
                                    System.Threading.Tasks.Task.Run(() =>
                                    {
                                        try
                                        {
                                            System.Threading.Thread.Sleep(1000);
                                            
                                            // ✅ Give daily reward (ให้รางวัลปกติ)
                                            var dailyReward = new LoginDailyRewardSystem();
                                            dailyReward.PlayerDailyLoginItem(player);
                                            
                                            WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ✅ Welcome reward completed for {player.GetNickname}", ConsoleColor.Green);
                                            
                                            // ✅ Update LastLogonTime to NOW (mark as no longer new)
                                            try
                                            {
                                                using (var dbMemberUpdate = new DB_pangya_member())
                                                {
                                                    dbMemberUpdate.UpdateLastLogonTime((int)player.GetUID, DateTime.Now);
                                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ✓ LastLogonTime initialized to NOW()", ConsoleColor.Green);
                                                }
                                            }
                                            catch (Exception updateEx)
                                            {
                                                WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ⚠ Failed to update LastLogonTime: {updateEx.Message}", ConsoleColor.Yellow);
                                            }
                                        }
                                        catch (Exception welcomeEx)
                                        {
                                            WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ❌ Welcome reward error: {welcomeEx.Message}", ConsoleColor.Red);
                                            if (welcomeEx.InnerException != null)
                                            {
                                                WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: Inner: {welcomeEx.InnerException.Message}", ConsoleColor.Yellow);
                                            }
                                        }
                                        finally
                                        {
                                            WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                        }
                                    });
                                }
                                else
                                {
                                    // ✅ Fallback - Member data not found (should not happen)
                                    WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                    WriteConsole.WriteLine($"[AUTO_DAILY_REWARD]: ⚠ Warning - Member data not found for UID: {player.GetUID}", ConsoleColor.Yellow);
                                    WriteConsole.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", ConsoleColor.Magenta);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteConsole.WriteLine($"[AUTO_DAILY_REWARD_INIT]: ⚠️ Could not start: {ex.Message}", ConsoleColor.Yellow);
                        }
                    }

                    // ## if request join lobby
                    if (RequestJoinGameList)
                    {
                        WriteConsole.WriteLine($"[LOBBY_SELECT]: Player {player.GetUID} joining game list", ConsoleColor.Cyan);

                        player.SendResponse(ShowEnterLobby(1));
                        player.SendResponse(new byte[] { 0xF6, 0x01, 0x00, 0x00, 0x00, 0x00 });
                        lobby.JoinMultiplayerGamesList(player);
                    }
                }
                else
                {
                    WriteConsole.WriteLine($"[LOBBY_SELECT_ERROR]: Failed to add player {player.GetUID} to lobby {lobbyId}", ConsoleColor.Red);
                    player.SendResponse(new byte[] { 0x95, 0x00, 0x02, 0x01, 0x00 });
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[LOBBY_SELECT_EXCEPTION]: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOBBY_SELECT_EXCEPTION]: StackTrace: {ex.StackTrace}", ConsoleColor.Red);

                try
                {
                    player?.SendResponse(new byte[] { 0x95, 0x00, 0x02, 0x01, 0x00 });
                }
                catch { }
            }
        }

        public void PlayerJoinMultiGameList(GPlayer player, bool GrandPrix = false)
        {
            var lobby = player.Lobby;

            if (lobby == null) return;

            lobby.JoinMultiplayerGamesList(player);

            if (GrandPrix)
            {
                player.SendResponse(new byte[]
               {
                    0x50, 0x02, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02,
                    0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x34, 0x43
               });
            }
            else
            {
                player.SendResponse(new byte[] { 0xF5, 0x00 });
            }
        }

        public void PlayerLeaveMultiGamesList(GPlayer player, bool GrandPrix = false)
        {
            var lobby = player.Lobby;

            if (lobby == null) return;

            lobby.LeaveMultiplayerGamesList(player);

            if (GrandPrix)
            {
                player.SendResponse(new byte[]
               {
                    0x51, 0x02, 0x00, 0x00, 0x00, 0x00
               });
            }
            else
            {
                player.SendResponse(new byte[] { 0xF6, 0x00 });
            }
        }

        public void PlayerChat(GPlayer player, Packet packet)
        {
            var PLobby = player.Lobby;
            if (PLobby == null)
            {
                return;
            }
            Console.WriteLine(packet.ReadUInt32());
            packet.ReadPStr(out string Nickname);
            packet.ReadPStr(out string Messages);

            if (!(Nickname == player.GetNickname))
            {
                return;
            }
            PLobby.PlayerSendChat(player, Messages);
        }

        public void PlayerWhisper(GPlayer player, Packet packet)
        {
            Channel PLobby;

            PLobby = player.Lobby;
            if (PLobby == null)
            {
                return;
            }

            if (!packet.ReadPStr(out string Nickname))
            {

            }

            if (!packet.ReadPStr(out string Messages))
            {

            }

            PLobby.PlayerSendWhisper(player, Nickname, Messages);
        }

        public void PlayerChangeNickname(GPlayer player, Packet packet)
        {
            if (!packet.ReadPStr(out string nick)) { return; }

            if (nick.Length < 4 || nick.Length > 16)
            {
                ShowChangeNickName(1);
                return;
            }

            if (player.GetCookie < 1500)
            {
                ShowChangeNickName(4);
                throw new Exception($"Player not have cookies enough: {player.GetCookie}");
            }

            var CODE = 1;
            //Nickname duplicate
            if (CODE == 2 || CODE == 0)
            {
                ShowChangeNickName(2);
                return;
            }
            //Sucess
            if (CODE == 1)
            {
                ShowChangeNickName(0, nick);

                player.SetNickname(nick);
                //se não for gm ou A.I
                if (player.GetCapability != 4 || player.GetCapability != 15)
                {
                    player.RemoveCookie(500);//debita 

                    player.SendCookies();
                }

                var lobby = player.Lobby;
                if (lobby != null)
                {
                    lobby.UpdatePlayerLobbyInfo(player);
                }
            }
        }

        public void PlayerCreateGame(GPlayer player, Packet packet)
        {
            var PLobby = player.Lobby;
            if (PLobby == null && player.Game != null)
            {
                return;
            }
            PLobby.PlayerCreateGame(player, packet);
        }

        public void PlayerLeaveGame(GPlayer player)
        {
            var PLobby = player.Lobby;
            if (PLobby == null)
            {
                WriteConsole.WriteLine($"[LEAVE_ROOM] ⚠ Player {player.GetNickname} - Lobby is null, sending error response", ConsoleColor.Yellow);
                try
                {
                    player.SendResponse(new byte[] { 0x54, 0x02, 0x01, 0x00, 0x00, 0x00 });
                }
                catch { }
                return;
            }
            
            // Save inventory/equipment when leaving room
            try
            {
                WriteConsole.WriteLine($"[LEAVE_ROOM] ⚡ Player {player.GetNickname} (UID:{player.GetUID}) leaving room - Saving equipment...", ConsoleColor.Cyan);
                
                if (player.Inventory != null)
                {
                    // ✅ ใช้ DB_pangya_user_equip แทน DbContextFactory
                    WriteConsole.WriteLine($"[LEAVE_ROOM] 📝 Current Equipment:", ConsoleColor.Yellow);
                    WriteConsole.WriteLine($"[LEAVE_ROOM]   • Character: {player.Inventory.CharacterIndex}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LEAVE_ROOM]   • Caddie: {player.Inventory.CaddieIndex}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LEAVE_ROOM]   • Club: {player.Inventory.ClubSetIndex}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LEAVE_ROOM]   • Ball: {player.Inventory.BallTypeID}", ConsoleColor.White);
                    WriteConsole.WriteLine($"[LEAVE_ROOM]   • Mascot: {player.Inventory.MascotIndex}", ConsoleColor.White);
                    
                    using (var dbEquip = new DB_pangya_user_equip())
                    {
                        var equipData = dbEquip.SelectByUID((int)player.GetUID);
                        if (equipData != null)
                        {
                            equipData.CHARACTER_ID = (int)player.Inventory.CharacterIndex;
                            equipData.CADDIE = (int)player.Inventory.CaddieIndex;
                            equipData.CLUB_ID = (int)player.Inventory.ClubSetIndex;
                            equipData.BALL_ID = (int)player.Inventory.BallTypeID;
                            equipData.MASCOT_ID = (int)player.Inventory.MascotIndex;
                            dbEquip.Update(equipData);
                            
                            WriteConsole.WriteLine($"[LEAVE_ROOM] ✓ Equipment saved successfully to pangya_user_equip", ConsoleColor.Green);
                            WriteConsole.WriteLine($"[LEAVE_ROOM] ✓ Verification: CHARACTER_ID = {equipData.CHARACTER_ID}", ConsoleColor.Green);
                        }
                        else
                        {
                            WriteConsole.WriteLine($"[LEAVE_ROOM] ⚠ Equipment data not found for UID:{player.GetUID}", ConsoleColor.Yellow);
                        }
                    }
                }
                else
                {
                    WriteConsole.WriteLine($"[LEAVE_ROOM] ⚠ Warning: Inventory is null, cannot save", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[LEAVE_ROOM] ✗ Equipment save failed: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LEAVE_ROOM] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[LEAVE_ROOM] Inner: {ex.InnerException.Message}", ConsoleColor.Yellow);
                }
            }
            
            // Remove player from game
            PLobby.PlayerLeaveGame(player);
            
            // ✅ SEND RESPONSE - ต้องส่ง response กลับไปที่ client เพื่อไม่ให้ค้าง
            try
            {
                WriteConsole.WriteLine($"[LEAVE_ROOM] 📤 Sending leave room response to client", ConsoleColor.Cyan);
                player.SendResponse(new byte[] { 0x54, 0x02, 0x00, 0x00, 0x00, 0x00 });
                WriteConsole.WriteLine($"[LEAVE_ROOM] ✅ Leave room response sent successfully", ConsoleColor.Green);
            }
            catch (Exception responseEx)
            {
                WriteConsole.WriteLine($"[LEAVE_ROOM] ⚠ Failed to send response: {responseEx.Message}", ConsoleColor.Yellow);
            }
        }

        public void PlayerLeaveGP(GPlayer player)
        {
            var PLobby = player.Lobby;
            if (PLobby == null)
            {
                return;
            }
            PLobby.PlayerLeaveGP(player);
        }

        public void PlayerJoinGame(GPlayer player, Packet packet)
        {
            var PLobby = player.Lobby;
            if (PLobby == null)
            {
                return;
            }
            PLobby.PlayerJoinGame(player, packet);
        }

        public void PlayerGetLobbyInfo(GPlayer player)
        {
            player.Response.Write(LobbyList.GetBuildServerInfo());
            player.SendResponse();
        }

        public void PlayerGetGameInfo(GPlayer player, Packet packet)
        {
            var PLobby = player.Lobby;
            if (PLobby == null)
            {
                return;
            }
            PLobby.PlayerRequestGameInfo(player, packet);
        }

        public void PlayerEnterGP(GPlayer player, Packet packet)
        {
            var PLobby = player.Lobby;
            if (PLobby == null)
            {
                return;
            }
            PLobby.PlayerJoinGrandPrix(player, packet);
        }

        public void PlayerGetTime(GPlayer player)
        {
            player.Response.Write(new byte[] { 0xBA, 0x00 });
            player.Response.Write(GameTime());
            player.SendResponse();
        }
    }
}
