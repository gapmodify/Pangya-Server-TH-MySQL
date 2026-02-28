using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_warehouse : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_warehouse(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_warehouse() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public WarehouseData SelectByItemID(int itemId, int uid)
        {
            string sql = "SELECT * FROM pangya_warehouse WHERE item_id = @p0 AND UID = @p1";
            return _db.Database.SqlQuery<WarehouseData>(sql, itemId, uid).FirstOrDefault();
        }

        public List<WarehouseData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_warehouse WHERE UID = @p0 AND VALID = 1";
            return _db.Database.SqlQuery<WarehouseData>(sql, uid).ToList();
        }

        public List<WarehouseData> SelectByUIDAndType(int uid, byte itemType)
        {
            string sql = "SELECT * FROM pangya_warehouse WHERE UID = @p0 AND ItemType = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<WarehouseData>(sql, uid, itemType).ToList();
        }

        public WarehouseData SelectByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT * FROM pangya_warehouse WHERE UID = @p0 AND TYPEID = @p1 AND VALID = 1 LIMIT 1";
            return _db.Database.SqlQuery<WarehouseData>(sql, uid, typeId).FirstOrDefault();
        }

        public bool ExistsByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_warehouse WHERE UID = @p0 AND TYPEID = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<int>(sql, uid, typeId).FirstOrDefault() > 0;
        }

        public int GetNextAvailableItemID(int uid)
        {
            // Find next available item_id > 13 for this user
            string sql = @"SELECT COALESCE(MAX(item_id), 13) + 1 AS next_id 
                          FROM pangya_warehouse 
                          WHERE UID = @p0 AND item_id > 13";
            
            int nextId = _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault();
            
            // Ensure we start from 14 minimum
            return nextId < 14 ? 14 : nextId;
        }

        #endregion

        #region INSERT Methods

        public int Insert(WarehouseData data)
        {
            // ? ไม่ระบุ item_id เพื่อให้ AUTO_INCREMENT ทำงาน
            string sql = @"INSERT INTO pangya_warehouse 
                (UID, TYPEID, C0, C1, C2, C3, C4, RegDate, DateEnd, VALID, ItemType, Flag) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, NOW(), @p7, @p8, @p9, @p10);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.TYPEID,
                data.C0,
                data.C1,
                data.C2,
                data.C3,
                data.C4,
                data.DateEnd,
                data.VALID,
                data.ItemType,
                data.Flag
            ).FirstOrDefault();
        }

        // Insert with specific item_id (for fixed ID items like Club ID 13)
        public int InsertWithItemID(WarehouseData data)
        {
            // ? ระบุ item_id สำหรับกรณีพิเศษ (เช่น Starter Club ID=13)
            string sql = @"INSERT INTO pangya_warehouse 
                (item_id, UID, TYPEID, C0, C1, C2, C3, C4, RegDate, DateEnd, VALID, ItemType, Flag) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, NOW(), @p8, @p9, @p10, @p11);
                SELECT @p0;";

            return _db.Database.SqlQuery<int>(sql,
                data.item_id,
                data.UID,
                data.TYPEID,
                data.C0,
                data.C1,
                data.C2,
                data.C3,
                data.C4,
                data.DateEnd,
                data.VALID,
                data.ItemType,
                data.Flag
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(WarehouseData data)
        {
            string sql = @"UPDATE pangya_warehouse SET 
                UID = @p0, TYPEID = @p1, C0 = @p2, C1 = @p3, C2 = @p4, C3 = @p5, C4 = @p6, 
                DateEnd = @p7, VALID = @p8, ItemType = @p9, Flag = @p10 
                WHERE item_id = @p11 AND UID = @p12";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.TYPEID,
                data.C0,
                data.C1,
                data.C2,
                data.C3,
                data.C4,
                data.DateEnd,
                data.VALID,
                data.ItemType,
                data.Flag,
                data.item_id,
                data.UID
            );
        }

        public int UpdateSlots(int itemId, int uid, short c0, short c1, short c2, short c3, short c4)
        {
            string sql = @"UPDATE pangya_warehouse SET 
                C0 = @p0, C1 = @p1, C2 = @p2, C3 = @p3, C4 = @p4 
                WHERE item_id = @p5 AND UID = @p6";
            return _db.Database.ExecuteSqlCommand(sql, c0, c1, c2, c3, c4, itemId, uid);
        }

        public int UpdateDateEnd(int itemId, int uid, DateTime? dateEnd)
        {
            string sql = "UPDATE pangya_warehouse SET DateEnd = @p0 WHERE item_id = @p1 AND UID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, dateEnd, itemId, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int itemId, int uid)
        {
            string sql = "DELETE FROM pangya_warehouse WHERE item_id = @p0 AND UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, itemId, uid);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_warehouse WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int itemId, int uid)
        {
            string sql = "UPDATE pangya_warehouse SET VALID = 0 WHERE item_id = @p0 AND UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, itemId, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class WarehouseData
    {
        public int item_id { get; set; }
        public int UID { get; set; }
        public int TYPEID { get; set; }
        public short? C0 { get; set; }
        public short? C1 { get; set; }
        public short? C2 { get; set; }
        public short? C3 { get; set; }
        public short? C4 { get; set; }
        public DateTime? RegDate { get; set; }
        public DateTime? DateEnd { get; set; }
        public byte VALID { get; set; }
        public byte? ItemType { get; set; }
        public byte? Flag { get; set; }
    }
}
