using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_daily_quest : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_daily_quest(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_daily_quest() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public DailyQuestData SelectByID(int id)
        {
            string sql = "SELECT * FROM pangya_daily_quest WHERE ID = @p0";
            return _db.Database.SqlQuery<DailyQuestData>(sql, id).FirstOrDefault();
        }

        public DailyQuestData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_daily_quest WHERE UID = @p0 LIMIT 1";
            return _db.Database.SqlQuery<DailyQuestData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_daily_quest WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(DailyQuestData data)
        {
            string sql = @"INSERT INTO pangya_daily_quest 
                (UID, QuestID1, QuestID2, QuestID3, LastAccept, LastCancel) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.QuestID1,
                data.QuestID2,
                data.QuestID3,
                data.LastAccept,
                data.LastCancel
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(DailyQuestData data)
        {
            string sql = @"UPDATE pangya_daily_quest SET 
                UID = @p0, QuestID1 = @p1, QuestID2 = @p2, QuestID3 = @p3, 
                LastAccept = @p4, LastCancel = @p5 
                WHERE ID = @p6";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.QuestID1,
                data.QuestID2,
                data.QuestID3,
                data.LastAccept,
                data.LastCancel,
                data.ID
            );
        }

        public int UpdateQuests(int uid, int questId1, int questId2, int questId3)
        {
            string sql = @"UPDATE pangya_daily_quest SET 
                QuestID1 = @p0, QuestID2 = @p1, QuestID3 = @p2, LastAccept = NOW() 
                WHERE UID = @p3";
            return _db.Database.ExecuteSqlCommand(sql, questId1, questId2, questId3, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM pangya_daily_quest WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_daily_quest WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class DailyQuestData
    {
        public int ID { get; set; }
        public int UID { get; set; }
        public int? QuestID1 { get; set; }
        public int? QuestID2 { get; set; }
        public int? QuestID3 { get; set; }
        public DateTime? LastAccept { get; set; }
        public DateTime? LastCancel { get; set; }
        public DateTime? RegDate { get; set; }
    }
}
