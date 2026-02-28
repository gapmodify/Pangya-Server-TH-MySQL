using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_guild_info : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_guild_info(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_guild_info() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public GuildInfoData SelectByGuildIndex(int guildIndex)
        {
            string sql = "SELECT * FROM pangya_guild_info WHERE GUILD_INDEX = @p0 AND GUILD_VALID = 1";
            return _db.Database.SqlQuery<GuildInfoData>(sql, guildIndex).FirstOrDefault();
        }

        public GuildInfoData SelectByGuildName(string guildName)
        {
            string sql = "SELECT * FROM pangya_guild_info WHERE GUILD_NAME = @p0 AND GUILD_VALID = 1";
            return _db.Database.SqlQuery<GuildInfoData>(sql, guildName).FirstOrDefault();
        }

        public List<GuildInfoData> SelectAll()
        {
            string sql = "SELECT * FROM pangya_guild_info WHERE GUILD_VALID = 1";
            return _db.Database.SqlQuery<GuildInfoData>(sql).ToList();
        }

        public bool ExistsByName(string guildName)
        {
            string sql = "SELECT COUNT(*) FROM pangya_guild_info WHERE GUILD_NAME = @p0 AND GUILD_VALID = 1";
            return _db.Database.SqlQuery<int>(sql, guildName).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(GuildInfoData data)
        {
            string sql = @"INSERT INTO pangya_guild_info 
                (GUILD_NAME, GUILD_INTRODUCING, GUILD_NOTICE, GUILD_LEADER_UID, GUILD_POINT, 
                 GUILD_PANG, GUILD_IMAGE, GUILD_IMAGE_KEY_UPLOAD, GUILD_VALID) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.GUILD_NAME,
                data.GUILD_INTRODUCING,
                data.GUILD_NOTICE,
                data.GUILD_LEADER_UID,
                data.GUILD_POINT,
                data.GUILD_PANG,
                data.GUILD_IMAGE,
                data.GUILD_IMAGE_KEY_UPLOAD,
                data.GUILD_VALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(GuildInfoData data)
        {
            string sql = @"UPDATE pangya_guild_info SET 
                GUILD_NAME = @p0, GUILD_INTRODUCING = @p1, GUILD_NOTICE = @p2, GUILD_LEADER_UID = @p3, 
                GUILD_POINT = @p4, GUILD_PANG = @p5, GUILD_IMAGE = @p6, GUILD_IMAGE_KEY_UPLOAD = @p7, 
                GUILD_VALID = @p8 
                WHERE GUILD_INDEX = @p9";

            return _db.Database.ExecuteSqlCommand(sql,
                data.GUILD_NAME,
                data.GUILD_INTRODUCING,
                data.GUILD_NOTICE,
                data.GUILD_LEADER_UID,
                data.GUILD_POINT,
                data.GUILD_PANG,
                data.GUILD_IMAGE,
                data.GUILD_IMAGE_KEY_UPLOAD,
                data.GUILD_VALID,
                data.GUILD_INDEX
            );
        }

        public int UpdateNotice(int guildIndex, string notice)
        {
            string sql = "UPDATE pangya_guild_info SET GUILD_NOTICE = @p0 WHERE GUILD_INDEX = @p1";
            return _db.Database.ExecuteSqlCommand(sql, notice, guildIndex);
        }

        public int UpdateLeader(int guildIndex, int leaderUid)
        {
            string sql = "UPDATE pangya_guild_info SET GUILD_LEADER_UID = @p0 WHERE GUILD_INDEX = @p1";
            return _db.Database.ExecuteSqlCommand(sql, leaderUid, guildIndex);
        }

        public int UpdatePoint(int guildIndex, int point)
        {
            string sql = "UPDATE pangya_guild_info SET GUILD_POINT = @p0 WHERE GUILD_INDEX = @p1";
            return _db.Database.ExecuteSqlCommand(sql, point, guildIndex);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int guildIndex)
        {
            string sql = "DELETE FROM pangya_guild_info WHERE GUILD_INDEX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, guildIndex);
        }

        public int SoftDelete(int guildIndex)
        {
            string sql = "UPDATE pangya_guild_info SET GUILD_VALID = 0 WHERE GUILD_INDEX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, guildIndex);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class GuildInfoData
    {
        public int GUILD_INDEX { get; set; }
        public string GUILD_NAME { get; set; }
        public string GUILD_INTRODUCING { get; set; }
        public string GUILD_NOTICE { get; set; }
        public int GUILD_LEADER_UID { get; set; }
        public int? GUILD_POINT { get; set; }
        public int? GUILD_PANG { get; set; }
        public string GUILD_IMAGE { get; set; }
        public int? GUILD_IMAGE_KEY_UPLOAD { get; set; }
        public DateTime? GUILD_CREATE_DATE { get; set; }
        public byte? GUILD_VALID { get; set; }
    }
}
