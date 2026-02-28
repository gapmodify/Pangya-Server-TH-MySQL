using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_user_message : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_user_message(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_user_message() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public UserMessageData SelectByID(int idMsg)
        {
            string sql = "SELECT * FROM pangya_user_message WHERE ID_MSG = @p0";
            return _db.Database.SqlQuery<UserMessageData>(sql, idMsg).FirstOrDefault();
        }

        public List<UserMessageData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_user_message WHERE UID = @p0 AND Valid = 1 ORDER BY Reg_Date DESC";
            return _db.Database.SqlQuery<UserMessageData>(sql, uid).ToList();
        }

        public List<UserMessageData> SelectByFromUID(int uidFrom)
        {
            string sql = "SELECT * FROM pangya_user_message WHERE UID_FROM = @p0 AND Valid = 1 ORDER BY Reg_Date DESC";
            return _db.Database.SqlQuery<UserMessageData>(sql, uidFrom).ToList();
        }

        public int CountByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_user_message WHERE UID = @p0 AND Valid = 1";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault();
        }

        #endregion

        #region INSERT Methods

        public int Insert(UserMessageData data)
        {
            string sql = @"INSERT INTO pangya_user_message 
                (UID, UID_FROM, Valid, Message) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.UID_FROM,
                data.Valid,
                data.Message
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(UserMessageData data)
        {
            string sql = @"UPDATE pangya_user_message SET 
                UID = @p0, UID_FROM = @p1, Valid = @p2, Message = @p3 
                WHERE ID_MSG = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.UID_FROM,
                data.Valid,
                data.Message,
                data.ID_MSG
            );
        }

        public int MarkAsRead(int idMsg)
        {
            string sql = "UPDATE pangya_user_message SET Valid = 0 WHERE ID_MSG = @p0";
            return _db.Database.ExecuteSqlCommand(sql, idMsg);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int idMsg)
        {
            string sql = "DELETE FROM pangya_user_message WHERE ID_MSG = @p0";
            return _db.Database.ExecuteSqlCommand(sql, idMsg);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_user_message WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int DeleteOldMessages(int days)
        {
            string sql = "DELETE FROM pangya_user_message WHERE Reg_Date < DATE_SUB(NOW(), INTERVAL @p0 DAY)";
            return _db.Database.ExecuteSqlCommand(sql, days);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class UserMessageData
    {
        public int ID_MSG { get; set; }
        public int UID { get; set; }
        public int UID_FROM { get; set; }
        public byte Valid { get; set; }
        public string Message { get; set; }
        public DateTime Reg_Date { get; set; }
    }
}
