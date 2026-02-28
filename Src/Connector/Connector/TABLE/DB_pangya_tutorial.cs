using Connector.DataBase;
using System;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_tutorial : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_tutorial(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_tutorial() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public TutorialData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_tutorial WHERE UID = @p0";
            return _db.Database.SqlQuery<TutorialData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_tutorial WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(TutorialData data)
        {
            string sql = @"INSERT INTO pangya_tutorial 
                (UID, Rookie, Beginner, Advancer) 
                VALUES (@p0, @p1, @p2, @p3)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.Rookie,
                data.Beginner,
                data.Advancer
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(TutorialData data)
        {
            string sql = @"UPDATE pangya_tutorial SET 
                Rookie = @p0, Beginner = @p1, Advancer = @p2 
                WHERE UID = @p3";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Rookie,
                data.Beginner,
                data.Advancer,
                data.UID
            );
        }

        public int UpdateRookie(int uid, int rookie)
        {
            string sql = "UPDATE pangya_tutorial SET Rookie = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, rookie, uid);
        }

        public int UpdateBeginner(int uid, int beginner)
        {
            string sql = "UPDATE pangya_tutorial SET Beginner = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, beginner, uid);
        }

        public int UpdateAdvancer(int uid, int advancer)
        {
            string sql = "UPDATE pangya_tutorial SET Advancer = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, advancer, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_tutorial WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class TutorialData
    {
        public int? UID { get; set; }
        public int? Rookie { get; set; }
        public int? Beginner { get; set; }
        public int? Advancer { get; set; }
    }
}
