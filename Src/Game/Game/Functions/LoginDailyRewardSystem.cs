using PangyaAPI.BinaryModels;
using PangyaAPI.Tools;
using Game.Client;
using System;
using System.Linq;
using Connector.DataBase;
using System.Data.Entity;

namespace Game.Functions
{
    public class DailyRewardResult
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ItemTypeID { get; set; }
        public int Quantity { get; set; }
        public int ItemType { get; set; }
    }

    public class DailyLoginLog
    {
        public int DayNumber { get; set; }
        public DateTime LoginDate { get; set; }
    }

    public class LoginDailyRewardSystem
    {
        private const string DailyLogTable = "pangya_item_daily_log";

        private static bool TableExists(DbContext db, string tableName)
        {
            try
            {
                const string sql = @"SELECT COUNT(*)
                                    FROM information_schema.TABLES
                                    WHERE TABLE_SCHEMA = DATABASE()
                                      AND TABLE_NAME = @p0";

                return db.Database.SqlQuery<int>(sql, tableName).FirstOrDefault() > 0;
            }
            catch
            {
                return false;
            }
        }

        public void PlayerDailyLoginItem(GPlayer player)
        {
            WriteConsole.WriteLine($"[DAILY_ITEM_ENTRY] Starting for UID:{player?.GetUID}", ConsoleColor.Magenta);
            PangyaBinaryWriter packet = new PangyaBinaryWriter();
            try
            {
                WriteConsole.WriteLine($"[DAILY_ITEM_STEP1] Player check...", ConsoleColor.Magenta);
                if (player == null || !player.Connected)
                {
                    WriteConsole.WriteLine($"[DAILY_ITEM_ABORT] Player null or disconnected", ConsoleColor.Red);
                    return;
                }

                WriteConsole.WriteLine($"[DAILY_ITEM_STEP2] Creating DB context...", ConsoleColor.Magenta);
                using (var db = DbContextFactory.Create())
                {
                    WriteConsole.WriteLine($"[DAILY_ITEM_STEP3] Checking tables...", ConsoleColor.Magenta);
                    if (!TableExists(db, DailyLogTable) || !TableExists(db, "pangya_item_daily"))
                    {
                        WriteConsole.WriteLine($"[DAILY_ITEM_ABORT] Tables don't exist", ConsoleColor.Red);
                        return;
                    }

                    WriteConsole.WriteLine($"[DAILY_ITEM_STEP4] Tables OK, proceeding...", ConsoleColor.Green);

                    int uid = (int)player.GetUID;
                    DateTime today = DateTime.Now.Date;

                    var lastLog = db.Database.SqlQuery<DailyLoginLog>(
                        "SELECT LoginCount AS DayNumber, RegDate AS LoginDate FROM pangya_item_daily_log WHERE UID = @p0 ORDER BY RegDate DESC LIMIT 1",
                        uid
                    ).FirstOrDefault();

                    int counter;
                    int code;

                    if (lastLog == null)
                    {
                        counter = 1;
                        code = 0;
                    }
                    else if (lastLog.LoginDate.Date == today)
                    {
                        counter = lastLog.DayNumber;
                        code = 1;
                    }
                    else if ((today - lastLog.LoginDate.Date).TotalDays == 1)
                    {
                        counter = (lastLog.DayNumber % 30) + 1;
                        code = 0;
                    }
                    else
                    {
                        counter = 1;
                        code = 0;
                    }

                    int nextCounter = (counter % 30) + 1;

                    var reward = db.Database.SqlQuery<DailyRewardResult>(
                        "SELECT ID, Name, ItemTypeID, Quantity, ItemType FROM pangya_item_daily WHERE ID = @p0 LIMIT 1",
                        counter
                    ).FirstOrDefault();

                    var nextReward = db.Database.SqlQuery<DailyRewardResult>(
                        "SELECT ID, Name, ItemTypeID, Quantity, ItemType FROM pangya_item_daily WHERE ID = @p0 LIMIT 1",
                        nextCounter
                    ).FirstOrDefault();

                    uint itemTypeId = 0;
                    uint qty = 0;
                    uint nextItemTypeId = 0;
                    uint nextQty = 0;

                    if (reward != null)
                    {
                        itemTypeId = (uint)Math.Max(0, reward.ItemTypeID);
                        qty = (uint)Math.Max(0, reward.Quantity);
                    }

                    if (nextReward != null)
                    {
                        nextItemTypeId = (uint)Math.Max(0, nextReward.ItemTypeID);
                        nextQty = (uint)Math.Max(0, nextReward.Quantity);
                    }

                    if (itemTypeId != 0 && qty == 0)
                        qty = 1;

                    if (nextItemTypeId != 0 && nextQty == 0)
                        nextQty = 1;

                    WriteConsole.WriteLine($"[DAILY_ITEM_PKT] UID:{player.GetUID} Day:{counter} CODE:{code}", ConsoleColor.Cyan);
                    WriteConsole.WriteLine($"[DAILY_ITEM_PKT]   Today: TypeID={itemTypeId} Qty={qty}", ConsoleColor.Yellow);
                    WriteConsole.WriteLine($"[DAILY_ITEM_PKT]   Next:  TypeID={nextItemTypeId} Qty={nextQty}", ConsoleColor.Yellow);

                    if (code == 0 && reward != null)
                    {
                        var mailIndex = SendDailyRewardMail(player, reward, counter, db);
                        if (mailIndex > 0)
                        {
                            db.Database.ExecuteSqlCommand(
                                $@"INSERT INTO {DailyLogTable}
                                   (UID, LoginCount, RegDate)
                                   VALUES (@p0, @p1, @p2)",
                                (int)player.GetUID,
                                counter,
                                DateTime.Now
                            );
                        }
                    }

                    packet.Write(new byte[] { 0x49, 0x02 });
                    packet.WriteUInt32(0);
                    packet.WriteByte((byte)(code));
                    packet.WriteUInt32(itemTypeId);
                    packet.WriteUInt32(qty);
                    packet.WriteUInt32(nextItemTypeId);
                    packet.WriteUInt32(nextQty);
                    packet.WriteInt32(counter);

                    player.SendResponse(packet);
                }

                try { player.SendMailPopup(); } catch { }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[DAILY_ITEM_EXCEPTION] {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[DAILY_ITEM_STACKTRACE] {ex.StackTrace}", ConsoleColor.DarkRed);
                SendEmptyReward(player, 0x49);
            }
            finally
            {
                packet?.Dispose();
            }
        }

        public void PlayerDailyLoginCheck(GPlayer player, byte code)
        {
            PangyaBinaryWriter packet = new PangyaBinaryWriter();
            try
            {
                if (player == null || !player.Connected)
                    return;

                using (var db = DbContextFactory.Create())
                {
                    if (!TableExists(db, DailyLogTable) || !TableExists(db, "pangya_item_daily"))
                        return;

                    int uid = (int)player.GetUID;
                    DateTime today = DateTime.Now.Date;

                    var lastLog = db.Database.SqlQuery<DailyLoginLog>(
                        "SELECT LoginCount AS DayNumber, RegDate AS LoginDate FROM pangya_item_daily_log WHERE UID = @p0 ORDER BY RegDate DESC LIMIT 1",
                        uid
                    ).FirstOrDefault();

                    int counter;
                    int CODE;

                    if (lastLog == null)
                    {
                        counter = 1;
                        CODE = 0;
                    }
                    else if (lastLog.LoginDate.Date == today)
                    {
                        counter = lastLog.DayNumber;
                        CODE = 1;
                    }
                    else if ((today - lastLog.LoginDate.Date).TotalDays == 1)
                    {
                        counter = (lastLog.DayNumber % 30) + 1;
                        CODE = 0;
                    }
                    else
                    {
                        counter = 1;
                        CODE = 0;
                    }

                    int nextCounter = (counter % 30) + 1;

                    var reward = db.Database.SqlQuery<DailyRewardResult>(
                        "SELECT ID, Name, ItemTypeID, Quantity, ItemType FROM pangya_item_daily WHERE ID = @p0 LIMIT 1",
                        counter
                    ).FirstOrDefault();

                    var nextReward = db.Database.SqlQuery<DailyRewardResult>(
                        "SELECT ID, Name, ItemTypeID, Quantity, ItemType FROM pangya_item_daily WHERE ID = @p0 LIMIT 1",
                        nextCounter
                    ).FirstOrDefault();

                    uint itemTypeId = 0;
                    uint qty = 0;
                    uint nextItemTypeId = 0;
                    uint nextQty = 0;

                    if (reward != null)
                    {
                        itemTypeId = (uint)Math.Max(0, reward.ItemTypeID);
                        qty = (uint)Math.Max(0, reward.Quantity);
                    }

                    if (nextReward != null)
                    {
                        nextItemTypeId = (uint)Math.Max(0, nextReward.ItemTypeID);
                        nextQty = (uint)Math.Max(0, nextReward.Quantity);
                    }

                    if (itemTypeId != 0 && qty == 0)
                        qty = 1;

                    if (nextItemTypeId != 0 && nextQty == 0)
                        nextQty = 1;

                    WriteConsole.WriteLine($"[DAILY_CHECK_PKT] UID:{player.GetUID} Day:{counter} CODE:{CODE}", ConsoleColor.Cyan);
                    WriteConsole.WriteLine($"[DAILY_CHECK_PKT]   Today: TypeID={itemTypeId} Qty={qty}", ConsoleColor.Yellow);
                    WriteConsole.WriteLine($"[DAILY_CHECK_PKT]   Next:  TypeID={nextItemTypeId} Qty={nextQty}", ConsoleColor.Yellow);

                    packet.Write(new byte[] { 0x48, 0x02 });
                    packet.WriteUInt32(0);
                    packet.WriteByte((byte)(CODE));
                    packet.WriteUInt32(itemTypeId);
                    packet.WriteUInt32(qty);
                    packet.WriteUInt32(nextItemTypeId);
                    packet.WriteUInt32(nextQty);
                    packet.WriteInt32(counter);

                    player.SendResponse(packet.GetBytes());
                }
            }
            catch
            {
                SendEmptyReward(player, 0x48);
            }
            finally
            {
                packet?.Dispose();
            }
        }

        private int SendDailyRewardMail(GPlayer player, DailyRewardResult reward, int dayNumber, DbContext db)
        {
            try
            {
                var warehouseInsert = $@"INSERT INTO pangya_warehouse 
                    (UID, TYPEID, C0, RegDate, DateEnd, VALID, ItemType, Flag) 
                    VALUES (
                        {(int)player.GetUID}, 
                        {reward.ItemTypeID}, 
                        {reward.Quantity},
                        NOW(3),
                        NOW(3),
                        1,
                        0,
                        0
                    )";

                db.Database.ExecuteSqlCommand(warehouseInsert);

                var mailInsert = $@"INSERT INTO pangya_mail 
                    (UID, Sender, Sender_UID, Receiver, Receiver_UID, Subject, Msg, RegDate) 
                    VALUES (
                        {(int)player.GetUID}, 
                        '@Daily Reward', 
                        0,
                        '{player.GetNickname}',
                        {(int)player.GetUID},
                        'Daily Login Reward', 
                        'Day {dayNumber} Login Reward: {reward.Name} x{reward.Quantity} has been added to your warehouse!', 
                        NOW(3)
                    )";

                db.Database.ExecuteSqlCommand(mailInsert);

                var mailIndex = db.Database.SqlQuery<int>(
                    "SELECT LAST_INSERT_ID()"
                ).FirstOrDefault();

                return mailIndex > 0 ? mailIndex : 1;
            }
            catch
            {
                return 0;
            }
        }

        private void SendEmptyReward(GPlayer player, byte packetId)
        {
            using (var packet = new PangyaBinaryWriter())
            {
                packet.Write(new byte[] { packetId, 0x02 });
                packet.WriteUInt32(0);
                packet.WriteByte(1);
                packet.WriteUInt32(0);
                packet.WriteUInt32(0);
                packet.WriteUInt32(0);
                packet.WriteUInt32(0);
                packet.WriteInt32(0);

                if (packetId == 0x48)
                    player.SendResponse(packet.GetBytes());
                else
                    player.SendResponse(packet);
            }
        }

        private sealed class ProcAlterDailyRow
        {
            public int counter { get; set; }
            public int CODE { get; set; }
            public int Item_TypeID { get; set; }
            public int Item_Quantity { get; set; }
            public int Item_TypeID_Next { get; set; }
            public int Item_Quantity_Next { get; set; }
            public int LoginCount { get; set; }
        }
    }
}



