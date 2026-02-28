using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_guild_member : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_guild_member(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_guild_member() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public GuildMemberData SelectByGuildAndUID(int guildId, int memberUid)
        {
            string sql = "SELECT * FROM pangya_guild_member WHERE GUILD_ID = @p0 AND GUILD_MEMBER_UID = @p1";
            return _db.Database.SqlQuery<GuildMemberData>(sql, guildId, memberUid).FirstOrDefault();
        }

        public List<GuildMemberData> SelectByGuildID(int guildId)
        {
            string sql = "SELECT * FROM pangya_guild_member WHERE GUILD_ID = @p0";
            return _db.Database.SqlQuery<GuildMemberData>(sql, guildId).ToList();
        }

        public GuildMemberData SelectByUID(int memberUid)
        {
            string sql = "SELECT * FROM pangya_guild_member WHERE GUILD_MEMBER_UID = @p0 LIMIT 1";
            return _db.Database.SqlQuery<GuildMemberData>(sql, memberUid).FirstOrDefault();
        }

        public bool ExistsByUID(int memberUid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_guild_member WHERE GUILD_MEMBER_UID = @p0";
            return _db.Database.SqlQuery<int>(sql, memberUid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(GuildMemberData data)
        {
            string sql = @"INSERT INTO pangya_guild_member 
                (GUILD_ID, GUILD_MEMBER_UID, GUILD_POSITION, GUILD_MESSAGE, GUILD_MEMBER_STATUS) 
                VALUES (@p0, @p1, @p2, @p3, @p4)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.GUILD_ID,
                data.GUILD_MEMBER_UID,
                data.GUILD_POSITION,
                data.GUILD_MESSAGE,
                data.GUILD_MEMBER_STATUS
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(GuildMemberData data)
        {
            string sql = @"UPDATE pangya_guild_member SET 
                GUILD_POSITION = @p0, GUILD_MESSAGE = @p1, GUILD_MEMBER_STATUS = @p2 
                WHERE GUILD_ID = @p3 AND GUILD_MEMBER_UID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.GUILD_POSITION,
                data.GUILD_MESSAGE,
                data.GUILD_MEMBER_STATUS,
                data.GUILD_ID,
                data.GUILD_MEMBER_UID
            );
        }

        public int UpdatePosition(int guildId, int memberUid, byte position)
        {
            string sql = "UPDATE pangya_guild_member SET GUILD_POSITION = @p0 WHERE GUILD_ID = @p1 AND GUILD_MEMBER_UID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, position, guildId, memberUid);
        }

        public int UpdateMessage(int guildId, int memberUid, string message)
        {
            string sql = "UPDATE pangya_guild_member SET GUILD_MESSAGE = @p0 WHERE GUILD_ID = @p1 AND GUILD_MEMBER_UID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, message, guildId, memberUid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int guildId, int memberUid)
        {
            string sql = "DELETE FROM pangya_guild_member WHERE GUILD_ID = @p0 AND GUILD_MEMBER_UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, guildId, memberUid);
        }

        public int DeleteByGuildID(int guildId)
        {
            string sql = "DELETE FROM pangya_guild_member WHERE GUILD_ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, guildId);
        }

        public int DeleteByUID(int memberUid)
        {
            string sql = "DELETE FROM pangya_guild_member WHERE GUILD_MEMBER_UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, memberUid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class GuildMemberData
    {
        public int GUILD_ID { get; set; }
        public int GUILD_MEMBER_UID { get; set; }
        public byte? GUILD_POSITION { get; set; }
        public string GUILD_MESSAGE { get; set; }
        public DateTime? GUILD_ENTERED_TIME { get; set; }
        public byte? GUILD_MEMBER_STATUS { get; set; }
    }
}
