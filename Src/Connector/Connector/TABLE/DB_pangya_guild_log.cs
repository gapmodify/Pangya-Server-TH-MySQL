using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_guild_log : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_guild_log(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_guild_log() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public List<GuildLogData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_guild_log WHERE UID = @p0 ORDER BY GUILD_ACTION_DATE DESC";
            return _db.Database.SqlQuery<GuildLogData>(sql, uid).ToList();
        }

        public List<GuildLogData> SelectByGuildID(int guildId)
        {
            string sql = "SELECT * FROM pangya_guild_log WHERE GUILD_ID = @p0 ORDER BY GUILD_ACTION_DATE DESC";
            return _db.Database.SqlQuery<GuildLogData>(sql, guildId).ToList();
        }

        public List<GuildLogData> SelectByAction(byte action)
        {
            string sql = "SELECT * FROM pangya_guild_log WHERE GUILD_ACTION = @p0 ORDER BY GUILD_ACTION_DATE DESC";
            return _db.Database.SqlQuery<GuildLogData>(sql, action).ToList();
        }

        #endregion

        #region INSERT Methods

        public int Insert(GuildLogData data)
        {
            string sql = @"INSERT INTO pangya_guild_log 
                (UID, GUILD_ID, GUILD_NAME, GUILD_ACTION) 
                VALUES (@p0, @p1, @p2, @p3)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.GUILD_ID,
                data.GUILD_NAME,
                data.GUILD_ACTION
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(GuildLogData data)
        {
            string sql = @"UPDATE pangya_guild_log SET 
                GUILD_ID = @p0, GUILD_NAME = @p1, GUILD_ACTION = @p2 
                WHERE UID = @p3 AND GUILD_ACTION_DATE = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.GUILD_ID,
                data.GUILD_NAME,
                data.GUILD_ACTION,
                data.UID,
                data.GUILD_ACTION_DATE
            );
        }

        #endregion

        #region DELETE Methods

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_guild_log WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int DeleteByGuildID(int guildId)
        {
            string sql = "DELETE FROM pangya_guild_log WHERE GUILD_ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, guildId);
        }

        public int DeleteOldLogs(int days)
        {
            string sql = "DELETE FROM pangya_guild_log WHERE GUILD_ACTION_DATE < DATE_SUB(NOW(), INTERVAL @p0 DAY)";
            return _db.Database.ExecuteSqlCommand(sql, days);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class GuildLogData
    {
        public int UID { get; set; }
        public int GUILD_ID { get; set; }
        public string GUILD_NAME { get; set; }
        public byte GUILD_ACTION { get; set; }
        public DateTime? GUILD_ACTION_DATE { get; set; }
    }
}
