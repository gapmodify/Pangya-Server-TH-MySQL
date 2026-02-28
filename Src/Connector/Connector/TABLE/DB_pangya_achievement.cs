using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_achievement : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_achievement(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_achievement() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public AchievementData SelectByID(int id)
        {
            string sql = "SELECT * FROM pangya_achievement WHERE ID = @p0";
            return _db.Database.SqlQuery<AchievementData>(sql, id).FirstOrDefault();
        }

        public List<AchievementData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_achievement WHERE UID = @p0 AND Valid = 1";
            return _db.Database.SqlQuery<AchievementData>(sql, uid).ToList();
        }

        public List<AchievementData> SelectByUIDAndType(int uid, byte type)
        {
            string sql = "SELECT * FROM pangya_achievement WHERE UID = @p0 AND Type = @p1 AND Valid = 1";
            return _db.Database.SqlQuery<AchievementData>(sql, uid, type).ToList();
        }

        public bool ExistsByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_achievement WHERE UID = @p0 AND TypeID = @p1 AND Valid = 1";
            return _db.Database.SqlQuery<int>(sql, uid, typeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(AchievementData data)
        {
            string sql = @"INSERT INTO pangya_achievement 
                (UID, TypeID, Type, Valid) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.TypeID,
                data.Type,
                data.Valid
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(AchievementData data)
        {
            string sql = @"UPDATE pangya_achievement SET 
                UID = @p0, TypeID = @p1, Type = @p2, Valid = @p3 
                WHERE ID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.TypeID,
                data.Type,
                data.Valid,
                data.ID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM pangya_achievement WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_achievement WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int id)
        {
            string sql = "UPDATE pangya_achievement SET Valid = 0 WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class AchievementData
    {
        public int ID { get; set; }
        public int UID { get; set; }
        public int TypeID { get; set; }
        public byte Type { get; set; }
        public byte? Valid { get; set; }
    }
}
