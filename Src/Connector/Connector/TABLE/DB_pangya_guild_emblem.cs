using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_guild_emblem : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_guild_emblem(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_guild_emblem() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public GuildEmblemData SelectByEmblemIdx(int emblemIdx)
        {
            string sql = "SELECT * FROM pangya_guild_emblem WHERE EMBLEM_IDX = @p0";
            return _db.Database.SqlQuery<GuildEmblemData>(sql, emblemIdx).FirstOrDefault();
        }

        public GuildEmblemData SelectByGuildID(int guildId)
        {
            string sql = "SELECT * FROM pangya_guild_emblem WHERE GUILD_ID = @p0 AND GUILD_MARK_ISVALID = 1 LIMIT 1";
            return _db.Database.SqlQuery<GuildEmblemData>(sql, guildId).FirstOrDefault();
        }

        public List<GuildEmblemData> SelectAll()
        {
            string sql = "SELECT * FROM pangya_guild_emblem WHERE GUILD_MARK_ISVALID = 1";
            return _db.Database.SqlQuery<GuildEmblemData>(sql).ToList();
        }

        public bool ExistsByGuildID(int guildId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_guild_emblem WHERE GUILD_ID = @p0 AND GUILD_MARK_ISVALID = 1";
            return _db.Database.SqlQuery<int>(sql, guildId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(GuildEmblemData data)
        {
            string sql = @"INSERT INTO pangya_guild_emblem 
                (GUILD_ID, GUILD_MARK_IMG, GUILD_MARK_ISVALID) 
                VALUES (@p0, @p1, @p2);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.GUILD_ID,
                data.GUILD_MARK_IMG,
                data.GUILD_MARK_ISVALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(GuildEmblemData data)
        {
            string sql = @"UPDATE pangya_guild_emblem SET 
                GUILD_ID = @p0, GUILD_MARK_IMG = @p1, GUILD_MARK_ISVALID = @p2 
                WHERE EMBLEM_IDX = @p3";

            return _db.Database.ExecuteSqlCommand(sql,
                data.GUILD_ID,
                data.GUILD_MARK_IMG,
                data.GUILD_MARK_ISVALID,
                data.EMBLEM_IDX
            );
        }

        public int UpdateImage(int emblemIdx, string markImg)
        {
            string sql = "UPDATE pangya_guild_emblem SET GUILD_MARK_IMG = @p0 WHERE EMBLEM_IDX = @p1";
            return _db.Database.ExecuteSqlCommand(sql, markImg, emblemIdx);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int emblemIdx)
        {
            string sql = "DELETE FROM pangya_guild_emblem WHERE EMBLEM_IDX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, emblemIdx);
        }

        public int DeleteByGuildID(int guildId)
        {
            string sql = "DELETE FROM pangya_guild_emblem WHERE GUILD_ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, guildId);
        }

        public int SoftDelete(int emblemIdx)
        {
            string sql = "UPDATE pangya_guild_emblem SET GUILD_MARK_ISVALID = 0 WHERE EMBLEM_IDX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, emblemIdx);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class GuildEmblemData
    {
        public int EMBLEM_IDX { get; set; }
        public int GUILD_ID { get; set; }
        public string GUILD_MARK_IMG { get; set; }
        public byte? GUILD_MARK_ISVALID { get; set; }
    }
}
