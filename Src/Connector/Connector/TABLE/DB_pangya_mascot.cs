using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_mascot : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_mascot(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_mascot() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public MascotData SelectByMID(int mid)
        {
            string sql = "SELECT * FROM pangya_mascot WHERE MID = @p0";
            return _db.Database.SqlQuery<MascotData>(sql, mid).FirstOrDefault();
        }

        public List<MascotData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_mascot WHERE UID = @p0 AND VALID = 1";
            return _db.Database.SqlQuery<MascotData>(sql, uid).ToList();
        }

        public MascotData SelectByUIDAndTypeID(int uid, int mascotTypeId)
        {
            string sql = "SELECT * FROM pangya_mascot WHERE UID = @p0 AND MASCOT_TYPEID = @p1 AND VALID = 1 LIMIT 1";
            return _db.Database.SqlQuery<MascotData>(sql, uid, mascotTypeId).FirstOrDefault();
        }

        public bool ExistsByUIDAndTypeID(int uid, int mascotTypeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_mascot WHERE UID = @p0 AND MASCOT_TYPEID = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<int>(sql, uid, mascotTypeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(MascotData data)
        {
            string sql = @"INSERT INTO pangya_mascot 
                (UID, MASCOT_TYPEID, MESSAGE, DateEnd, VALID) 
                VALUES (@p0, @p1, @p2, @p3, @p4);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.MASCOT_TYPEID,
                data.MESSAGE,
                data.DateEnd,
                data.VALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(MascotData data)
        {
            string sql = @"UPDATE pangya_mascot SET 
                UID = @p0, MASCOT_TYPEID = @p1, MESSAGE = @p2, DateEnd = @p3, VALID = @p4 
                WHERE MID = @p5";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.MASCOT_TYPEID,
                data.MESSAGE,
                data.DateEnd,
                data.VALID,
                data.MID
            );
        }

        public int UpdateMessage(int mid, string message)
        {
            string sql = "UPDATE pangya_mascot SET MESSAGE = @p0 WHERE MID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, message, mid);
        }

        public int UpdateDateEnd(int mid, DateTime? dateEnd)
        {
            string sql = "UPDATE pangya_mascot SET DateEnd = @p0 WHERE MID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, dateEnd, mid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int mid)
        {
            string sql = "DELETE FROM pangya_mascot WHERE MID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, mid);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_mascot WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int mid)
        {
            string sql = "UPDATE pangya_mascot SET VALID = 0 WHERE MID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, mid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class MascotData
    {
        public int MID { get; set; }
        public int UID { get; set; }
        public int MASCOT_TYPEID { get; set; }
        public string MESSAGE { get; set; }
        public DateTime? DateEnd { get; set; }
        public byte? VALID { get; set; }
    }
}
