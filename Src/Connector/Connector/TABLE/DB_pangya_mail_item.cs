using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_mail_item : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_mail_item(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_mail_item() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public MailItemData SelectByMailIndexAndTypeID(int mailIndex, int typeId)
        {
            string sql = "SELECT * FROM pangya_mail_item WHERE Mail_Index = @p0 AND TYPEID = @p1";
            return _db.Database.SqlQuery<MailItemData>(sql, mailIndex, typeId).FirstOrDefault();
        }

        public List<MailItemData> SelectByMailIndex(int mailIndex)
        {
            string sql = "SELECT * FROM pangya_mail_item WHERE Mail_Index = @p0";
            return _db.Database.SqlQuery<MailItemData>(sql, mailIndex).ToList();
        }

        public List<MailItemData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_mail_item WHERE TO_UID = @p0 ORDER BY IN_DATE DESC";
            return _db.Database.SqlQuery<MailItemData>(sql, uid).ToList();
        }

        #endregion

        #region INSERT Methods

        public int Insert(MailItemData data)
        {
            string sql = @"INSERT INTO pangya_mail_item 
                (Mail_Index, TYPEID, SETTYPEID, QTY, DAY, UCC_UNIQUE, ITEM_GRP, TO_UID, RELEASE_DATE, APPLY_ITEM_ID) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.Mail_Index,
                data.TYPEID,
                data.SETTYPEID,
                data.QTY,
                data.DAY,
                data.UCC_UNIQUE,
                data.ITEM_GRP,
                data.TO_UID,
                data.RELEASE_DATE,
                data.APPLY_ITEM_ID
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(MailItemData data)
        {
            string sql = @"UPDATE pangya_mail_item SET 
                TYPEID = @p0, SETTYPEID = @p1, QTY = @p2, DAY = @p3, UCC_UNIQUE = @p4, 
                ITEM_GRP = @p5, TO_UID = @p6, RELEASE_DATE = @p7, APPLY_ITEM_ID = @p8 
                WHERE Mail_Index = @p9 AND TYPEID = @p10";

            return _db.Database.ExecuteSqlCommand(sql,
                data.TYPEID,
                data.SETTYPEID,
                data.QTY,
                data.DAY,
                data.UCC_UNIQUE,
                data.ITEM_GRP,
                data.TO_UID,
                data.RELEASE_DATE,
                data.APPLY_ITEM_ID,
                data.Mail_Index,
                data.TYPEID
            );
        }

        #endregion

        #region DELETE Methods

        public int Delete(int mailIndex, int typeId)
        {
            string sql = "DELETE FROM pangya_mail_item WHERE Mail_Index = @p0 AND TYPEID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, mailIndex, typeId);
        }

        public int DeleteByMailIndex(int mailIndex)
        {
            string sql = "DELETE FROM pangya_mail_item WHERE Mail_Index = @p0";
            return _db.Database.ExecuteSqlCommand(sql, mailIndex);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_mail_item WHERE TO_UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class MailItemData
    {
        public int Mail_Index { get; set; }
        public int TYPEID { get; set; }
        public int? SETTYPEID { get; set; }
        public int? QTY { get; set; }
        public short? DAY { get; set; }
        public string UCC_UNIQUE { get; set; }
        public byte? ITEM_GRP { get; set; }
        public int? TO_UID { get; set; }
        public DateTime? IN_DATE { get; set; }
        public DateTime? RELEASE_DATE { get; set; }
        public int? APPLY_ITEM_ID { get; set; }
    }
}
