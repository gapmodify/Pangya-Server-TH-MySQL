using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_club_info : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_club_info(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_club_info() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public ClubInfoData SelectByItemID(int itemId, int uid)
        {
            string sql = "SELECT * FROM pangya_club_info WHERE ITEM_ID = @p0 AND UID = @p1";
            return _db.Database.SqlQuery<ClubInfoData>(sql, itemId, uid).FirstOrDefault();
        }

        public List<ClubInfoData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_club_info WHERE UID = @p0";
            return _db.Database.SqlQuery<ClubInfoData>(sql, uid).ToList();
        }

        public bool ExistsByItemID(int itemId, int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_club_info WHERE ITEM_ID = @p0 AND UID = @p1";
            return _db.Database.SqlQuery<int>(sql, itemId, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(ClubInfoData data)
        {
            string sql = @"INSERT INTO pangya_club_info 
                (ITEM_ID, UID, TYPEID, C0_SLOT, C1_SLOT, C2_SLOT, C3_SLOT, C4_SLOT, 
                 CLUB_POINT, CLUB_WORK_COUNT, CLUB_SLOT_CANCEL, CLUB_POINT_TOTAL_LOG, CLUB_UPGRADE_PANG_LOG) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.ITEM_ID,
                data.UID,
                data.TYPEID,
                data.C0_SLOT,
                data.C1_SLOT,
                data.C2_SLOT,
                data.C3_SLOT,
                data.C4_SLOT,
                data.CLUB_POINT,
                data.CLUB_WORK_COUNT,
                data.CLUB_SLOT_CANCEL,
                data.CLUB_POINT_TOTAL_LOG,
                data.CLUB_UPGRADE_PANG_LOG
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(ClubInfoData data)
        {
            string sql = @"UPDATE pangya_club_info SET 
                C0_SLOT = @p0, C1_SLOT = @p1, C2_SLOT = @p2, C3_SLOT = @p3, C4_SLOT = @p4, 
                CLUB_POINT = @p5, CLUB_WORK_COUNT = @p6, CLUB_SLOT_CANCEL = @p7, 
                CLUB_POINT_TOTAL_LOG = @p8, CLUB_UPGRADE_PANG_LOG = @p9 
                WHERE ITEM_ID = @p10 AND UID = @p11";

            return _db.Database.ExecuteSqlCommand(sql,
                data.C0_SLOT,
                data.C1_SLOT,
                data.C2_SLOT,
                data.C3_SLOT,
                data.C4_SLOT,
                data.CLUB_POINT,
                data.CLUB_WORK_COUNT,
                data.CLUB_SLOT_CANCEL,
                data.CLUB_POINT_TOTAL_LOG,
                data.CLUB_UPGRADE_PANG_LOG,
                data.ITEM_ID,
                data.UID
            );
        }

        public int UpdateSlots(int itemId, int uid, short c0, short c1, short c2, short c3, short c4)
        {
            string sql = @"UPDATE pangya_club_info SET 
                C0_SLOT = @p0, C1_SLOT = @p1, C2_SLOT = @p2, C3_SLOT = @p3, C4_SLOT = @p4 
                WHERE ITEM_ID = @p5 AND UID = @p6";
            return _db.Database.ExecuteSqlCommand(sql, c0, c1, c2, c3, c4, itemId, uid);
        }

        public int UpdateClubPoint(int itemId, int uid, int clubPoint)
        {
            string sql = "UPDATE pangya_club_info SET CLUB_POINT = @p0 WHERE ITEM_ID = @p1 AND UID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, clubPoint, itemId, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int itemId, int uid)
        {
            string sql = "DELETE FROM pangya_club_info WHERE ITEM_ID = @p0 AND UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, itemId, uid);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_club_info WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class ClubInfoData
    {
        public int ITEM_ID { get; set; }
        public int UID { get; set; }
        public int TYPEID { get; set; }
        public short? C0_SLOT { get; set; }
        public short? C1_SLOT { get; set; }
        public short? C2_SLOT { get; set; }
        public short? C3_SLOT { get; set; }
        public short? C4_SLOT { get; set; }
        public int? CLUB_POINT { get; set; }
        public int? CLUB_WORK_COUNT { get; set; }
        public int? CLUB_SLOT_CANCEL { get; set; }
        public int? CLUB_POINT_TOTAL_LOG { get; set; }
        public int? CLUB_UPGRADE_PANG_LOG { get; set; }
    }
}
