using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_friend : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_friend(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_friend() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public FriendData SelectByOwnerAndFriend(string owner, string friend)
        {
            string sql = "SELECT * FROM pangya_friend WHERE Owner = @p0 AND Friend = @p1";
            return _db.Database.SqlQuery<FriendData>(sql, owner, friend).FirstOrDefault();
        }

        public List<FriendData> SelectByOwner(string owner)
        {
            string sql = "SELECT * FROM pangya_friend WHERE Owner = @p0 AND IsDeleted = 0";
            return _db.Database.SqlQuery<FriendData>(sql, owner).ToList();
        }

        public List<FriendData> SelectAcceptedFriends(string owner)
        {
            string sql = "SELECT * FROM pangya_friend WHERE Owner = @p0 AND IsAccept = 1 AND IsDeleted = 0";
            return _db.Database.SqlQuery<FriendData>(sql, owner).ToList();
        }

        public List<FriendData> SelectPendingFriends(string owner)
        {
            string sql = "SELECT * FROM pangya_friend WHERE Friend = @p0 AND IsAccept = 0 AND IsAgree = 0 AND IsDeleted = 0";
            return _db.Database.SqlQuery<FriendData>(sql, owner).ToList();
        }

        public List<FriendData> SelectBlockedUsers(string owner)
        {
            string sql = "SELECT * FROM pangya_friend WHERE Owner = @p0 AND IsBlock = 1 AND IsDeleted = 0";
            return _db.Database.SqlQuery<FriendData>(sql, owner).ToList();
        }

        public bool IsFriend(string owner, string friend)
        {
            string sql = "SELECT COUNT(*) FROM pangya_friend WHERE Owner = @p0 AND Friend = @p1 AND IsAccept = 1 AND IsDeleted = 0";
            return _db.Database.SqlQuery<int>(sql, owner, friend).FirstOrDefault() > 0;
        }

        public bool IsBlocked(string owner, string friend)
        {
            string sql = "SELECT COUNT(*) FROM pangya_friend WHERE Owner = @p0 AND Friend = @p1 AND IsBlock = 1 AND IsDeleted = 0";
            return _db.Database.SqlQuery<int>(sql, owner, friend).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(FriendData data)
        {
            string sql = @"INSERT INTO pangya_friend 
                (Owner, Friend, IsAccept, GroupName, IsAgree, IsDeleted, Memo, IsBlock) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Owner,
                data.Friend,
                data.IsAccept,
                data.GroupName,
                data.IsAgree,
                data.IsDeleted,
                data.Memo,
                data.IsBlock
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(FriendData data)
        {
            string sql = @"UPDATE pangya_friend SET 
                IsAccept = @p0, GroupName = @p1, IsAgree = @p2, IsDeleted = @p3, Memo = @p4, IsBlock = @p5 
                WHERE Owner = @p6 AND Friend = @p7";

            return _db.Database.ExecuteSqlCommand(sql,
                data.IsAccept,
                data.GroupName,
                data.IsAgree,
                data.IsDeleted,
                data.Memo,
                data.IsBlock,
                data.Owner,
                data.Friend
            );
        }

        public int AcceptFriend(string owner, string friend)
        {
            string sql = "UPDATE pangya_friend SET IsAccept = 1, IsAgree = 1 WHERE Owner = @p0 AND Friend = @p1";
            return _db.Database.ExecuteSqlCommand(sql, owner, friend);
        }

        public int BlockUser(string owner, string friend)
        {
            string sql = "UPDATE pangya_friend SET IsBlock = 1 WHERE Owner = @p0 AND Friend = @p1";
            return _db.Database.ExecuteSqlCommand(sql, owner, friend);
        }

        public int UnblockUser(string owner, string friend)
        {
            string sql = "UPDATE pangya_friend SET IsBlock = 0 WHERE Owner = @p0 AND Friend = @p1";
            return _db.Database.ExecuteSqlCommand(sql, owner, friend);
        }

        public int UpdateMemo(string owner, string friend, string memo)
        {
            string sql = "UPDATE pangya_friend SET Memo = @p0 WHERE Owner = @p1 AND Friend = @p2";
            return _db.Database.ExecuteSqlCommand(sql, memo, owner, friend);
        }

        #endregion

        #region DELETE Methods

        public int Delete(string owner, string friend)
        {
            string sql = "DELETE FROM pangya_friend WHERE Owner = @p0 AND Friend = @p1";
            return _db.Database.ExecuteSqlCommand(sql, owner, friend);
        }

        public int DeleteByOwner(string owner)
        {
            string sql = "DELETE FROM pangya_friend WHERE Owner = @p0";
            return _db.Database.ExecuteSqlCommand(sql, owner);
        }

        public int SoftDelete(string owner, string friend)
        {
            string sql = "UPDATE pangya_friend SET IsDeleted = 1 WHERE Owner = @p0 AND Friend = @p1";
            return _db.Database.ExecuteSqlCommand(sql, owner, friend);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class FriendData
    {
        public string Owner { get; set; }
        public string Friend { get; set; }
        public byte IsAccept { get; set; }
        public string GroupName { get; set; }
        public byte IsAgree { get; set; }
        public byte IsDeleted { get; set; }
        public string Memo { get; set; }
        public byte IsBlock { get; set; }
    }
}
