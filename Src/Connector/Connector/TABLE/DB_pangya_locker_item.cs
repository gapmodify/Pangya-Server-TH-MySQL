using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_locker_item : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_locker_item(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_locker_item() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public LockerItemData SelectByInvenID(int invenId, int uid)
        {
            string sql = "SELECT * FROM pangya_locker_item WHERE INVEN_ID = @p0 AND UID = @p1";
            return _db.Database.SqlQuery<LockerItemData>(sql, invenId, uid).FirstOrDefault();
        }

        public List<LockerItemData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_locker_item WHERE UID = @p0 AND Valid = 1";
            return _db.Database.SqlQuery<LockerItemData>(sql, uid).ToList();
        }

        public bool ExistsByTypeID(int uid, int typeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_locker_item WHERE UID = @p0 AND TypeID = @p1 AND Valid = 1";
            return _db.Database.SqlQuery<int>(sql, uid, typeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(LockerItemData data)
        {
            string sql = @"INSERT INTO pangya_locker_item 
                (UID, TypeID, Name, FROM_ID, Valid) 
                VALUES (@p0, @p1, @p2, @p3, @p4);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.TypeID,
                data.Name,
                data.FROM_ID,
                data.Valid
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(LockerItemData data)
        {
            string sql = @"UPDATE pangya_locker_item SET 
                UID = @p0, TypeID = @p1, Name = @p2, FROM_ID = @p3, Valid = @p4 
                WHERE INVEN_ID = @p5 AND UID = @p6";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.TypeID,
                data.Name,
                data.FROM_ID,
                data.Valid,
                data.INVEN_ID,
                data.UID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int invenId, int uid)
        {
            string sql = "DELETE FROM pangya_locker_item WHERE INVEN_ID = @p0 AND UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, invenId, uid);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_locker_item WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int invenId, int uid)
        {
            string sql = "UPDATE pangya_locker_item SET Valid = 0 WHERE INVEN_ID = @p0 AND UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, invenId, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class LockerItemData
    {
        public int INVEN_ID { get; set; }
        public int UID { get; set; }
        public int? TypeID { get; set; }
        public string Name { get; set; }
        public int? FROM_ID { get; set; }
        public sbyte? Valid { get; set; }
    }
}
