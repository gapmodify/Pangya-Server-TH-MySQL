using Connector.DataBase;
using System;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_user_matchhistory : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_user_matchhistory(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_user_matchhistory() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public UserMatchHistoryData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_user_matchhistory WHERE UID = @p0";
            return _db.Database.SqlQuery<UserMatchHistoryData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_user_matchhistory WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(UserMatchHistoryData data)
        {
            string sql = @"INSERT INTO pangya_user_matchhistory 
                (UID, UID1, UID2, UID3, UID4, UID5) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.UID1,
                data.UID2,
                data.UID3,
                data.UID4,
                data.UID5
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(UserMatchHistoryData data)
        {
            string sql = @"UPDATE pangya_user_matchhistory SET 
                UID1 = @p0, UID2 = @p1, UID3 = @p2, UID4 = @p3, UID5 = @p4 
                WHERE UID = @p5";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID1,
                data.UID2,
                data.UID3,
                data.UID4,
                data.UID5,
                data.UID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_user_matchhistory WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class UserMatchHistoryData
    {
        public int UID { get; set; }
        public int UID1 { get; set; }
        public int UID2 { get; set; }
        public int UID3 { get; set; }
        public int UID4 { get; set; }
        public int UID5 { get; set; }
    }
}
