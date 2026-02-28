using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_item_daily_log : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_item_daily_log(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_item_daily_log() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public ItemDailyLogData SelectByCounter(int counter)
        {
            string sql = "SELECT * FROM pangya_item_daily_log WHERE Counter = @p0";
            return _db.Database.SqlQuery<ItemDailyLogData>(sql, counter).FirstOrDefault();
        }

        public List<ItemDailyLogData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_item_daily_log WHERE UID = @p0 ORDER BY RegDate DESC";
            return _db.Database.SqlQuery<ItemDailyLogData>(sql, uid).ToList();
        }

        public ItemDailyLogData SelectLatestByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_item_daily_log WHERE UID = @p0 ORDER BY RegDate DESC LIMIT 1";
            return _db.Database.SqlQuery<ItemDailyLogData>(sql, uid).FirstOrDefault();
        }

        public List<ItemDailyLogData> SelectByDate(DateTime date)
        {
            string sql = "SELECT * FROM pangya_item_daily_log WHERE RegDate = @p0";
            return _db.Database.SqlQuery<ItemDailyLogData>(sql, date.Date).ToList();
        }

        #endregion

        #region INSERT Methods

        public int Insert(ItemDailyLogData data)
        {
            string sql = @"INSERT INTO pangya_item_daily_log 
                (UID, Item_TypeID, Item_Quantity, Item_TypeID_Next, Item_Quantity_Next, LoginCount, RegDate) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.Item_TypeID,
                data.Item_Quantity,
                data.Item_TypeID_Next,
                data.Item_Quantity_Next,
                data.LoginCount,
                data.RegDate
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(ItemDailyLogData data)
        {
            string sql = @"UPDATE pangya_item_daily_log SET 
                UID = @p0, Item_TypeID = @p1, Item_Quantity = @p2, Item_TypeID_Next = @p3, 
                Item_Quantity_Next = @p4, LoginCount = @p5, RegDate = @p6 
                WHERE Counter = @p7";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.Item_TypeID,
                data.Item_Quantity,
                data.Item_TypeID_Next,
                data.Item_Quantity_Next,
                data.LoginCount,
                data.RegDate,
                data.Counter
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int counter)
        {
            string sql = "DELETE FROM pangya_item_daily_log WHERE Counter = @p0";
            return _db.Database.ExecuteSqlCommand(sql, counter);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_item_daily_log WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int DeleteOldLogs(int days)
        {
            string sql = "DELETE FROM pangya_item_daily_log WHERE RegDate < DATE_SUB(NOW(), INTERVAL @p0 DAY)";
            return _db.Database.ExecuteSqlCommand(sql, days);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class ItemDailyLogData
    {
        public int Counter { get; set; }
        public int UID { get; set; }
        public int Item_TypeID { get; set; }
        public int Item_Quantity { get; set; }
        public int Item_TypeID_Next { get; set; }
        public int Item_Quantity_Next { get; set; }
        public int LoginCount { get; set; }
        public DateTime RegDate { get; set; }
    }
}
