using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_card_equip : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_card_equip(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_card_equip() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public CardEquipData SelectByID(int id)
        {
            string sql = "SELECT * FROM pangya_card_equip WHERE ID = @p0";
            return _db.Database.SqlQuery<CardEquipData>(sql, id).FirstOrDefault();
        }

        public List<CardEquipData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_card_equip WHERE UID = @p0 AND VALID = 1";
            return _db.Database.SqlQuery<CardEquipData>(sql, uid).ToList();
        }

        public List<CardEquipData> SelectByUIDAndCID(int uid, int cid)
        {
            string sql = "SELECT * FROM pangya_card_equip WHERE UID = @p0 AND CID = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<CardEquipData>(sql, uid, cid).ToList();
        }

        public CardEquipData SelectBySlot(int uid, int cid, int slot)
        {
            string sql = "SELECT * FROM pangya_card_equip WHERE UID = @p0 AND CID = @p1 AND SLOT = @p2 AND VALID = 1 LIMIT 1";
            return _db.Database.SqlQuery<CardEquipData>(sql, uid, cid, slot).FirstOrDefault();
        }

        #endregion

        #region INSERT Methods

        public int Insert(CardEquipData data)
        {
            string sql = @"INSERT INTO pangya_card_equip 
                (UID, CID, CHAR_TYPEID, CARD_TYPEID, SLOT, REGDATE, ENDDATE, FLAG, VALID) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.CID,
                data.CHAR_TYPEID,
                data.CARD_TYPEID,
                data.SLOT,
                data.REGDATE,
                data.ENDDATE,
                data.FLAG,
                data.VALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(CardEquipData data)
        {
            string sql = @"UPDATE pangya_card_equip SET 
                UID = @p0, CID = @p1, CHAR_TYPEID = @p2, CARD_TYPEID = @p3, SLOT = @p4, 
                REGDATE = @p5, ENDDATE = @p6, FLAG = @p7, VALID = @p8 
                WHERE ID = @p9";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.CID,
                data.CHAR_TYPEID,
                data.CARD_TYPEID,
                data.SLOT,
                data.REGDATE,
                data.ENDDATE,
                data.FLAG,
                data.VALID,
                data.ID
            );
        }

        public int UpdateEndDate(int id, DateTime? endDate)
        {
            string sql = "UPDATE pangya_card_equip SET ENDDATE = @p0 WHERE ID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, endDate, id);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM pangya_card_equip WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_card_equip WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int id)
        {
            string sql = "UPDATE pangya_card_equip SET VALID = 0 WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class CardEquipData
    {
        public int ID { get; set; }
        public int UID { get; set; }
        public int CID { get; set; }
        public int? CHAR_TYPEID { get; set; }
        public int? CARD_TYPEID { get; set; }
        public int? SLOT { get; set; }
        public DateTime? REGDATE { get; set; }
        public DateTime? ENDDATE { get; set; }
        public byte? FLAG { get; set; }
        public byte? VALID { get; set; }
    }
}
