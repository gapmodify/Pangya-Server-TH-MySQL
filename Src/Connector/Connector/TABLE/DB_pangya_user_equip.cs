using Connector.DataBase;
using System;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_user_equip : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_user_equip(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_user_equip() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public UserEquipData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_user_equip WHERE UID = @p0";
            return _db.Database.SqlQuery<UserEquipData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_user_equip WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(UserEquipData data)
        {
            string sql = @"INSERT INTO pangya_user_equip 
                (UID, CADDIE, CHARACTER_ID, CLUB_ID, BALL_ID, 
                 ITEM_SLOT_1, ITEM_SLOT_2, ITEM_SLOT_3, ITEM_SLOT_4, ITEM_SLOT_5, 
                 ITEM_SLOT_6, ITEM_SLOT_7, ITEM_SLOT_8, ITEM_SLOT_9, ITEM_SLOT_10, 
                 Skin_1, Skin_2, Skin_3, Skin_4, Skin_5, Skin_6, 
                 MASCOT_ID, POSTER_1, POSTER_2) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, 
                        @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19, @p20, @p21, @p22, @p23)";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID, data.CADDIE, data.CHARACTER_ID, data.CLUB_ID, data.BALL_ID,
                data.ITEM_SLOT_1, data.ITEM_SLOT_2, data.ITEM_SLOT_3, data.ITEM_SLOT_4, data.ITEM_SLOT_5,
                data.ITEM_SLOT_6, data.ITEM_SLOT_7, data.ITEM_SLOT_8, data.ITEM_SLOT_9, data.ITEM_SLOT_10,
                data.Skin_1, data.Skin_2, data.Skin_3, data.Skin_4, data.Skin_5, data.Skin_6,
                data.MASCOT_ID, data.POSTER_1, data.POSTER_2
            );
        }

        #endregion

        #region UPDATE Methods

        public int Update(UserEquipData data)
        {
            string sql = @"UPDATE pangya_user_equip SET 
                CADDIE = @p0, CHARACTER_ID = @p1, CLUB_ID = @p2, BALL_ID = @p3, 
                ITEM_SLOT_1 = @p4, ITEM_SLOT_2 = @p5, ITEM_SLOT_3 = @p6, ITEM_SLOT_4 = @p7, ITEM_SLOT_5 = @p8, 
                ITEM_SLOT_6 = @p9, ITEM_SLOT_7 = @p10, ITEM_SLOT_8 = @p11, ITEM_SLOT_9 = @p12, ITEM_SLOT_10 = @p13, 
                Skin_1 = @p14, Skin_2 = @p15, Skin_3 = @p16, Skin_4 = @p17, Skin_5 = @p18, Skin_6 = @p19, 
                MASCOT_ID = @p20, POSTER_1 = @p21, POSTER_2 = @p22 
                WHERE UID = @p23";

            return _db.Database.ExecuteSqlCommand(sql,
                data.CADDIE, data.CHARACTER_ID, data.CLUB_ID, data.BALL_ID,
                data.ITEM_SLOT_1, data.ITEM_SLOT_2, data.ITEM_SLOT_3, data.ITEM_SLOT_4, data.ITEM_SLOT_5,
                data.ITEM_SLOT_6, data.ITEM_SLOT_7, data.ITEM_SLOT_8, data.ITEM_SLOT_9, data.ITEM_SLOT_10,
                data.Skin_1, data.Skin_2, data.Skin_3, data.Skin_4, data.Skin_5, data.Skin_6,
                data.MASCOT_ID, data.POSTER_1, data.POSTER_2,
                data.UID
            );
        }

        public int UpdateCharacterID(int uid, int characterId)
        {
            string sql = "UPDATE pangya_user_equip SET CHARACTER_ID = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, characterId, uid);
        }

        public int UpdateCaddie(int uid, int caddieId)
        {
            string sql = "UPDATE pangya_user_equip SET CADDIE = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, caddieId, uid);
        }

        public int UpdateClub(int uid, int clubId)
        {
            string sql = "UPDATE pangya_user_equip SET CLUB_ID = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, clubId, uid);
        }

        public int UpdateBall(int uid, int ballId)
        {
            string sql = "UPDATE pangya_user_equip SET BALL_ID = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, ballId, uid);
        }

        public int UpdateItemSlot(int uid, int slotNumber, int itemId)
        {
            string sql = $"UPDATE pangya_user_equip SET ITEM_SLOT_{slotNumber} = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, itemId, uid);
        }

        public int UpdateMascot(int uid, int mascotId)
        {
            string sql = "UPDATE pangya_user_equip SET MASCOT_ID = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, mascotId, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_user_equip WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class UserEquipData
    {
        public int UID { get; set; }
        public int CADDIE { get; set; }
        public int CHARACTER_ID { get; set; }
        public int CLUB_ID { get; set; }
        public int BALL_ID { get; set; }
        public int ITEM_SLOT_1 { get; set; }
        public int ITEM_SLOT_2 { get; set; }
        public int ITEM_SLOT_3 { get; set; }
        public int ITEM_SLOT_4 { get; set; }
        public int ITEM_SLOT_5 { get; set; }
        public int ITEM_SLOT_6 { get; set; }
        public int ITEM_SLOT_7 { get; set; }
        public int ITEM_SLOT_8 { get; set; }
        public int ITEM_SLOT_9 { get; set; }
        public int ITEM_SLOT_10 { get; set; }
        public int Skin_1 { get; set; }
        public int Skin_2 { get; set; }
        public int Skin_3 { get; set; }
        public int Skin_4 { get; set; }
        public int Skin_5 { get; set; }
        public int Skin_6 { get; set; }
        public int MASCOT_ID { get; set; }
        public int POSTER_1 { get; set; }
        public int POSTER_2 { get; set; }
    }
}
