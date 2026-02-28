using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_item_daily : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_item_daily(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_item_daily() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public ItemDailyData SelectByID(int id)
        {
            string sql = "SELECT * FROM pangya_item_daily WHERE ID = @p0";
            return _db.Database.SqlQuery<ItemDailyData>(sql, id).FirstOrDefault();
        }

        public List<ItemDailyData> SelectAll()
        {
            string sql = "SELECT * FROM pangya_item_daily ORDER BY ID";
            return _db.Database.SqlQuery<ItemDailyData>(sql).ToList();
        }

        public ItemDailyData SelectByDay(int day)
        {
            string sql = "SELECT * FROM pangya_item_daily WHERE ID = @p0";
            return _db.Database.SqlQuery<ItemDailyData>(sql, day).FirstOrDefault();
        }

        #endregion

        #region INSERT Methods

        public int Insert(ItemDailyData data)
        {
            string sql = @"INSERT INTO pangya_item_daily 
                (Name, ItemTypeID, Quantity, ItemType) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.Name,
                data.ItemTypeID,
                data.Quantity,
                data.ItemType
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(ItemDailyData data)
        {
            string sql = @"UPDATE pangya_item_daily SET 
                Name = @p0, ItemTypeID = @p1, Quantity = @p2, ItemType = @p3 
                WHERE ID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Name,
                data.ItemTypeID,
                data.Quantity,
                data.ItemType,
                data.ID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM pangya_item_daily WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class ItemDailyData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int ItemTypeID { get; set; }
        public int? Quantity { get; set; }
        public int ItemType { get; set; }
    }
}
