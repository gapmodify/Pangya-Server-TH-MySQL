using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_member : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_member(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_member() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public MemberData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_member WHERE UID = @p0";
            return _db.Database.SqlQuery<MemberData>(sql, uid).FirstOrDefault();
        }

        public MemberData SelectByUsername(string username)
        {
            string sql = "SELECT * FROM pangya_member WHERE Username = @p0";
            return _db.Database.SqlQuery<MemberData>(sql, username).FirstOrDefault();
        }

        public MemberData SelectByNickname(string nickname)
        {
            string sql = "SELECT * FROM pangya_member WHERE Nickname = @p0";
            return _db.Database.SqlQuery<MemberData>(sql, nickname).FirstOrDefault();
        }

        public List<MemberData> SelectAll()
        {
            string sql = "SELECT * FROM pangya_member";
            return _db.Database.SqlQuery<MemberData>(sql).ToList();
        }

        public bool ExistsByUsername(string username)
        {
            string sql = "SELECT COUNT(*) FROM pangya_member WHERE Username = @p0";
            return _db.Database.SqlQuery<int>(sql, username).FirstOrDefault() > 0;
        }

        public bool ExistsByNickname(string nickname)
        {
            string sql = "SELECT COUNT(*) FROM pangya_member WHERE Nickname = @p0";
            return _db.Database.SqlQuery<int>(sql, nickname).FirstOrDefault() > 0;
        }

        public MemberData AuthenticateUser(string username, string password)
        {
            string sql = @"SELECT * FROM pangya_member WHERE Username = @p0 LIMIT 1";
            var member = _db.Database.SqlQuery<MemberData>(sql, username).FirstOrDefault();
            
            if (member == null)
                return null;
            
            // Check password match
            if (member.Password != password)
                return null;
                
            return member;
        }

        #endregion

        #region INSERT Methods

        public int Insert(MemberData data)
        {
            string sql = @"INSERT INTO pangya_member 
                (Username, Password, IDState, FirstSet, LastLogonTime, Logon, Nickname, Sex, IPAddress, 
                 LogonCount, Capabilities, AuthKey_Login, AuthKey_Game, GUILDINDEX, DailyLoginCount, 
                 Tutorial, BirthDay, Event1, Event2) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.Username,
                data.Password,
                data.IDState,
                data.FirstSet,
                data.LastLogonTime,
                data.Logon,
                data.Nickname,
                data.Sex,
                data.IPAddress,
                data.LogonCount,
                data.Capabilities,
                data.AuthKey_Login,
                data.AuthKey_Game,
                data.GUILDINDEX,
                data.DailyLoginCount,
                data.Tutorial,
                data.BirthDay,
                data.Event1,
                data.Event2
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(MemberData data)
        {
            string sql = @"UPDATE pangya_member SET 
                Username = @p0, Password = @p1, IDState = @p2, FirstSet = @p3, LastLogonTime = @p4, 
                Logon = @p5, Nickname = @p6, Sex = @p7, IPAddress = @p8, LogonCount = @p9, 
                Capabilities = @p10, AuthKey_Login = @p11, AuthKey_Game = @p12, GUILDINDEX = @p13, 
                DailyLoginCount = @p14, Tutorial = @p15, BirthDay = @p16, Event1 = @p17, Event2 = @p18 
                WHERE UID = @p19";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Username,
                data.Password,
                data.IDState,
                data.FirstSet,
                data.LastLogonTime,
                data.Logon,
                data.Nickname,
                data.Sex,
                data.IPAddress,
                data.LogonCount,
                data.Capabilities,
                data.AuthKey_Login,
                data.AuthKey_Game,
                data.GUILDINDEX,
                data.DailyLoginCount,
                data.Tutorial,
                data.BirthDay,
                data.Event1,
                data.Event2,
                data.UID
            );
        }

        public int UpdateLogonStatus(int uid, byte logon)
        {
            string sql = "UPDATE pangya_member SET Logon = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, logon, uid);
        }

        public int UpdateLastLogonTime(int uid, DateTime? time)
        {
            string sql = "UPDATE pangya_member SET LastLogonTime = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, time, uid);
        }

        public int UpdateAuthKeys(int uid, string authKeyLogin, string authKeyGame)
        {
            string sql = "UPDATE pangya_member SET AuthKey_Login = @p0, AuthKey_Game = @p1 WHERE UID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, authKeyLogin, authKeyGame, uid);
        }

        public int UpdateLoginInfo(int uid, string authKeyLogin, string authKeyGame, string ipAddress)
        {
            // ? แก้ไข: ไม่ update LastLogonTime ใน Login Server
            // LastLogonTime จะถูก update ใน Game Server (Daily Reward System) แทน
            string sql = @"UPDATE pangya_member 
                SET AuthKey_Login = @p0, AuthKey_Game = @p1, 
                    IPAddress = @p2, LogonCount = LogonCount + 1 
                WHERE UID = @p3";
            return _db.Database.ExecuteSqlCommand(sql, authKeyLogin, authKeyGame, ipAddress, uid);
        }

        public int UpdateDailyLoginCount(int uid, int count)
        {
            string sql = "UPDATE pangya_member SET DailyLoginCount = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, count, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_member WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int DeleteByUsername(string username)
        {
            string sql = "DELETE FROM pangya_member WHERE Username = @p0";
            return _db.Database.ExecuteSqlCommand(sql, username);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class MemberData
    {
        public int UID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public byte? IDState { get; set; }
        public byte? FirstSet { get; set; }
        public DateTime? LastLogonTime { get; set; }
        public byte? Logon { get; set; }
        public string Nickname { get; set; }
        public byte? Sex { get; set; }
        public string IPAddress { get; set; }
        public int? LogonCount { get; set; }
        public byte? Capabilities { get; set; }
        public DateTime? RegDate { get; set; }
        public string AuthKey_Login { get; set; }
        public string AuthKey_Game { get; set; }
        public int? GUILDINDEX { get; set; }
        public int? DailyLoginCount { get; set; }
        public byte? Tutorial { get; set; }
        public DateTime? BirthDay { get; set; }
        public byte? Event1 { get; set; }
        public byte? Event2 { get; set; }
    }
}
