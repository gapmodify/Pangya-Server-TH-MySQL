using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_mail : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_mail(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_mail() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public MailData SelectByMailIndex(int mailIndex)
        {
            string sql = "SELECT * FROM pangya_mail WHERE Mail_Index = @p0";
            return _db.Database.SqlQuery<MailData>(sql, mailIndex).FirstOrDefault();
        }

        public List<MailData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_mail WHERE UID = @p0 ORDER BY RegDate DESC";
            return _db.Database.SqlQuery<MailData>(sql, uid).ToList();
        }

        public List<MailData> SelectUnreadByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_mail WHERE UID = @p0 AND ReadDate IS NULL ORDER BY RegDate DESC";
            return _db.Database.SqlQuery<MailData>(sql, uid).ToList();
        }

        public int CountUnreadByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_mail WHERE UID = @p0 AND ReadDate IS NULL";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault();
        }

        #endregion

        #region INSERT Methods

        public int Insert(MailData data)
        {
            string sql = @"INSERT INTO pangya_mail 
                (UID, Sender, Sender_UID, Receiver, Receiver_UID, Subject, Msg, 
                 ReadDate, ReceiveDate, DeleteDate) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.Sender,
                data.Sender_UID,
                data.Receiver,
                data.Receiver_UID,
                data.Subject,
                data.Msg,
                data.ReadDate,
                data.ReceiveDate,
                data.DeleteDate
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(MailData data)
        {
            string sql = @"UPDATE pangya_mail SET 
                UID = @p0, Sender = @p1, Sender_UID = @p2, Receiver = @p3, Receiver_UID = @p4, 
                Subject = @p5, Msg = @p6, ReadDate = @p7, ReceiveDate = @p8, DeleteDate = @p9 
                WHERE Mail_Index = @p10";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.Sender,
                data.Sender_UID,
                data.Receiver,
                data.Receiver_UID,
                data.Subject,
                data.Msg,
                data.ReadDate,
                data.ReceiveDate,
                data.DeleteDate,
                data.Mail_Index
            );
        }

        public int MarkAsRead(int mailIndex)
        {
            string sql = "UPDATE pangya_mail SET ReadDate = NOW() WHERE Mail_Index = @p0";
            return _db.Database.ExecuteSqlCommand(sql, mailIndex);
        }

        public int MarkAsDeleted(int mailIndex)
        {
            string sql = "UPDATE pangya_mail SET DeleteDate = NOW() WHERE Mail_Index = @p0";
            return _db.Database.ExecuteSqlCommand(sql, mailIndex);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int mailIndex)
        {
            string sql = "DELETE FROM pangya_mail WHERE Mail_Index = @p0";
            return _db.Database.ExecuteSqlCommand(sql, mailIndex);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_mail WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int DeleteOldMails(int days)
        {
            string sql = "DELETE FROM pangya_mail WHERE RegDate < DATE_SUB(NOW(), INTERVAL @p0 DAY)";
            return _db.Database.ExecuteSqlCommand(sql, days);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class MailData
    {
        public int Mail_Index { get; set; }
        public int UID { get; set; }
        public string Sender { get; set; }
        public int? Sender_UID { get; set; }
        public string Receiver { get; set; }
        public int? Receiver_UID { get; set; }
        public string Subject { get; set; }
        public string Msg { get; set; }
        public DateTime? ReadDate { get; set; }
        public DateTime? ReceiveDate { get; set; }
        public DateTime? DeleteDate { get; set; }
        public DateTime? RegDate { get; set; }
    }
}
