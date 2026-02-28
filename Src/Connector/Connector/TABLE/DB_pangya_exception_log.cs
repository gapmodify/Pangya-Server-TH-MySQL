using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_exception_log : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_exception_log(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_exception_log() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public ExceptionLogData SelectByExceptionID(int exceptionId)
        {
            string sql = "SELECT * FROM pangya_exception_log WHERE ExceptionID = @p0";
            return _db.Database.SqlQuery<ExceptionLogData>(sql, exceptionId).FirstOrDefault();
        }

        public List<ExceptionLogData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_exception_log WHERE UID = @p0 ORDER BY CreateDate DESC";
            return _db.Database.SqlQuery<ExceptionLogData>(sql, uid).ToList();
        }

        public List<ExceptionLogData> SelectRecent(int count)
        {
            string sql = $"SELECT * FROM pangya_exception_log ORDER BY CreateDate DESC LIMIT {count}";
            return _db.Database.SqlQuery<ExceptionLogData>(sql).ToList();
        }

        public List<ExceptionLogData> SelectByServer(string server)
        {
            string sql = "SELECT * FROM pangya_exception_log WHERE Server = @p0 ORDER BY CreateDate DESC";
            return _db.Database.SqlQuery<ExceptionLogData>(sql, server).ToList();
        }

        #endregion

        #region INSERT Methods

        public int Insert(ExceptionLogData data)
        {
            string sql = @"INSERT INTO pangya_exception_log 
                (UID, Username, ExceptionMessage, Server) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.Username,
                data.ExceptionMessage,
                data.Server
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(ExceptionLogData data)
        {
            string sql = @"UPDATE pangya_exception_log SET 
                UID = @p0, Username = @p1, ExceptionMessage = @p2, Server = @p3 
                WHERE ExceptionID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.Username,
                data.ExceptionMessage,
                data.Server,
                data.ExceptionID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int exceptionId)
        {
            string sql = "DELETE FROM pangya_exception_log WHERE ExceptionID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, exceptionId);
        }

        public int DeleteOldLogs(int days)
        {
            string sql = "DELETE FROM pangya_exception_log WHERE CreateDate < DATE_SUB(NOW(), INTERVAL @p0 DAY)";
            return _db.Database.ExecuteSqlCommand(sql, days);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class ExceptionLogData
    {
        public int ExceptionID { get; set; }
        public int? UID { get; set; }
        public string Username { get; set; }
        public string ExceptionMessage { get; set; }
        public string Server { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
