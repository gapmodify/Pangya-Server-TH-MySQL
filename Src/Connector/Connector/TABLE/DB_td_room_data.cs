using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_td_room_data : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_td_room_data(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_td_room_data() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public RoomData SelectByIDX(int idx)
        {
            string sql = "SELECT * FROM td_room_data WHERE IDX = @p0";
            return _db.Database.SqlQuery<RoomData>(sql, idx).FirstOrDefault();
        }

        public List<RoomData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM td_room_data WHERE UID = @p0 AND VALID = 1";
            return _db.Database.SqlQuery<RoomData>(sql, uid).ToList();
        }

        public List<RoomData> SelectByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT * FROM td_room_data WHERE UID = @p0 AND TYPEID = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<RoomData>(sql, uid, typeId).ToList();
        }

        public bool ExistsByUIDAndTypeID(int uid, int typeId)
        {
            string sql = "SELECT COUNT(*) FROM td_room_data WHERE UID = @p0 AND TYPEID = @p1 AND VALID = 1";
            return _db.Database.SqlQuery<int>(sql, uid, typeId).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(RoomData data)
        {
            string sql = @"INSERT INTO td_room_data 
                (UID, TYPEID, POS_X, POS_Y, POS_Z, POS_R, VALID) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.TYPEID,
                data.POS_X,
                data.POS_Y,
                data.POS_Z,
                data.POS_R,
                data.VALID
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(RoomData data)
        {
            string sql = @"UPDATE td_room_data SET 
                UID = @p0, TYPEID = @p1, POS_X = @p2, POS_Y = @p3, POS_Z = @p4, POS_R = @p5, VALID = @p6 
                WHERE IDX = @p7";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.TYPEID,
                data.POS_X,
                data.POS_Y,
                data.POS_Z,
                data.POS_R,
                data.VALID,
                data.IDX
            );
        }

        public int UpdatePosition(int idx, decimal posX, decimal posY, decimal posZ, decimal posR)
        {
            string sql = "UPDATE td_room_data SET POS_X = @p0, POS_Y = @p1, POS_Z = @p2, POS_R = @p3 WHERE IDX = @p4";
            return _db.Database.ExecuteSqlCommand(sql, posX, posY, posZ, posR, idx);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int idx)
        {
            string sql = "DELETE FROM td_room_data WHERE IDX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, idx);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM td_room_data WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        public int SoftDelete(int idx)
        {
            string sql = "UPDATE td_room_data SET VALID = 0 WHERE IDX = @p0";
            return _db.Database.ExecuteSqlCommand(sql, idx);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class RoomData
    {
        public int IDX { get; set; }
        public int UID { get; set; }
        public int TYPEID { get; set; }
        public decimal? POS_X { get; set; }
        public decimal? POS_Y { get; set; }
        public decimal? POS_Z { get; set; }
        public decimal? POS_R { get; set; }
        public byte? VALID { get; set; }
        public DateTime? GETDATE { get; set; }
    }
}
