using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_card : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_card(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_card() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public CardData SelectByCardIdx(int cardIdx)
        {
            string sql = "SELECT * FROM pangya_card WHERE CARD_IDX = @p0";
            return _db.Database.SqlQuery<CardData>(sql, cardIdx).FirstOrDefault();
        }

        public List<CardData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_card WHERE UID = @p0 AND VALID = 1";
            return _db.Database.SqlQuery<CardData>(sql, uid).ToList();
        }

        public CardData SelectByUIDAndTypeID(int uid, int cardTypeId)
        {
            string sql = "SELECT * FROM pangya_card WHERE UID = @p0 AND CARD_TYPEID = @p1 AND VALID = 1 LIMIT 1";
            return _db.Database.SqlQuery<CardData>(sql, uid, cardTypeId).FirstOrDefault();
        }

        public bool ExistsByUIDAndTypeID(int uid, int cardTypeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_card WHERE UID = @p0 AND CARD_TYPEID = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<int>(sql, uid, cardTypeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(CardData data)
        {
            string sql = @"INSERT INTO pangya_card 
                (UID, CARD_TYPEID, QTY, VALID) 
                VALUES (@p0, @p1, @p2, @p3);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.CARD_TYPEID,
                data.QTY,
                data.VALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(CardData data)
        {
            string sql = @"UPDATE pangya_card SET 
                UID = @p0, CARD_TYPEID = @p1, QTY = @p2, VALID = @p3 
                WHERE CARD_IDX = @p4";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.CARD_TYPEID,
                data.QTY,
                data.VALID,
                data.CARD_IDX
            );
        }

        public int UpdateQuantity(int cardIdx, int qty)
        {
            string sql = "UPDATE pangya_card SET QTY = @p0 WHERE CARD_IDX = @p1";
            return _db.Database.ExecuteSqlCommand(sql, qty, cardIdx);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int cardIdx)
        {
            string sql = "DELETE FROM pangya_card WHERE CARD_IDX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, cardIdx);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_card WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int cardIdx)
        {
            string sql = "UPDATE pangya_card SET VALID = 0 WHERE CARD_IDX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, cardIdx);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class CardData
    {
        public int CARD_IDX { get; set; }
        public int UID { get; set; }
        public int CARD_TYPEID { get; set; }
        public int QTY { get; set; }
        public DateTime? RegData { get; set; }
        public byte? VALID { get; set; }
    }
}
