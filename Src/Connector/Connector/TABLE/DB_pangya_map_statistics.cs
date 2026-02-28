using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_map_statistics : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_map_statistics(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_map_statistics() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public MapStatisticsData SelectByID(int id)
        {
            string sql = "SELECT * FROM pangya_map_statistics WHERE ID = @p0";
            return _db.Database.SqlQuery<MapStatisticsData>(sql, id).FirstOrDefault();
        }

        public List<MapStatisticsData> SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_map_statistics WHERE UID = @p0";
            return _db.Database.SqlQuery<MapStatisticsData>(sql, uid).ToList();
        }

        public MapStatisticsData SelectByUIDAndMap(int uid, short map)
        {
            string sql = "SELECT * FROM pangya_map_statistics WHERE UID = @p0 AND Map = @p1 LIMIT 1";
            return _db.Database.SqlQuery<MapStatisticsData>(sql, uid, map).FirstOrDefault();
        }

        public bool ExistsByUIDAndMap(int uid, short map)
        {
            string sql = "SELECT COUNT(*) FROM pangya_map_statistics WHERE UID = @p0 AND Map = @p1";
            return _db.Database.SqlQuery<int>(sql, uid, map).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(MapStatisticsData data)
        {
            string sql = @"INSERT INTO pangya_map_statistics 
                (UID, Map, Drive, Putt, Hole, Fairway, Holein, PuttIn, TotalScore, BestScore, 
                 MaxPang, CharTypeId, EventScore, Assist) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID,
                data.Map,
                data.Drive,
                data.Putt,
                data.Hole,
                data.Fairway,
                data.Holein,
                data.PuttIn,
                data.TotalScore,
                data.BestScore,
                data.MaxPang,
                data.CharTypeId,
                data.EventScore,
                data.Assist
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(MapStatisticsData data)
        {
            string sql = @"UPDATE pangya_map_statistics SET 
                UID = @p0, Map = @p1, Drive = @p2, Putt = @p3, Hole = @p4, Fairway = @p5, 
                Holein = @p6, PuttIn = @p7, TotalScore = @p8, BestScore = @p9, MaxPang = @p10, 
                CharTypeId = @p11, EventScore = @p12, Assist = @p13 
                WHERE ID = @p14";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID,
                data.Map,
                data.Drive,
                data.Putt,
                data.Hole,
                data.Fairway,
                data.Holein,
                data.PuttIn,
                data.TotalScore,
                data.BestScore,
                data.MaxPang,
                data.CharTypeId,
                data.EventScore,
                data.Assist,
                data.ID
            );
        }

        public int UpdateBestScore(int id, short bestScore)
        {
            string sql = "UPDATE pangya_map_statistics SET BestScore = @p0 WHERE ID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, bestScore, id);
        }

        public int UpdateMaxPang(int id, int maxPang)
        {
            string sql = "UPDATE pangya_map_statistics SET MaxPang = @p0 WHERE ID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, maxPang, id);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int id)
        {
            string sql = "DELETE FROM pangya_map_statistics WHERE ID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, id);
        }

        public int DeleteByUID(int uid)
        {
            string sql = "DELETE FROM pangya_map_statistics WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }

    public class MapStatisticsData
    {
        public int ID { get; set; }
        public int UID { get; set; }
        public short Map { get; set; }
        public int Drive { get; set; }
        public int Putt { get; set; }
        public int Hole { get; set; }
        public int Fairway { get; set; }
        public int Holein { get; set; }
        public int PuttIn { get; set; }
        public int TotalScore { get; set; }
        public short BestScore { get; set; }
        public int MaxPang { get; set; }
        public int CharTypeId { get; set; }
        public byte EventScore { get; set; }
        public byte Assist { get; set; }
        public DateTime? REGDATE { get; set; }
    }
}
