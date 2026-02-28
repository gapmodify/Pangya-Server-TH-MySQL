using Connector.DataBase;
using System;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_personal : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_personal(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_personal() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public PersonalData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_personal WHERE UID = @p0";
            return _db.Database.SqlQuery<PersonalData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_personal WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(PersonalData data)
        {
            string sql = @"INSERT INTO pangya_personal 
                (UID, CookieAmt, PangLockerAmt, LockerPwd, AssistMode) 
                VALUES (@p0, @p1, @p2, @p3, @p4)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.CookieAmt,
                data.PangLockerAmt,
                data.LockerPwd,
                data.AssistMode
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(PersonalData data)
        {
            string sql = @"UPDATE pangya_personal SET 
                CookieAmt = @p0, PangLockerAmt = @p1, LockerPwd = @p2, AssistMode = @p3 
                WHERE UID = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.CookieAmt,
                data.PangLockerAmt,
                data.LockerPwd,
                data.AssistMode,
                data.UID
            );
        }

        public int UpdateCookie(int uid, int cookieAmt)
        {
            string sql = "UPDATE pangya_personal SET CookieAmt = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, cookieAmt, uid);
        }

        public int UpdatePangLocker(int uid, int pangLockerAmt)
        {
            string sql = "UPDATE pangya_personal SET PangLockerAmt = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, pangLockerAmt, uid);
        }

        public int UpdateLockerPwd(int uid, string pwd)
        {
            string sql = "UPDATE pangya_personal SET LockerPwd = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, pwd, uid);
        }

        public int UpdateAssistMode(int uid, byte assistMode)
        {
            string sql = "UPDATE pangya_personal SET AssistMode = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, assistMode, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_personal WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class PersonalData
    {
        public int UID { get; set; }
        public int? CookieAmt { get; set; }
        public int? PangLockerAmt { get; set; }
        public string LockerPwd { get; set; }
        public int? AssistMode { get; set; }
    }
}
