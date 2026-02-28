using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_character : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_character(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_character() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public CharacterDbData SelectByCID(int cid)
        {
            string sql = "SELECT * FROM pangya_character WHERE CID = @p0";
            return _db.Database.SqlQuery<CharacterDbData>(sql, cid).FirstOrDefault();
        }

        public List<CharacterDbData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_character WHERE UID = @p0";
            return _db.Database.SqlQuery<CharacterDbData>(sql, uid).ToList();
        }

        public CharacterDbData SelectByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT * FROM pangya_character WHERE UID = @p0 AND TYPEID = @p1 LIMIT 1";
            return _db.Database.SqlQuery<CharacterDbData>(sql, uid, typeId).FirstOrDefault();
        }

        public bool ExistsByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT COUNT(*) FROM pangya_character WHERE UID = @p0 AND TYPEID = @p1";
            return _db.Database.SqlQuery<int>(sql, uid, typeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(CharacterDbData data)
        {
            string sql = @"INSERT INTO pangya_character 
                (UID, TYPEID, GIFT_FLAG, HAIR_COLOR, POWER, CONTROL, IMPACT, SPIN, CURVE, CUTIN, 
                 AuxPart, AuxPart2, 
                 PART_TYPEID_1, PART_TYPEID_2, PART_TYPEID_3, PART_TYPEID_4, PART_TYPEID_5, 
                 PART_TYPEID_6, PART_TYPEID_7, PART_TYPEID_8, PART_TYPEID_9, PART_TYPEID_10, 
                 PART_TYPEID_11, PART_TYPEID_12, PART_TYPEID_13, PART_TYPEID_14, PART_TYPEID_15, 
                 PART_TYPEID_16, PART_TYPEID_17, PART_TYPEID_18, PART_TYPEID_19, PART_TYPEID_20, 
                 PART_TYPEID_21, PART_TYPEID_22, PART_TYPEID_23, PART_TYPEID_24, 
                 PART_IDX_1, PART_IDX_2, PART_IDX_3, PART_IDX_4, PART_IDX_5, 
                 PART_IDX_6, PART_IDX_7, PART_IDX_8, PART_IDX_9, PART_IDX_10, 
                 PART_IDX_11, PART_IDX_12, PART_IDX_13, PART_IDX_14, PART_IDX_15, 
                 PART_IDX_16, PART_IDX_17, PART_IDX_18, PART_IDX_19, PART_IDX_20, 
                 PART_IDX_21, PART_IDX_22, PART_IDX_23, PART_IDX_24) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, 
                        @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18, @p19, 
                        @p20, @p21, @p22, @p23, @p24, @p25, @p26, @p27, @p28, @p29, 
                        @p30, @p31, @p32, @p33, @p34, @p35, @p36, @p37, @p38, @p39, 
                        @p40, @p41, @p42, @p43, @p44, @p45, @p46, @p47, @p48, @p49, 
                        @p50, @p51, @p52, @p53, @p54, @p55, @p56, @p57, @p58, @p59);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID, data.TYPEID, data.GIFT_FLAG, data.HAIR_COLOR, data.POWER, data.CONTROL,
                data.IMPACT, data.SPIN, data.CURVE, data.CUTIN, data.AuxPart, data.AuxPart2,
                data.PART_TYPEID_1, data.PART_TYPEID_2, data.PART_TYPEID_3, data.PART_TYPEID_4, data.PART_TYPEID_5,
                data.PART_TYPEID_6, data.PART_TYPEID_7, data.PART_TYPEID_8, data.PART_TYPEID_9, data.PART_TYPEID_10,
                data.PART_TYPEID_11, data.PART_TYPEID_12, data.PART_TYPEID_13, data.PART_TYPEID_14, data.PART_TYPEID_15,
                data.PART_TYPEID_16, data.PART_TYPEID_17, data.PART_TYPEID_18, data.PART_TYPEID_19, data.PART_TYPEID_20,
                data.PART_TYPEID_21, data.PART_TYPEID_22, data.PART_TYPEID_23, data.PART_TYPEID_24,
                data.PART_IDX_1, data.PART_IDX_2, data.PART_IDX_3, data.PART_IDX_4, data.PART_IDX_5,
                data.PART_IDX_6, data.PART_IDX_7, data.PART_IDX_8, data.PART_IDX_9, data.PART_IDX_10,
                data.PART_IDX_11, data.PART_IDX_12, data.PART_IDX_13, data.PART_IDX_14, data.PART_IDX_15,
                data.PART_IDX_16, data.PART_IDX_17, data.PART_IDX_18, data.PART_IDX_19, data.PART_IDX_20,
                data.PART_IDX_21, data.PART_IDX_22, data.PART_IDX_23, data.PART_IDX_24
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(CharacterDbData data)
        {
            string sql = @"UPDATE pangya_character SET 
                UID = @p0, TYPEID = @p1, GIFT_FLAG = @p2, HAIR_COLOR = @p3, POWER = @p4, CONTROL = @p5, 
                IMPACT = @p6, SPIN = @p7, CURVE = @p8, CUTIN = @p9, AuxPart = @p10, AuxPart2 = @p11, 
                PART_TYPEID_1 = @p12, PART_TYPEID_2 = @p13, PART_TYPEID_3 = @p14, PART_TYPEID_4 = @p15, PART_TYPEID_5 = @p16, 
                PART_TYPEID_6 = @p17, PART_TYPEID_7 = @p18, PART_TYPEID_8 = @p19, PART_TYPEID_9 = @p20, PART_TYPEID_10 = @p21, 
                PART_TYPEID_11 = @p22, PART_TYPEID_12 = @p23, PART_TYPEID_13 = @p24, PART_TYPEID_14 = @p25, PART_TYPEID_15 = @p26, 
                PART_TYPEID_16 = @p27, PART_TYPEID_17 = @p28, PART_TYPEID_18 = @p29, PART_TYPEID_19 = @p30, PART_TYPEID_20 = @p31, 
                PART_TYPEID_21 = @p32, PART_TYPEID_22 = @p33, PART_TYPEID_23 = @p34, PART_TYPEID_24 = @p35, 
                PART_IDX_1 = @p36, PART_IDX_2 = @p37, PART_IDX_3 = @p38, PART_IDX_4 = @p39, PART_IDX_5 = @p40, 
                PART_IDX_6 = @p41, PART_IDX_7 = @p42, PART_IDX_8 = @p43, PART_IDX_9 = @p44, PART_IDX_10 = @p45, 
                PART_IDX_11 = @p46, PART_IDX_12 = @p47, PART_IDX_13 = @p48, PART_IDX_14 = @p49, PART_IDX_15 = @p50, 
                PART_IDX_16 = @p51, PART_IDX_17 = @p52, PART_IDX_18 = @p53, PART_IDX_19 = @p54, PART_IDX_20 = @p55, 
                PART_IDX_21 = @p56, PART_IDX_22 = @p57, PART_IDX_23 = @p58, PART_IDX_24 = @p59 
                WHERE CID = @p60";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID, data.TYPEID, data.GIFT_FLAG, data.HAIR_COLOR, data.POWER, data.CONTROL,
                data.IMPACT, data.SPIN, data.CURVE, data.CUTIN, data.AuxPart, data.AuxPart2,
                data.PART_TYPEID_1, data.PART_TYPEID_2, data.PART_TYPEID_3, data.PART_TYPEID_4, data.PART_TYPEID_5,
                data.PART_TYPEID_6, data.PART_TYPEID_7, data.PART_TYPEID_8, data.PART_TYPEID_9, data.PART_TYPEID_10,
                data.PART_TYPEID_11, data.PART_TYPEID_12, data.PART_TYPEID_13, data.PART_TYPEID_14, data.PART_TYPEID_15,
                data.PART_TYPEID_16, data.PART_TYPEID_17, data.PART_TYPEID_18, data.PART_TYPEID_19, data.PART_TYPEID_20,
                data.PART_TYPEID_21, data.PART_TYPEID_22, data.PART_TYPEID_23, data.PART_TYPEID_24,
                data.PART_IDX_1, data.PART_IDX_2, data.PART_IDX_3, data.PART_IDX_4, data.PART_IDX_5,
                data.PART_IDX_6, data.PART_IDX_7, data.PART_IDX_8, data.PART_IDX_9, data.PART_IDX_10,
                data.PART_IDX_11, data.PART_IDX_12, data.PART_IDX_13, data.PART_IDX_14, data.PART_IDX_15,
                data.PART_IDX_16, data.PART_IDX_17, data.PART_IDX_18, data.PART_IDX_19, data.PART_IDX_20,
                data.PART_IDX_21, data.PART_IDX_22, data.PART_IDX_23, data.PART_IDX_24,
                data.CID
            );
        }

        public int UpdateStats(int cid, byte power, byte control, byte impact, byte spin, byte curve)
        {
            string sql = @"UPDATE pangya_character SET 
                POWER = @p0, CONTROL = @p1, IMPACT = @p2, SPIN = @p3, CURVE = @p4 
                WHERE CID = @p5";
            return _db.Database.ExecuteSqlCommand(sql, power, control, impact, spin, curve, cid);
        }

        public int UpdatePart(int cid, int slotNumber, int partTypeId, int partIdx)
        {
            string sql = $"UPDATE pangya_character SET PART_TYPEID_{slotNumber} = @p0, PART_IDX_{slotNumber} = @p1 WHERE CID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, partTypeId, partIdx, cid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int cid)
        {
            string sql = "DELETE FROM pangya_character WHERE CID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, cid);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_character WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class CharacterDbData
    {
        public int CID { get; set; }
        public int UID { get; set; }
        public int TYPEID { get; set; }
        public byte? GIFT_FLAG { get; set; }
        public byte? HAIR_COLOR { get; set; }
        public byte? POWER { get; set; }
        public byte? CONTROL { get; set; }
        public byte? IMPACT { get; set; }
        public byte? SPIN { get; set; }
        public byte? CURVE { get; set; }
        public int? CUTIN { get; set; }
        public int? AuxPart { get; set; }
        public int? AuxPart2 { get; set; }
        public int? PART_TYPEID_1 { get; set; }
        public int? PART_TYPEID_2 { get; set; }
        public int? PART_TYPEID_3 { get; set; }
        public int? PART_TYPEID_4 { get; set; }
        public int? PART_TYPEID_5 { get; set; }
        public int? PART_TYPEID_6 { get; set; }
        public int? PART_TYPEID_7 { get; set; }
        public int? PART_TYPEID_8 { get; set; }
        public int? PART_TYPEID_9 { get; set; }
        public int? PART_TYPEID_10 { get; set; }
        public int? PART_TYPEID_11 { get; set; }
        public int? PART_TYPEID_12 { get; set; }
        public int? PART_TYPEID_13 { get; set; }
        public int? PART_TYPEID_14 { get; set; }
        public int? PART_TYPEID_15 { get; set; }
        public int? PART_TYPEID_16 { get; set; }
        public int? PART_TYPEID_17 { get; set; }
        public int? PART_TYPEID_18 { get; set; }
        public int? PART_TYPEID_19 { get; set; }
        public int? PART_TYPEID_20 { get; set; }
        public int? PART_TYPEID_21 { get; set; }
        public int? PART_TYPEID_22 { get; set; }
        public int? PART_TYPEID_23 { get; set; }
        public int? PART_TYPEID_24 { get; set; }
        public int? PART_IDX_1 { get; set; }
        public int? PART_IDX_2 { get; set; }
        public int? PART_IDX_3 { get; set; }
        public int? PART_IDX_4 { get; set; }
        public int? PART_IDX_5 { get; set; }
        public int? PART_IDX_6 { get; set; }
        public int? PART_IDX_7 { get; set; }
        public int? PART_IDX_8 { get; set; }
        public int? PART_IDX_9 { get; set; }
        public int? PART_IDX_10 { get; set; }
        public int? PART_IDX_11 { get; set; }
        public int? PART_IDX_12 { get; set; }
        public int? PART_IDX_13 { get; set; }
        public int? PART_IDX_14 { get; set; }
        public int? PART_IDX_15 { get; set; }
        public int? PART_IDX_16 { get; set; }
        public int? PART_IDX_17 { get; set; }
        public int? PART_IDX_18 { get; set; }
        public int? PART_IDX_19 { get; set; }
        public int? PART_IDX_20 { get; set; }
        public int? PART_IDX_21 { get; set; }
        public int? PART_IDX_22 { get; set; }
        public int? PART_IDX_23 { get; set; }
        public int? PART_IDX_24 { get; set; }
    }
}
