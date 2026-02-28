using Game.GameTools;
using PangyaAPI;
using Game.Client;
using System;
using System.Linq;
using Connector.DataBase;
using static Game.GameTools.JunkPacket;
using System.Collections.Generic;
using Game.Data;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class LoginInfoCoreSystem
    {
        public void HandleUserInfo(GPlayer player, Packet packet)
        {
            //Ler UID do player baseado no Login do Player, e a Sessão 
            #region Leitura do packet
            var UID = packet.ReadUInt32();
            var session = packet.ReadByte();
            if (UID > 0)
            {
                player.SearchUID = UID;
            }
            else
            {
                UID = player.SearchUID;
            }
            #endregion

            var Client = (GPlayer)player.Server.GetPlayerByUID(UID);

            //Check
            if (Client != null)
            {
                PlayerOnline(Client, player, session);
            }
            else
            {
                PlayerOffLine(player, (int)UID, session);
            }
        }

        protected void PlayerOnline(GPlayer GetPlayer, GPlayer player, byte Session)
        {
            var Inventory = GetPlayer.Inventory;

            #region PlayerGetUserInfo
            player.Response.Write(new byte[] { 0x57, 0x01, Session });
            player.Response.Write(GetPlayer.GetUID);
            player.Response.Write(GetPlayer.GetLoginInfo());
            player.Response.Write(0); //guild points
            player.SendResponse();
            #endregion

            #region PlayerGetCharacterInfo
            player.Response.Write(new byte[] { 0x5E, 0x01 });
            player.Response.Write(GetPlayer.GetUID);
            player.Response.Write(Inventory.GetCharData());
            #endregion

            #region PlayerGetToolbarInfo
            player.Response.Write(new byte[] { 0x56, 0x01, Session });
            player.Response.Write(GetPlayer.GetUID);
            player.Response.Write(Inventory.GetEquipData());
            #endregion

            #region PlayerGetStatisticsInfo
            player.Response.Write(new byte[] { 0x58, 0x01, Session });
            player.Response.Write(GetPlayer.GetUID);
            player.Response.Write(GetPlayer.Statistic());
            player.SendResponse();
            #endregion

            #region PlayerGetGuildInfo
            player.Response.Write(new byte[] { 0x5D, 0x01 });
            player.Response.WriteUInt64(GetPlayer.GetUID);
            player.Response.Write(GetPlayer.GetGuildInfo());
            player.Response.Write(Tools.GetFixTime(GetPlayer.GuildInfo.Create_Date));
            player.SendResponse();
            #endregion

            #region PlayerResultInfo
            player.Response.Write(new byte[] { 0x89, 0x00, 0x01, 0x00, 0x00, 0x00, Session });
            player.Response.Write(GetPlayer.GetUID);
            player.SendResponse();
            #endregion
        }

        protected void PlayerOffLine(GPlayer GetClient, int UID, byte Session)
        {
          //------------
        }

        public bool InsertNewMember(string username, string password, string nickname, byte? sex, string ipAddress)
        {
            try
            {
                using (var _db = DbContextFactory.Create())
                {
                    WriteConsole.WriteLine($"[INSERT_MEMBER]: Creating new member '{username}'...", ConsoleColor.Cyan);

                    // Check if username already exists
                    var checkUsername = _db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM pangya_member WHERE username = @p0", username).FirstOrDefault();
                    
                    if (checkUsername > 0)
                    {
                        WriteConsole.WriteLine($"[INSERT_FAILED]: Username '{username}' already exists", ConsoleColor.Red);
                        return false;
                    }

                    // Check if nickname already exists
                    var checkNickname = _db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM pangya_member WHERE nickname = @p0", nickname).FirstOrDefault();
                    
                    if (checkNickname > 0)
                    {
                        WriteConsole.WriteLine($"[INSERT_FAILED]: Nickname '{nickname}' already exists", ConsoleColor.Red);
                        return false;
                    }

                    // Insert new member
                    string insertMemberSql = @"
                        INSERT INTO pangya_member 
                        (username, password, nickname, sex, ipaddress, idstate, firstset, logon, logoncount, capabilities, regdate, authkey_login, authkey_game, guildindex, dailylogincount, tutorial, birthday, event1, event2)
                        VALUES 
                        (@p0, @p1, @p2, @p3, @p4, 1, 1, 0, 0, 0, NOW(), '', '', NULL, 0, 0, NULL, 0, 0)";
                    
                    _db.Database.ExecuteSqlCommand(insertMemberSql, username, password, nickname, sex, ipAddress);

                    // Get the newly created UID
                    var uid = _db.Database.SqlQuery<int>(
                        "SELECT uid FROM pangya_member WHERE username = @p0 LIMIT 1", username).FirstOrDefault();

                    if (uid == 0)
                    {
                        WriteConsole.WriteLine($"[INSERT_FAILED]: Failed to get UID after insert", ConsoleColor.Red);
                        return false;
                    }

                    WriteConsole.WriteLine($"[INSERT_MEMBER]: Member created with UID: {uid}", ConsoleColor.Green);

                    // Insert into pangya_personal
                    string insertPersonalSql = @"
                        INSERT INTO pangya_personal (uid, cookieamt, panglockeramt, lockerpwd)
                        VALUES (@p0, 0, 0, '')";
                    
                    _db.Database.ExecuteSqlCommand(insertPersonalSql, uid);
                    WriteConsole.WriteLine($"[INSERT_MEMBER]: ✓ pangya_personal created", ConsoleColor.Green);

                    // Insert into pangya_user_statistics with all required fields
                    string insertStatsSql = @"
                        INSERT INTO pangya_user_statistics 
                        (uid, drive, putt, playtime, shottime, longest, pangya, timeout, ob, distance, hole, teamhole, holeinone, bunker, fairway, albatross, holein, puttin, longestputtin, longestchipin, game_point, game_level, pang, totalscore, bestscore0, bestscore1, bestscore2, bestscore3, bestscore4, maxpang0, maxpang1, maxpang2, maxpang3, maxpang4, sumpang, gamecount, disconnectgames, wteamwin, wteamgames, ladderpoint, ladderwin, ladderlose, ladderdraw, ladderhole, combocount, maxcombocount, nomannergamecount, skinspang, skinswin, skinslose, skinsrunholes, skinsstrikepoint, skinsallincount, gamecountseason, eventvalue, eventflag)
                        VALUES 
                        (@p0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 100000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)";
                    
                    _db.Database.ExecuteSqlCommand(insertStatsSql, uid);
                    WriteConsole.WriteLine($"[INSERT_MEMBER]: ✓ pangya_user_statistics created (Pang: 100,000)", ConsoleColor.Green);

                    // Create starter items
                    CreateStarterItems(uid, _db);

                    WriteConsole.WriteLine($"[INSERT_MEMBER]: ✅ Member '{username}' created successfully!", ConsoleColor.Green);
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[INSERT_ERROR]: {ex.Message}", ConsoleColor.Red);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[INSERT_ERROR_INNER]: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                return false;
            }
        }

        public bool InsertNewMemberSimple(string username, string password)
        {
            try
            {
                using (var _db = DbContextFactory.Create())
                {
                    // Check if username already exists
                    var checkUsername = _db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM pangya_member WHERE username = @p0", username).FirstOrDefault();
                    
                    if (checkUsername > 0)
                    {
                        WriteConsole.WriteLine($"[INSERT_FAILED]: Username '{username}' already exists", ConsoleColor.Red);
                        return false;
                    }

                    WriteConsole.WriteLine($"[INSERT_DEBUG]: Creating new user '{username}'...", ConsoleColor.Cyan);

                    // Insert into pangya_member (firstset=0, need character setup)
                    string insertMemberSql = @"
                        INSERT INTO pangya_member 
                        (username, password, idstate, firstset, lastlogontime, logon, nickname, sex, ipaddress, logoncount, capabilities, regdate, guildindex, dailylogincount, tutorial, birthday, event1, event2)
                        VALUES 
                        (@p0, @p1, 0, 0, NULL, 0, '', NULL, NULL, 0, 0, NOW(), NULL, 0, 0, NULL, 0, 0)";
                    
                    _db.Database.ExecuteSqlCommand(insertMemberSql, username, password);
                    
                    // Get the newly created UID
                    var uid = _db.Database.SqlQuery<int>(
                        "SELECT uid FROM pangya_member WHERE username = @p0 LIMIT 1", username).FirstOrDefault();

                    if (uid == 0)
                    {
                        WriteConsole.WriteLine($"[INSERT_FAILED]: Failed to get UID after insert", ConsoleColor.Red);
                        return false;
                    }

                    WriteConsole.WriteLine($"[INSERT_DEBUG]: User created with UID: {uid}", ConsoleColor.Green);
                    
                    WriteConsole.WriteLine($"[INSERT_DEBUG]: Creating pangya_personal...", ConsoleColor.Cyan);
                    // Insert into pangya_personal
                    string insertPersonalSql = @"
                        INSERT INTO pangya_personal (uid, cookieamt, panglockeramt, lockerpwd, AssistMode)
                        VALUES (@p0, 10000000, 0, '0',1)";
                    
                    _db.Database.ExecuteSqlCommand(insertPersonalSql, uid);
                    WriteConsole.WriteLine($"[INSERT_DEBUG]: ✓ pangya_personal created (Cookie: 1M)", ConsoleColor.Green);

                    WriteConsole.WriteLine($"[INSERT_DEBUG]: Creating pangya_user_statistics...", ConsoleColor.Cyan);
                    // Insert into pangya_user_statistics  
                    string insertStatsSql = @"
                        INSERT INTO pangya_user_statistics 
                        (uid, drive, putt, playtime, longest, distance, pangya, hole, teamhole, holeinone, ob, bunker, fairway, albatross, holein, pang, timeout, game_level, game_point, puttin, longestputtin, longestchipin, nomannergamecount, shottime, gamecount, disconnectgames, wteamwin, wteamgames, ladderpoint, ladderwin, ladderlose, ladderdraw, combocount, maxcombocount, totalscore, bestscore0, bestscore1, bestscore2, bestscore3, bestscore4, maxpang0, maxpang1, maxpang2, maxpang3, maxpang4, sumpang, ladderhole, gamecountseason, skinspang, skinswin, skinslose, skinsrunholes, skinsstrikepoint, skinsallincount, eventvalue, eventflag)
                        VALUES 
                        (@p0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 30000000, 0, 70, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1000, 0, 0, 0, 0, 0, 0, 127, 127, 127, 127, 127, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)";
                    
                    _db.Database.ExecuteSqlCommand(insertStatsSql, uid);
                    WriteConsole.WriteLine($"[INSERT_DEBUG]: ✓ pangya_user_statistics created (Pang: 3M)", ConsoleColor.Green);

                    WriteConsole.WriteLine($"[INSERT_SUCCESS]: User '{username}' created successfully (UID: {uid})", ConsoleColor.Green);
                    WriteConsole.WriteLine($"  ✓ FirstSet: 0 (Need to create character in Login Server)", ConsoleColor.Yellow);
                    WriteConsole.WriteLine($"  ✓ Cookie: 1M, Pang: 3M", ConsoleColor.Cyan);
                    WriteConsole.WriteLine($"  ⚠ Character/Caddie/Items will be created when selecting character in Login", ConsoleColor.Yellow);

                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[INSERT_ERROR]: {ex.Message}", ConsoleColor.Red);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[INSERT_ERROR_INNER]: {ex.InnerException.Message}", ConsoleColor.Red);
                }
                return false;
            }
        }

       
       

       

        private void CreateStarterItems(int uid, System.Data.Entity.DbContext _db)
        {
            try
            {
                WriteConsole.WriteLine($"[STARTER_ITEMS]: Creating starter items for UID: {uid}...", ConsoleColor.Cyan);

                // Create default Character (Hana - TypeID: 0x04)
                WriteConsole.WriteLine($"[STARTER_ITEMS]: Creating default character (Hana)...", ConsoleColor.Yellow);
                string insertCharacterSql = @"
                    INSERT INTO pangya_character 
                    (uid, typeid, hair_color, gift_flag, power, control, impact, spin, curve, auxpart, auxpart2, cutin)
                    VALUES (@p0, @p1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)";
                
                _db.Database.ExecuteSqlCommand(insertCharacterSql, uid, 0x04);

                var characterCID = _db.Database.SqlQuery<int>(
                    "SELECT cid FROM pangya_character WHERE uid = @p0 AND typeid = @p1 LIMIT 1", uid, 0x04).FirstOrDefault();

                WriteConsole.WriteLine($"[STARTER_ITEMS]: ✓ Character created (CID: {characterCID})", ConsoleColor.Green);

                // Create default Caddie (Quma - TypeID: 0x05000000)
                WriteConsole.WriteLine($"[STARTER_ITEMS]: Creating default caddie (Quma)...", ConsoleColor.Yellow);
                string insertCaddieSql = @"
                    INSERT INTO pangya_caddie 
                    (uid, typeid, exp, clevel, valid, regdate)
                    VALUES (@p0, @p1, 0, 1, 1, NOW())";
                
                _db.Database.ExecuteSqlCommand(insertCaddieSql, uid, 0x05000000);

                var caddieCID = _db.Database.SqlQuery<int>(
                    "SELECT cid FROM pangya_caddie WHERE uid = @p0 AND typeid = @p1 LIMIT 1", uid, 0x05000000).FirstOrDefault();

                WriteConsole.WriteLine($"[STARTER_ITEMS]: ✓ Caddie created (CID: {caddieCID})", ConsoleColor.Green);

                // Create Warehouse Items (Club, Ball)
                // Club: Air Knight (0x10000000)
                WriteConsole.WriteLine($"[STARTER_ITEMS]: Creating club (Air Knight)...", ConsoleColor.Yellow);
                string insertClubSql = @"
                    INSERT INTO pangya_warehouse 
                    (uid, typeid, valid, regdate)
                    VALUES (@p0, @p1, 1, NOW())";
                
                _db.Database.ExecuteSqlCommand(insertClubSql, uid, 0x10000000);

                var clubItemID = _db.Database.SqlQuery<int>(
                    "SELECT item_id FROM pangya_warehouse WHERE uid = @p0 AND typeid = @p1 LIMIT 1", uid, 0x10000000).FirstOrDefault();

                WriteConsole.WriteLine($"[STARTER_ITEMS]: ✓ Club created (item_id: {clubItemID})", ConsoleColor.Green);

                // Ball: Premium Commet (0x14000000)
                WriteConsole.WriteLine($"[STARTER_ITEMS]: Creating ball (Premium Commet)...", ConsoleColor.Yellow);
                string insertBallSql = @"
                    INSERT INTO pangya_warehouse 
                    (uid, typeid, valid, regdate)
                    VALUES (@p0, @p1, 1, NOW())";
                
                _db.Database.ExecuteSqlCommand(insertBallSql, uid, 0x14000000);

                var ballItemID = _db.Database.SqlQuery<int>(
                    "SELECT item_id FROM pangya_warehouse WHERE uid = @p0 AND typeid = @p1 LIMIT 1", uid, 0x14000000).FirstOrDefault();

                WriteConsole.WriteLine($"[STARTER_ITEMS]: ✓ Ball created (item_id: {ballItemID})", ConsoleColor.Green);

                // Create Toolbar (Equip)
                WriteConsole.WriteLine($"[STARTER_ITEMS]: Creating equipment toolbar...", ConsoleColor.Yellow);
                string insertToolbarSql = @"
                    INSERT INTO pangya_user_equip 
                    (uid, caddie, character_id, club_id, ball_id, mascot_id, 
                     item_slot_1, item_slot_2, item_slot_3, item_slot_4, item_slot_5,
                     item_slot_6, item_slot_7, item_slot_8, item_slot_9, item_slot_10,
                     skin_1, skin_2, skin_3, skin_4, skin_5, skin_6, poster_1, poster_2)
                    VALUES 
                    (@p0, @p1, @p2, @p3, @p4, 0, 
                     0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                     0, 0, 0, 0, 0, 0, 0, 0)";
                
                _db.Database.ExecuteSqlCommand(insertToolbarSql, uid, caddieCID, characterCID, clubItemID, ballItemID);

                WriteConsole.WriteLine($"[STARTER_ITEMS]: ✓ Equipment toolbar created", ConsoleColor.Green);
                WriteConsole.WriteLine($"[STARTER_ITEMS]: ✅ All starter items created successfully!", ConsoleColor.Green);
                WriteConsole.WriteLine($"  ├─ Character: Hana (CID: {characterCID})", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"  ├─ Caddie: Quma (CID: {caddieCID})", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"  ├─ Club: Air Knight (item_id: {clubItemID})", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"  └─ Ball: Premium Commet (item_id: {ballItemID})", ConsoleColor.Cyan);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[STARTER_ITEMS_ERROR]: {ex.Message}", ConsoleColor.Red);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[STARTER_ITEMS_ERROR_INNER]: {ex.InnerException.Message}", ConsoleColor.Red);
                }
            }
        }
    }
}
