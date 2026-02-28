using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_achievement_data : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_achievement_data(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_achievement_data() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public AchievementDataInfo SelectByID(int id)
        {
            string sql = "SELECT * FROM achievement_data WHERE ID = @p0";
            return _db.Database.SqlQuery<AchievementDataInfo>(sql, id).FirstOrDefault();
        }

        public List<AchievementDataInfo> SelectAll()
        {
            string sql = "SELECT * FROM achievement_data WHERE ACHIEVEMENT_ENABLE = 1";
            return _db.Database.SqlQuery<AchievementDataInfo>(sql).ToList();
        }

        public List<AchievementDataInfo> SelectByTypeID(int typeId)
        {
            string sql = "SELECT * FROM achievement_data WHERE ACHIEVEMENT_TYPEID = @p0 AND ACHIEVEMENT_ENABLE = 1";
            return _db.Database.SqlQuery<AchievementDataInfo>(sql, typeId).ToList();
        }

        public List<AchievementDataInfo> SelectByQuestTypeID(int questTypeId)
        {
            string sql = "SELECT * FROM achievement_data WHERE ACHIEVEMENT_QUEST_TYPEID = @p0 AND ACHIEVEMENT_ENABLE = 1";
            return _db.Database.SqlQuery<AchievementDataInfo>(sql, questTypeId).ToList();
        }

        #endregion

        #region INSERT Methods

        public int Insert(AchievementDataInfo data)
        {
            string sql = @"INSERT INTO achievement_data 
                (ACHIEVEMENT_ENABLE, ACHIEVEMENT_TYPEID, ACHIEVEMENT_NAME, ACHIEVEMENT_QUEST_TYPEID) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.ACHIEVEMENT_ENABLE,
                data.ACHIEVEMENT_TYPEID,
                data.ACHIEVEMENT_NAME,
                data.ACHIEVEMENT_QUEST_TYPEID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(AchievementDataInfo data)
        {
            string sql = @"UPDATE achievement_data SET 
                ACHIEVEMENT_ENABLE = @p0, ACHIEVEMENT_TYPEID = @p1, ACHIEVEMENT_NAME = @p2, 
                ACHIEVEMENT_QUEST_TYPEID = @p3 
                WHERE ID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.ACHIEVEMENT_ENABLE,
                data.ACHIEVEMENT_TYPEID,
                data.ACHIEVEMENT_NAME,
                data.ACHIEVEMENT_QUEST_TYPEID,
                data.ID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM achievement_data WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class AchievementDataInfo
    {
        public int ID { get; set; }
        public byte ACHIEVEMENT_ENABLE { get; set; }
        public int ACHIEVEMENT_TYPEID { get; set; }
        public string ACHIEVEMENT_NAME { get; set; }
        public int ACHIEVEMENT_QUEST_TYPEID { get; set; }
    }
}
