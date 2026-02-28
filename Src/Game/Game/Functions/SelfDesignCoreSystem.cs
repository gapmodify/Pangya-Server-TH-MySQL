using Connector.DataBase;
using  PangyaAPI.BinaryModels;
using System.Linq;
using Game.Client;
using Game.Client.Inventory;
using PangyaAPI;
using Game.Client.Inventory.Data.Warehouse;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using System;

namespace Game.Functions
{
    public class SelfDesignCoreSystem
    {
        public void PlayerRequestUploadKey(GPlayer player, Packet packet)
        {
            byte Option;
            uint ITEMID;
            PlayerItemData ItemUCC;
            
            Option = packet.ReadByte();
            switch (Option)
            {
                case 0:
                    {
                        var typeID = packet.ReadInt32();//meu uid
                        packet.Skip(1);// Skip for ununsed data
                        ITEMID = packet.ReadUInt32();
                        ItemUCC = player.Inventory.ItemWarehouse.GetUCC(ITEMID);
                        if (ItemUCC == null)
                        {
                            return;
                        }

                        using (var db = DbContextFactory.Create())
                        {
                            // Generate UCC upload key
                            string uccKey = Guid.NewGuid().ToString("N").Substring(0, 8);
                            
                            // Save UCC key to database
                            string sql = @"
                                UPDATE pangya_warehouse 
                                SET ucc_unique = @p0, ucc_status = 0 
                                WHERE uid = @p1 AND item_id = @p2";
                            
                            int result = db.Database.ExecuteSqlCommand(sql, uccKey, (int)player.GetUID, (int)ITEMID);

                            if (result == 0)
                            {
                                WriteConsole.WriteLine($"[UCC_REQUEST] Failed to generate key for item {ITEMID}", ConsoleColor.Red);
                                return;
                            }

                            player.Response.Write(new byte[] { 0x53, 0x01, Option });
                            player.Response.Write((byte)1); // Unknown now
                            player.Response.WriteUInt32(ITEMID);
                            player.Response.WritePStr(uccKey);
                            player.Response.Write((byte)1); // Unknown now
                            player.SendResponse();
                        }
                    }
                    break;
            }
        }

        public void PlayerAfterUploaded(GPlayer player, Packet packet)
        {
            byte Option;
            byte Cases;
            uint TypeId;
            uint UCC_IDX;
            string UCC_UNIQUE;
            string UCC_NAME;
            PlayerItemData Item = null;
            TSaveUCC UCC_SAVE = new TSaveUCC();
            Option = packet.ReadByte();
            
            switch (Option)
            {
                // Save Permanently
                case 0:
                    {
                        TypeId = packet.ReadUInt32();
                        UCC_UNIQUE = packet.ReadPStr();//key?
                        UCC_NAME = packet.ReadPStr();

                        Item = player.Inventory.ItemWarehouse.GetUCC(TypeId, UCC_UNIQUE);

                        if (Item == null)
                        {
                            Item = player.Inventory.ItemWarehouse.GetUCC(TypeId, UCC_UNIQUE);
                            return;
                        }
                        if (!(Item == null))
                        {
                            Item.ItemUCCStatus = 1;
                            Item.ItemUCCName = UCC_NAME;
                            Item.ItemUCCDrawerUID = (uint)player.GetUID;
                            Item.ItemNeedUpdate = false;
                            UCC_SAVE.UID = (uint)player.GetUID;
                            UCC_SAVE.UCCIndex = Item.ItemIndex;
                            UCC_SAVE.UCCName = UCC_NAME;
                            UCC_SAVE.UCCStatus = (byte)Item.ItemUCCStatus;
                            UCC_SAVE.UccDrawerUID = (uint)player.GetUID;
                            // SAVE TO DATABASE
                            SaveUCC(UCC_SAVE);
                        }
                        player.Response.Write(new byte[] { 0x2E, 0x01, 0x00, 0x01 });
                        player.Response.WriteUInt32(Item.ItemIndex);
                        player.Response.WriteUInt32(Item.ItemTypeID);
                        player.Response.WritePStr(Item.ItemUCCUnique);
                        player.Response.WritePStr(UCC_NAME);
                        player.SendResponse();
                        break;
                    }
                // UCC INFO
                case 1:
                    {
                        UCC_IDX = packet.ReadUInt32();
                        Cases = packet.ReadByte();

                        if ((UCC_IDX == 0))
                        {
                            player.SendResponse(new byte[] { 0x2E, 0x01, 0x04 });
                            return;
                        }
                        
                        using (var db = DbContextFactory.Create())
                        {
                            try
                            {
                                string sql = @"
                                    SELECT 
                                        w.item_id, w.typeid, w.ucc_unique as UCC_UNIQE, 
                                        w.ucc_name as UCC_NAME, w.ucc_status as UCC_STATUS, 
                                        w.ucc_cocount as UCC_COCOUNT,
                                        m.nickname as Nickname
                                    FROM pangya_warehouse w
                                    LEFT JOIN pangya_member m ON w.uid = m.uid
                                    WHERE w.item_id = @p0";
                                
                                var data = db.Database.SqlQuery<dynamic>(sql, (int)UCC_IDX).ToList();
                                
                                if (data.Count <= 0)
                                {
                                    return;
                                }

                                player.Response.Write(new byte[] { 0x2E, 0x01, 0x01 });
                                foreach (var Query in data)
                                {
                                    player.Response.WriteInt32((int)(Query.typeid ?? 0));
                                    player.Response.WritePStr((string)(Query.UCC_UNIQE ?? ""));
                                    player.Response.WriteByte(1);
                                    player.Response.WriteInt32((int)(Query.item_id ?? 0));
                                    player.Response.WriteInt32((int)(Query.typeid ?? 0));
                                    player.Response.WriteZero(0xF);
                                    player.Response.WriteByte(1);
                                    player.Response.WriteZero(0x10);
                                    player.Response.WriteByte(2);
                                    player.Response.WriteStr((string)(Query.UCC_NAME ?? ""), 0x10);
                                    player.Response.WriteZero(0x19);
                                    player.Response.WriteStr((string)(Query.UCC_UNIQE ?? ""), 0x9);
                                    player.Response.WriteByte((byte)(Query.UCC_STATUS ?? 0));
                                    player.Response.WriteUInt16((ushort)(Query.UCC_COCOUNT ?? 0));
                                    player.Response.WriteStr((string)(Query.Nickname ?? ""), 0x10);
                                    player.Response.WriteZero(0x56);
                                }
                                player.SendResponse();
                            }
                            catch (Exception ex)
                            {
                                WriteConsole.WriteLine($"[UCC_INFO_ERROR]: {ex.Message}", ConsoleColor.Red);
                            }
                        }
                        break;
                    }
                // COPY UCC
                case 2:
                    {
                        TypeId = packet.ReadUInt32();
                        UCC_UNIQUE = packet.ReadPStr();
                        packet.Skip(2);
                        UCC_IDX = packet.ReadUInt32();

                        // IDX TO COPY
                        Item = player.Inventory.ItemWarehouse.GetUCC(TypeId, UCC_UNIQUE, true);
                        if (Item == null)
                        {
                            return;
                        }
                        
                        using (var db = DbContextFactory.Create())
                        {
                            try
                            {
                                // Get source UCC data
                                string selectSql = @"
                                    SELECT ucc_unique, ucc_name, ucc_status, ucc_cocount 
                                    FROM pangya_warehouse 
                                    WHERE item_id = @p0";
                                
                                var sourceData = db.Database.SqlQuery<dynamic>(selectSql, (int)UCC_IDX).FirstOrDefault();
                                
                                if (sourceData == null)
                                {
                                    return;
                                }

                                // Update player's item with copied UCC data
                                string updateSql = @"
                                    UPDATE pangya_warehouse 
                                    SET ucc_unique = @p0, ucc_name = @p1, ucc_status = @p2, 
                                        ucc_cocount = COALESCE(ucc_cocount, 0) + 1
                                    WHERE uid = @p3 AND typeid = @p4 AND ucc_unique = @p5";
                                
                                int result = db.Database.ExecuteSqlCommand(updateSql,
                                    (string)(sourceData.ucc_unique ?? ""),
                                    (string)(sourceData.ucc_name ?? ""),
                                    (byte)(sourceData.ucc_status ?? 0),
                                    (int)player.GetUID,
                                    (int)TypeId,
                                    UCC_UNIQUE);

                                if (result == 0)
                                {
                                    return;
                                }

                                // Get the updated item info
                                string getSql = "SELECT item_id, typeid, ucc_unique, ucc_cocount FROM pangya_warehouse WHERE uid = @p0 AND typeid = @p1 LIMIT 1";
                                var updatedItem = db.Database.SqlQuery<dynamic>(getSql, (int)player.GetUID, (int)TypeId).FirstOrDefault();

                                player.Response.Write(new byte[] { 0x2E, 0x01, 0x02 });
                                player.Response.WriteUInt32(TypeId);
                                player.Response.WritePStr(UCC_UNIQUE);
                                player.Response.Write(new byte[] { 0x01, 0x00 }); // UNKNOWN YET                          
                                player.Response.WriteUInt32(UCC_IDX);
                                player.Response.WriteInt32((int)(updatedItem?.item_id ?? 0));
                                player.Response.WriteInt32((int)(updatedItem?.typeid ?? 0));
                                player.Response.WritePStr((string)(updatedItem?.ucc_unique ?? ""));
                                player.Response.Write((ushort)(updatedItem?.ucc_cocount ?? 0));
                                player.Response.Write((byte)1);
                                player.SendResponse();
                            }
                            catch (Exception ex)
                            {
                                WriteConsole.WriteLine($"[UCC_COPY_ERROR]: {ex.Message}", ConsoleColor.Red);
                            }
                        }
                        break;
                    }
                // SAVE TEMPARARILY
                case 3:
                    {
                        TypeId = packet.ReadUInt32();
                        UCC_UNIQUE = packet.ReadPStr();

                        player.Response.Write(new byte[] { 0x2E, 0x01 });
                        player.Response.Write(Option);
                        player.Response.WriteUInt32(TypeId);
                        player.Response.WritePStr(UCC_UNIQUE);
                        Item = player.Inventory.ItemWarehouse.GetUCC(TypeId, UCC_UNIQUE);
                        if (Item == null)
                        {
                            player.Response.Write((byte)0);
                        }
                        if (!(Item == null))
                        {
                            Item.ItemUCCStatus = 2;
                            Item.ItemNeedUpdate = true;
                            player.Response.Write((byte)1);
                        }
                        player.SendResponse();
                        break;
                    }
            }
        }

        class TSaveUCC
        {
            public uint UID { get; set; }
            public uint UCCIndex { get; set; }
            public string UCCName { get; set; }
            public byte UCCStatus { get; set; }
            public uint UccDrawerUID { get; set; }
        }

        static void SaveUCC(TSaveUCC Data)
        {
            using (var db = DbContextFactory.Create())
            {
                string sql = @"
                    UPDATE pangya_warehouse 
                    SET ucc_name = @p0, ucc_status = @p1, ucc_drawer_uid = @p2
                    WHERE uid = @p3 AND item_id = @p4";
                
                db.Database.ExecuteSqlCommand(sql, 
                    Data.UCCName, 
                    Data.UCCStatus, 
                    (int)Data.UccDrawerUID,
                    (int)Data.UID,
                    (int)Data.UCCIndex);
            }
        }
    }
}
