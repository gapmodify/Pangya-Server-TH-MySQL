using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_daily_quest : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_daily_quest(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_daily_quest() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public DailyQuestInfo SelectByID(int id)
        {
            string sql = "SELECT * FROM daily_quest WHERE ID = @p0";
            return _db.Database.SqlQuery<DailyQuestInfo>(sql, id).FirstOrDefault();
        }

        public DailyQuestInfo SelectByDay(byte day)
        {
            string sql = "SELECT * FROM daily_quest WHERE Day = @p0 LIMIT 1";
            return _db.Database.SqlQuery<DailyQuestInfo>(sql, day).FirstOrDefault();
        }

        public List<DailyQuestInfo> SelectAll()
        {
            string sql = "SELECT * FROM daily_quest ORDER BY Day";
            return _db.Database.SqlQuery<DailyQuestInfo>(sql).ToList();
        }

        public DailyQuestInfo SelectLatest()
        {
            string sql = "SELECT * FROM daily_quest ORDER BY RegDate DESC LIMIT 1";
            return _db.Database.SqlQuery<DailyQuestInfo>(sql).FirstOrDefault();
        }

        #endregion

        #region INSERT Methods

        public int Insert(DailyQuestInfo data)
        {
            string sql = @"INSERT INTO daily_quest 
                (QuestTypeID1, QuestTypeID2, QuestTypeID3, Day) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.QuestTypeID1,
                data.QuestTypeID2,
                data.QuestTypeID3,
                data.Day
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(DailyQuestInfo data)
        {
            string sql = @"UPDATE daily_quest SET 
                QuestTypeID1 = @p0, QuestTypeID2 = @p1, QuestTypeID3 = @p2, Day = @p3 
                WHERE ID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.QuestTypeID1,
                data.QuestTypeID2,
                data.QuestTypeID3,
                data.Day,
                data.ID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM daily_quest WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        public int DeleteOldQuests(int days)
        {
            string sql = "DELETE FROM daily_quest WHERE RegDate < DATE_SUB(NOW(), INTERVAL @p0 DAY)";
            return _db.Database.ExecuteSqlCommand(sql, days);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class DailyQuestInfo
    {
        public int ID { get; set; }
        public int QuestTypeID1 { get; set; }
        public int QuestTypeID2 { get; set; }
        public int QuestTypeID3 { get; set; }
        public DateTime? RegDate { get; set; }
        public byte? Day { get; set; }
    }
}
