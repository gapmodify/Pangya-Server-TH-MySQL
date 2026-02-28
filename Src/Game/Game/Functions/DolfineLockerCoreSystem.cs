using PangyaAPI;
using Game.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Connector.DataBase;
using static PangyaFileCore.IffBaseManager;
using Game.Client.Inventory.Data.Warehouse;
using PangyaAPI.PangyaPacket;
using Game.MainServer;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class DolfineLockerSystem
    {
        class LockerItem
        {
            public decimal TOTAL_PAGE { get; set; }
            public int INVEN_ID { get; set; }
            public int? TypeID { get; set; }
            public string UCC_UNIQE { get; set; }
            public byte UCC_STATUS { get; set; }
            public string UCC_NAME { get; set; }
            public int? UCC_COCOUNT { get; set; }
            public string NICKNAME { get; set; }
        }

        public void HandleEnterRoom(GPlayer player)
        {
            if (player.LockerPWD == "0")
            {
                //Chama a primeira criação da senha do dolfine locker
                player.SendResponse(new byte[] { 0x70, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 });
                return;
            }
            else
            {
                player.SendResponse(new byte[] { 0x70, 0x01, 0x00, 0x00, 0x00, 0x00, 0x4C, 0x00, 0x00, 0x00 });
            }
        }

        public void PlayerSetLocker(GPlayer player, Packet packet)
        {
            try
            {
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] PlayerSetLocker called for UID:{player.GetUID}", ConsoleColor.Cyan);
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] Current LockerPWD: '{player.LockerPWD}' (Length: {player.LockerPWD?.Length ?? 0})", ConsoleColor.Yellow);
                
                if (player.LockerPWD != "0")
                {
                    WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✗ LockerPWD is not '0' - returning without sending packet", ConsoleColor.Red);
                    return;
                }
                
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ LockerPWD is '0' - proceeding...", ConsoleColor.Green);
                
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] → Reading password from packet...", ConsoleColor.Yellow);
                var PwdInput = packet.ReadPStr();
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ Password read: '{PwdInput}' (Length: {PwdInput?.Length ?? 0})", ConsoleColor.Cyan);
                
                if (PwdInput.Length >= 4)
                {
                    WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ Password length valid (>=4)", ConsoleColor.Green);
                    WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] → Creating database context...", ConsoleColor.Yellow);

                    using (var _db = DbContextFactory.Create())
                    {
                        WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ Database context created", ConsoleColor.Green);
                        WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] → Executing UPDATE query...", ConsoleColor.Yellow);
                        
                        // Update LockerPwd in database
                        int rowsAffected = _db.Database.ExecuteSqlCommand(
                            "UPDATE pangya_personal SET LockerPwd = @p0 WHERE UID = @p1",
                            PwdInput, (int)player.GetUID);
                        
                        WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ Rows affected: {rowsAffected}", ConsoleColor.Green);
                        
                        if (rowsAffected > 0)
                        {
                            player.LockerPWD = PwdInput;
                            WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ Password set to: '{PwdInput}'", ConsoleColor.Green);

                            player.SendResponse(new byte[] { 0x76, 0x01, 0x00, 0x00, 0x00, 0x00 });
                            WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✓ Success packet (0x76 0x01 0x00...) sent", ConsoleColor.Green);
                        }
                        else
                        {
                            WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✗ No rows affected, not sending any packet", ConsoleColor.Red);
                        }
                    }
                }
                else
                {
                    WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✗ Password too short (<4 chars), not sending any packet", ConsoleColor.Red);
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] ✗✗✗ EXCEPTION ✗✗✗", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] Error: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                if (ex.InnerException != null)
                {
                    WriteConsole.WriteLine($"[LOCKER_SET_DEBUG] Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
            }
        }

        public void PlayerOpenLocker(GPlayer player, Packet packet)
        {
            var PwdInput = packet.ReadPStr();
            // senha diferente
            if (player.LockerPWD != PwdInput)
            {
                //senha incorreta 
                player.SendResponse(new byte[] { 0x6C, 0x01, 0x75, 0x00, 0x00, 0x00 });
                return;
            }
            else
                player.SendResponse(new byte[] { 0x6C, 0x01, 0x00, 0x00, 0x00, 0x00 });
        }

        public void PlayerChangeLockerPwd(GPlayer player, Packet packet)
        {
            var OLDPWD = packet.ReadPStr();
            var NEWPWD = packet.ReadPStr();

            // forem diferentes 
            if (player.LockerPWD != OLDPWD)
            {
                player.SendResponse(new byte[] { 0x6C, 0x01, 0x75, 0x00, 0x00, 0x00 });
                return;
            }
            
            if (NEWPWD.Length >= 4)
            {
                using (var _db = DbContextFactory.Create())
                {
                    int rowsAffected = _db.Database.ExecuteSqlCommand(
                        "UPDATE pangya_personal SET lockerpwd = @p0 WHERE uid = @p1",
                        NEWPWD, (int)player.GetUID);

                    if (rowsAffected == 0)
                        return;

                    player.LockerPWD = NEWPWD;

                    player.SendResponse(new byte[] { 0x74, 0x01, 0x00, 0x00, 0x00, 0x00, });
                }
            }
        }

        public void PlayerGetPangLocker(GPlayer player)
        {
            player.SendLockerPang();
            List<LockerItem> item;

            using (var _db = DbContextFactory.Create())
            {
                // Use raw SQL query instead of stored procedure
                string sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM pangya_locker WHERE uid = @p0) as TOTAL_PAGE,
                        item_id as INVEN_ID,
                        typeid as TypeID,
                        ucc_unique as UCC_UNIQE,
                        ucc_status as UCC_STATUS,
                        ucc_name as UCC_NAME,
                        ucc_cocount as UCC_COCOUNT,
                        ucc_drawer_nickname as NICKNAME
                    FROM pangya_locker 
                    WHERE uid = @p0
                    LIMIT 20 OFFSET 0";
                
                item = _db.Database.SqlQuery<LockerItem>(sql, (int)player.GetUID).ToList();

                player.Response.Write(new byte[] { 0x6D, 0x01 });
                if (item.Count == 0)
                {
                    player.Response.WriteZero(5);
                }
                else
                {
                    ushort TotalPage = (ushort)item.Count;

                    player.Response.Write(TotalPage);
                    player.Response.Write(TotalPage);
                    foreach (var data in item)
                    {
                        player.Response.Write((byte)item.Count);
                        player.Response.Write((uint)data.INVEN_ID);
                        player.Response.Write((uint)0);
                        player.Response.Write((uint)data.TypeID);
                        player.Response.Write((uint)0);
                        player.Response.Write(player.GetUID);
                        player.Response.Write((uint)1);
                        player.Response.WriteZero(23);
                        player.Response.WriteStr(data.UCC_UNIQE, 9);
                        player.Response.Write(Convert.ToUInt16(data.UCC_COCOUNT ?? 0));
                        player.Response.Write(data.UCC_STATUS);
                        player.Response.WriteZero(0x36);
                        player.Response.WriteStr(data.UCC_NAME, 16);
                        player.Response.WriteZero(0x19);
                        player.Response.WriteStr(data.NICKNAME, 0x16);
                    }
                }
                player.SendResponse();
            }
        }

        public void PlayerGetLockerItem(GPlayer player, Packet packet)
        {
            List<LockerItem> item = new List<LockerItem>();

            var TotalPage = (ushort)Math.Ceiling(a: 20 * 1.0);

            //dados não utilizados
            uint Unknown = packet.ReadUInt32();
            int Pages = packet.ReadUInt16();
            
            using (var _db = DbContextFactory.Create())
            {
                // Calculate offset for pagination
                int offset = (Pages - 1) * 20;
                
                string sql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM pangya_locker WHERE uid = @p0) as TOTAL_PAGE,
                        item_id as INVEN_ID,
                        typeid as TypeID,
                        ucc_unique as UCC_UNIQE,
                        ucc_status as UCC_STATUS,
                        ucc_name as UCC_NAME,
                        ucc_cocount as UCC_COCOUNT,
                        ucc_drawer_nickname as NICKNAME
                    FROM pangya_locker 
                    WHERE uid = @p0
                    LIMIT 20 OFFSET @p1";
                
                item = _db.Database.SqlQuery<LockerItem>(sql, (int)player.GetUID, offset).ToList();

                player.Response.Write(new byte[] { 0x6D, 0x01 });
                if (item.Count == 0)
                {
                    player.Response.WriteZero(5);
                }
                else
                {
                    player.Response.Write(TotalPage); // total page
                    player.Response.Write((ushort)Pages); //page current   
                    player.Response.Write((byte)item.Count);
                    foreach (var data in item)
                    {
                        player.Response.Write((uint)data.INVEN_ID);
                        player.Response.Write((uint)0);
                        player.Response.Write((uint)data.TypeID);
                        player.Response.Write((uint)0);
                        player.Response.Write(player.GetUID);//??
                        player.Response.WriteZero(0x1B);
                        player.Response.WriteStr(data.UCC_UNIQE, 9);
                        player.Response.Write((ushort?)data.UCC_COCOUNT ?? 0);
                        player.Response.Write(data.UCC_STATUS);
                        player.Response.WriteZero(0x36);
                        player.Response.WriteStr(data.UCC_NAME, 16);
                        player.Response.WriteZero(0x19);
                        player.Response.WriteStr(data.NICKNAME, 0x16);
                    }
                }
                player.SendResponse();
            }
        }
        // 6B = The process is not yet finished
        // 6C = You have too many items, cannot be put more
        // 6D = item can not be put in locker
        // 6E = item can be expired, cannot be put it locker
        // 6F = Cannot be put the amount of item more than you have
        // 70 = The process is finished // automatically close the locker
        public void PlayerPutItemLocker(GPlayer player, Packet packet)
        {

            //dados não utilizados
            var PlayerID = packet.ReadUInt32();
            packet.Skip(5);
            var TypeID = packet.ReadUInt32();
            var Index = packet.ReadUInt32();

            var GetItem = player.Inventory.ItemWarehouse.GetItem(Index);

            if (null == GetItem)
            {
                player.SendResponse(new byte[] { 0x6E, 0x01, 0x6B, 0x00, 0x00, 0x00 });
                return;
            }

            if (!(GameTools.Tools.GetItemGroup(GetItem.ItemTypeID) == 2))
            {
                player.SendResponse(new byte[] { 0x6E, 0x01, 0x6D, 0x00, 0x00, 0x00 });
                return;
            }
            
            using (var _db = DbContextFactory.Create())
            {
                // Insert item into locker
                string itemName = IffEntry.GetItemName(GetItem.ItemTypeID);
                
                string insertSql = @"
                    INSERT INTO pangya_locker 
                    (uid, typeid, item_name, warehouse_item_id, ucc_unique, ucc_status, ucc_name, ucc_drawer_uid, ucc_drawer_nickname, ucc_cocount)
                    VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";
                
                try
                {
                    int rowsAffected = _db.Database.ExecuteSqlCommand(insertSql,
                        (int)player.GetUID,
                        (int)GetItem.ItemTypeID,
                        itemName,
                        (int)GetItem.ItemIndex,
                        GetItem.ItemUCCUnique ?? "",
                        GetItem.ItemUCCStatus ?? 0,
                        GetItem.ItemUCCName ?? "",
                        GetItem.ItemUCCDrawerUID ?? 0,
                        GetItem.ItemUCCDrawer ?? "",
                        GetItem.ItemUCCCopyCount ?? 0);

                    if (rowsAffected == 0)
                    {
                        player.SendResponse(new byte[] { 0x6E, 0x01, 0x6B, 0x00, 0x00, 0x00 });
                        return;
                    }

                    if (player.Inventory.ItemWarehouse.RemoveItem(GetItem))
                    {
                        player.SendResponse(new byte[] { 0x39, 0x01, 0x00, 0x00 });

                        player.Response.Write(new byte[] { 0xEC, 0x00 });
                        player.Response.Write(1);
                        player.Response.Write(1);
                        player.Response.WriteStr("", 9);
                        player.Response.Write(GetItem.ItemTypeID);
                        player.Response.Write(GetItem.ItemIndex);
                        player.Response.Write(player.GetUID);//quantity
                        player.Response.WriteZero(27);
                        player.Response.WriteStr(GetItem.ItemUCCUnique, 9);
                        player.Response.Write(GetItem.ItemUCCCopyCount ?? 0);
                        player.Response.Write(GetItem.ItemUCCStatus ?? 0);
                        player.Response.WriteZero(54);
                        player.Response.WriteStr(GetItem.ItemUCCName, 16);
                        player.Response.WriteZero(25);
                        player.Response.WriteStr(GetItem.ItemUCCDrawer, 22);
                        player.SendResponse();

                        player.Response.Write(new byte[] { 0x6E, 0x01 });
                        player.Response.WriteZero(12);
                        player.Response.Write(GetItem.ItemTypeID);
                        player.Response.Write(GetItem.ItemIndex);
                        player.Response.Write(player.GetUID);//quantity
                        player.Response.WriteZero(27);
                        player.Response.WriteStr(GetItem.ItemUCCUnique, 9);
                        player.Response.Write(GetItem.ItemUCCCopyCount ?? 0);
                        player.Response.Write(GetItem.ItemUCCStatus ?? 0);
                        player.Response.WriteZero(54);
                        player.Response.WriteStr(GetItem.ItemUCCName, 16);
                        player.Response.WriteZero(25);
                        player.Response.WriteStr(GetItem.ItemUCCDrawer, 22);
                        player.SendResponse();
                    }
                }
                catch (Exception ex)
                {
                    WriteConsole.WriteLine($"[LOCKER_PUT_ERROR]: {ex.Message}", ConsoleColor.Red);
                    player.SendResponse(new byte[] { 0x6E, 0x01, 0x6B, 0x00, 0x00, 0x00 });
                }
            }
        }

        public void PlayerTalkItemLocker(GPlayer player, Packet packet)
        {
            PlayerItemData Item;

            var count = packet.ReadByte();//count item
            var Index = packet.ReadInt32();//id do item

            using (var _db = DbContextFactory.Create())
            {
                // Get item from locker
                string selectSql = @"
                    SELECT 
                        item_id as ITEM_ID,
                        typeid as TYPEID,
                        c0 as C0, c1 as C1, c2 as C2, c3 as C3, c4 as C4,
                        date_end as DateEnd,
                        flag as FLAG,
                        ucc_unique as UCC_UNIQE,
                        ucc_status as UCC_STATUS,
                        ucc_name as UCC_NAME,
                        ucc_drawer_uid as UCC_DRAWER_UID,
                        ucc_drawer_nickname as UCC_DRAWER_NICKNAME,
                        ucc_cocount as UCC_COCOUNT
                    FROM pangya_locker
                    WHERE uid = @p0 AND item_id = @p1
                    LIMIT 1";

                var lockerItems = _db.Database.SqlQuery<dynamic>(selectSql, (int)player.GetUID, Index).ToList();

                if (lockerItems.Count == 0)
                {
                    player.SendResponse(new byte[] { 0x6f, 0x01, 0x6B, 0x00, 0x00, 0x00 });
                    return;
                }

                var invent = lockerItems[0];

                // Delete from locker
                string deleteSql = "DELETE FROM pangya_locker WHERE uid = @p0 AND item_id = @p1";
                int deleted = _db.Database.ExecuteSqlCommand(deleteSql, (int)player.GetUID, Index);

                if (deleted == 0)
                {
                    player.SendResponse(new byte[] { 0x6f, 0x01, 0x6B, 0x00, 0x00, 0x00 });
                    return;
                }

                Item = new PlayerItemData();
                Item.CreateNewItem();

                Item.ItemIndex = (uint)(invent.ITEM_ID ?? 0);
                Item.ItemTypeID = (uint)(invent.TYPEID ?? 0);
                Item.ItemC0 = (ushort)(invent.C0 ?? 0);
                Item.ItemC1 = (ushort)(invent.C1 ?? 0);
                Item.ItemC2 = (ushort)(invent.C2 ?? 0);
                Item.ItemC3 = (ushort)(invent.C3 ?? 0);
                Item.ItemC4 = (ushort)(invent.C4 ?? 0);
                Item.ItemEndDate = invent.DateEnd;
                Item.ItemFlag = invent.FLAG ?? 0;
                Item.ItemUCCUnique = invent.UCC_UNIQE ?? "";
                Item.ItemUCCStatus = (byte?)(invent.UCC_STATUS ?? 0);
                Item.ItemUCCName = invent.UCC_NAME ?? "";
                Item.ItemUCCDrawerUID = (uint?)(invent.UCC_DRAWER_UID ?? 0);
                Item.ItemUCCDrawer = invent.UCC_DRAWER_NICKNAME ?? "";
                Item.ItemUCCCopyCount = (ushort?)(invent.UCC_COCOUNT ?? 0);
                
                // Add to inventory
                player.Inventory.ItemWarehouse.Add(Item);

                player.Response.Write(new byte[] { 0xEC, 0x00 });
                player.Response.Write((byte)1);
                player.Response.Write(0);
                player.Response.Write(player.GetPang);
                player.Response.WriteZero(8);
                player.Response.Write(Item.ItemTypeID);
                player.Response.Write(Item.ItemIndex);
                player.Response.Write(player.GetUID);//quantity
                player.Response.WriteZero(27);
                player.Response.WriteStr(Item.ItemUCCUnique, 9);
                player.Response.Write(Item.ItemUCCCopyCount ?? 0);
                player.Response.Write(Item.ItemUCCStatus ?? 0);
                player.Response.WriteZero(54);
                player.Response.WriteStr(Item.ItemUCCName, 16);
                player.Response.WriteZero(25);
                player.Response.WriteStr(Item.ItemUCCDrawer, 16);
                player.Response.WriteZero(6);
                player.Response.Write((byte)3);
                player.Response.Write(player.GetUID);
                player.Response.Write(Item.ItemTypeID);
                player.Response.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                player.Response.Write(1);
                player.Response.WriteZero(6);
                player.Response.Write(1);
                player.Response.WriteZero(0x0E);
                player.Response.Write((byte)2);
                player.Response.WriteStr(Item.ItemUCCName, 16);
                player.Response.WriteZero(25);
                player.Response.WriteStr(Item.ItemUCCUnique, 9);
                player.Response.Write(Item.ItemUCCStatus ?? 0);
                player.Response.Write(Item.ItemUCCCopyCount ?? 0);
                player.Response.WriteStr(Item.ItemUCCDrawer, 16);
                player.Response.WriteZero(0x4E);
                player.Response.Write(0);
                player.Response.Write(0);
                player.SendResponse();

                player.Response.Write(new byte[] { 0x6F, 0x01 });
                player.Response.Write(0);
                player.Response.Write(Index);
                player.Response.Write(0);
                player.Response.Write(Item.ItemIndex);
                player.Response.Write(player.GetUID);//quantity
                player.Response.Write(Item.ItemTypeID);
                player.Response.WriteZero(27);
                player.Response.WriteStr(Item.ItemUCCUnique, 9);
                player.Response.Write(Item.ItemUCCCopyCount ?? 0);
                player.Response.Write(Item.ItemUCCStatus ?? 0);
                player.Response.WriteZero(54);
                player.Response.WriteStr(Item.ItemUCCName, 16);
                player.Response.WriteZero(25);
                player.Response.WriteStr(Item.ItemUCCDrawer, 22);
                player.SendResponse();
            }
        }

        public void PlayerPangControlLocker(GPlayer player, Packet packet)
        {
            //120 =você inseriu um valor maior do que o permitido
            //111 = valor de entrada maior do que o que você tem
            //100 falhou
            //101 falied
            void SendCode(uint Code = 0)
            {
                player.Response.Write(new byte[] { 0x71, 0x01, });
                player.Response.Write(Code);
                player.SendResponse();
            }
            var Action = packet.ReadByte();
            var Pang = packet.ReadUInt64();

            bool Check()
            {
                return player.GetPang <= 0 || Pang <= 0;
            }

            try
            {
                if (!Check())
                {
                    SendCode(110);
                }
                else
                {
                    SendCode();
                    switch (Action)
                    {
                        case 0: //puxa pangs 
                            {
                                try
                                {
                                    if (player.RemoveLockerPang((uint)Pang))
                                    {
                                        player.AddPang((uint)Pang);
                                    }
                                }
                                catch
                                {
                                    SendCode(100);
                                    return;
                                }
                            }
                            break;
                        case 1:  //guarda pangs
                            {
                                try
                                {
                                    if (player.RemovePang((uint)Pang))
                                    {
                                        player.AddLockerPang((uint)Pang);
                                    }
                                }
                                catch
                                {
                                    SendCode(100);
                                    return;
                                }
                            }
                            break;
                    }

                    //reload pangs(reenvia os pangs na caixa de pangs do client)
                    player.SendPang();

                    //SendPangs in dolfine(envia os pangs na caixa do dolfine)
                    player.SendLockerPang();
                    
                    using (var _db = DbContextFactory.Create())
                    {
                        // Log the personal action
                        string logSql = @"
                            INSERT INTO pangya_personal_log (uid, action_type, locker_pang, log_date)
                            VALUES (@p0, @p1, @p2, NOW())";
                        
                        _db.Database.ExecuteSqlCommand(logSql, (int)player.GetUID, Action, (int)player.LockerPang);
                    }
                }
            }
            catch
            {
                SendCode(100);
            }
        }
    }
}
