using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_selfdesign : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_selfdesign(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_selfdesign() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public SelfDesignData SelectByKey(int uid, int itemId, string uccUnique)
        {
            string sql = "SELECT * FROM pangya_selfdesign WHERE UID = @p0 AND ITEM_ID = @p1 AND UCC_UNIQE = @p2";
            return _db.Database.SqlQuery<SelfDesignData>(sql, uid, itemId, uccUnique).FirstOrDefault();
        }

        public List<SelfDesignData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_selfdesign WHERE UID = @p0";
            return _db.Database.SqlQuery<SelfDesignData>(sql, uid).ToList();
        }

        public List<SelfDesignData> SelectByItemID(int itemId)
        {
            string sql = "SELECT * FROM pangya_selfdesign WHERE ITEM_ID = @p0";
            return _db.Database.SqlQuery<SelfDesignData>(sql, itemId).ToList();
        }

        public SelfDesignData SelectByUCCUnique(string uccUnique)
        {
            string sql = "SELECT * FROM pangya_selfdesign WHERE UCC_UNIQE = @p0 LIMIT 1";
            return _db.Database.SqlQuery<SelfDesignData>(sql, uccUnique).FirstOrDefault();
        }

        #endregion

        #region INSERT Methods

        public int Insert(SelfDesignData data)
        {
            string sql = @"INSERT INTO pangya_selfdesign 
                (UID, ITEM_ID, UCC_UNIQE, TYPEID, UCC_STATUS, UCC_KEY, UCC_NAME, UCC_DRAWER, UCC_COCOUNT) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.ITEM_ID,
                data.UCC_UNIQE,
                data.TYPEID,
                data.UCC_STATUS,
                data.UCC_KEY,
                data.UCC_NAME,
                data.UCC_DRAWER,
                data.UCC_COCOUNT
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(SelfDesignData data)
        {
            string sql = @"UPDATE pangya_selfdesign SET 
                TYPEID = @p0, UCC_STATUS = @p1, UCC_KEY = @p2, UCC_NAME = @p3, 
                UCC_DRAWER = @p4, UCC_COCOUNT = @p5 
                WHERE UID = @p6 AND ITEM_ID = @p7 AND UCC_UNIQE = @p8";

            return _db.Database.ExecuteSqlCommand(sql,
                data.TYPEID,
                data.UCC_STATUS,
                data.UCC_KEY,
                data.UCC_NAME,
                data.UCC_DRAWER,
                data.UCC_COCOUNT,
                data.UID,
                data.ITEM_ID,
                data.UCC_UNIQE
            );
        }

        public int UpdateStatus(int uid, int itemId, string uccUnique, byte status)
        {
            string sql = "UPDATE pangya_selfdesign SET UCC_STATUS = @p0 WHERE UID = @p1 AND ITEM_ID = @p2 AND UCC_UNIQE = @p3";
            return _db.Database.ExecuteSqlCommand(sql, status, uid, itemId, uccUnique);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid, int itemId, string uccUnique)
        {
            string sql = "DELETE FROM pangya_selfdesign WHERE UID = @p0 AND ITEM_ID = @p1 AND UCC_UNIQE = @p2";
            return _db.Database.ExecuteSqlCommand(sql, uid, itemId, uccUnique);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_selfdesign WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class SelfDesignData
    {
        public int UID { get; set; }
        public int ITEM_ID { get; set; }
        public string UCC_UNIQE { get; set; }
        public int TYPEID { get; set; }
        public byte? UCC_STATUS { get; set; }
        public string UCC_KEY { get; set; }
        public string UCC_NAME { get; set; }
        public int? UCC_DRAWER { get; set; }
        public int? UCC_COCOUNT { get; set; }
        public DateTime? IN_DATE { get; set; }
    }
}
