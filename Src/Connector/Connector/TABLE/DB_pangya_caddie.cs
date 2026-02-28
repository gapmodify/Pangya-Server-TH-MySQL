using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_caddie : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_caddie(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_caddie() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public CaddieData SelectByCID(int cid)
        {
            string sql = "SELECT * FROM pangya_caddie WHERE CID = @p0";
            return _db.Database.SqlQuery<CaddieData>(sql, cid).FirstOrDefault();
        }

        public List<CaddieData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_caddie WHERE UID = @p0 AND VALID = 1";
            return _db.Database.SqlQuery<CaddieData>(sql, uid).ToList();
        }

        public CaddieData SelectByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT * FROM pangya_caddie WHERE UID = @p0 AND TYPEID = @p1 LIMIT 1";
            return _db.Database.SqlQuery<CaddieData>(sql, uid, typeId).FirstOrDefault();
        }

        public bool ExistsByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_caddie WHERE UID = @p0 AND TYPEID = @p1";
            return _db.Database.SqlQuery<int>(sql, uid, typeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(CaddieData data)
        {
            string sql = @"INSERT INTO pangya_caddie 
                (UID, TYPEID, EXP, cLevel, SKIN_TYPEID, RentFlag, END_DATE, SKIN_END_DATE, TriggerPay, VALID) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.TYPEID,
                data.EXP,
                data.cLevel,
                data.SKIN_TYPEID,
                data.RentFlag,
                data.END_DATE,
                data.SKIN_END_DATE,
                data.TriggerPay,
                data.VALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(CaddieData data)
        {
            string sql = @"UPDATE pangya_caddie SET 
                UID = @p0, TYPEID = @p1, EXP = @p2, cLevel = @p3, SKIN_TYPEID = @p4, 
                RentFlag = @p5, END_DATE = @p6, SKIN_END_DATE = @p7, TriggerPay = @p8, VALID = @p9 
                WHERE CID = @p10";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.TYPEID,
                data.EXP,
                data.cLevel,
                data.SKIN_TYPEID,
                data.RentFlag,
                data.END_DATE,
                data.SKIN_END_DATE,
                data.TriggerPay,
                data.VALID,
                data.CID
            );
        }

        public int UpdateEXP(int cid, int exp)
        {
            string sql = "UPDATE pangya_caddie SET EXP = @p0 WHERE CID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, exp, cid);
        }

        public int UpdateLevel(int cid, byte level)
        {
            string sql = "UPDATE pangya_caddie SET cLevel = @p0 WHERE CID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, level, cid);
        }

        public int UpdateSkin(int cid, int skinTypeId, DateTime? skinEndDate)
        {
            string sql = "UPDATE pangya_caddie SET SKIN_TYPEID = @p0, SKIN_END_DATE = @p1 WHERE CID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, skinTypeId, skinEndDate, cid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int cid)
        {
            string sql = "DELETE FROM pangya_caddie WHERE CID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, cid);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_caddie WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int cid)
        {
            string sql = "UPDATE pangya_caddie SET VALID = 0 WHERE CID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, cid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class CaddieData
    {
        public int CID { get; set; }
        public int UID { get; set; }
        public int TYPEID { get; set; }
        public int EXP { get; set; }
        public byte cLevel { get; set; }
        public int? SKIN_TYPEID { get; set; }
        public byte? RentFlag { get; set; }
        public DateTime? RegDate { get; set; }
        public DateTime? END_DATE { get; set; }
        public DateTime? SKIN_END_DATE { get; set; }
        public byte? TriggerPay { get; set; }
        public byte VALID { get; set; }
    }
}
