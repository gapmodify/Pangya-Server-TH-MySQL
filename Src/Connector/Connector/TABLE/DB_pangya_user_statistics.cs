using Connector.DataBase;
using System;
using System.Linq;

namespace Connector.Table
{
    public class DB_pangya_user_statistics : IDisposable
    {
        private readonly MySqlDbContext _db;

        public DB_pangya_user_statistics(MySqlDbContext db)
        {
            _db = db;
        }

        public DB_pangya_user_statistics() : this(DbContextFactory.CreateMySql())
        {
        }

        #region SELECT Methods

        public UserStatisticsData SelectByID(int id)
        {
            string sql = "SELECT * FROM pangya_user_statistics WHERE ID = @p0";
            return _db.Database.SqlQuery<UserStatisticsData>(sql, id).FirstOrDefault();
        }

        public UserStatisticsData SelectByUID(int uid)
        {
            string sql = "SELECT * FROM pangya_user_statistics WHERE UID = @p0";
            return _db.Database.SqlQuery<UserStatisticsData>(sql, uid).FirstOrDefault();
        }

        public bool ExistsByUID(int uid)
        {
            string sql = "SELECT COUNT(*) FROM pangya_user_statistics WHERE UID = @p0";
            return _db.Database.SqlQuery<int>(sql, uid).FirstOrDefault() > 0;
        }

        #endregion

        #region INSERT Methods

        public int Insert(UserStatisticsData data)
        {
            string sql = @"INSERT INTO pangya_user_statistics 
                (UID, Drive, Putt, Playtime, Longest, Distance, Pangya, Hole, TeamHole, Holeinone, 
                 OB, Bunker, Fairway, Albatross, Holein, Pang, Timeout, Game_Level, Game_Point, PuttIn, 
                 LongestPuttIn, LongestChipIn, NoMannerGameCount, ShotTime, GameCount, DisconnectGames, 
                 wTeamWin, wTeamGames, LadderPoint, LadderWin, LadderLose, LadderDraw, ComboCount, 
                 MaxComboCount, TotalScore, BestScore0, BestScore1, BestScore2, BestScore3, BESTSCORE4, 
                 MaxPang0, MaxPang1, MaxPang2, MaxPang3, MAXPANG4, SumPang, LadderHole, GameCountSeason, 
                 SkinsPang, SkinsWin, SkinsLose, SkinsRunHoles, SkinsStrikePoint, SkinsAllinCount, 
                 EventValue, EventFlag) 
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, 
                        @p15, @p16, @p17, @p18, @p19, @p20, @p21, @p22, @p23, @p24, @p25, @p26, @p27, 
                        @p28, @p29, @p30, @p31, @p32, @p33, @p34, @p35, @p36, @p37, @p38, @p39, @p40, 
                        @p41, @p42, @p43, @p44, @p45, @p46, @p47, @p48, @p49, @p50, @p51, @p52, @p53, 
                        @p54, @p55);
                SELECT LAST_INSERT_ID();";

            return _db.Database.SqlQuery<int>(sql,
                data.UID, data.Drive, data.Putt, data.Playtime, data.Longest, data.Distance, data.Pangya,
                data.Hole, data.TeamHole, data.Holeinone, data.OB, data.Bunker, data.Fairway, data.Albatross,
                data.Holein, data.Pang, data.Timeout, data.Game_Level, data.Game_Point, data.PuttIn,
                data.LongestPuttIn, data.LongestChipIn, data.NoMannerGameCount, data.ShotTime, data.GameCount,
                data.DisconnectGames, data.wTeamWin, data.wTeamGames, data.LadderPoint, data.LadderWin,
                data.LadderLose, data.LadderDraw, data.ComboCount, data.MaxComboCount, data.TotalScore,
                data.BestScore0, data.BestScore1, data.BestScore2, data.BestScore3, data.BESTSCORE4,
                data.MaxPang0, data.MaxPang1, data.MaxPang2, data.MaxPang3, data.MAXPANG4, data.SumPang,
                data.LadderHole, data.GameCountSeason, data.SkinsPang, data.SkinsWin, data.SkinsLose,
                data.SkinsRunHoles, data.SkinsStrikePoint, data.SkinsAllinCount, data.EventValue, data.EventFlag
            ).FirstOrDefault();
        }

        #endregion

        #region UPDATE Methods

        public int Update(UserStatisticsData data)
        {
            string sql = @"UPDATE pangya_user_statistics SET 
                UID = @p0, Drive = @p1, Putt = @p2, Playtime = @p3, Longest = @p4, Distance = @p5, 
                Pangya = @p6, Hole = @p7, TeamHole = @p8, Holeinone = @p9, OB = @p10, Bunker = @p11, 
                Fairway = @p12, Albatross = @p13, Holein = @p14, Pang = @p15, Timeout = @p16, 
                Game_Level = @p17, Game_Point = @p18, PuttIn = @p19, LongestPuttIn = @p20, 
                LongestChipIn = @p21, NoMannerGameCount = @p22, ShotTime = @p23, GameCount = @p24, 
                DisconnectGames = @p25, wTeamWin = @p26, wTeamGames = @p27, LadderPoint = @p28, 
                LadderWin = @p29, LadderLose = @p30, LadderDraw = @p31, ComboCount = @p32, 
                MaxComboCount = @p33, TotalScore = @p34, BestScore0 = @p35, BestScore1 = @p36, 
                BestScore2 = @p37, BestScore3 = @p38, BESTSCORE4 = @p39, MaxPang0 = @p40, MaxPang1 = @p41, 
                MaxPang2 = @p42, MaxPang3 = @p43, MAXPANG4 = @p44, SumPang = @p45, LadderHole = @p46, 
                GameCountSeason = @p47, SkinsPang = @p48, SkinsWin = @p49, SkinsLose = @p50, 
                SkinsRunHoles = @p51, SkinsStrikePoint = @p52, SkinsAllinCount = @p53, EventValue = @p54, 
                EventFlag = @p55 
                WHERE UID = @p56";

            return _db.Database.ExecuteSqlCommand(sql,
                data.UID, data.Drive, data.Putt, data.Playtime, data.Longest, data.Distance, data.Pangya,
                data.Hole, data.TeamHole, data.Holeinone, data.OB, data.Bunker, data.Fairway, data.Albatross,
                data.Holein, data.Pang, data.Timeout, data.Game_Level, data.Game_Point, data.PuttIn,
                data.LongestPuttIn, data.LongestChipIn, data.NoMannerGameCount, data.ShotTime, data.GameCount,
                data.DisconnectGames, data.wTeamWin, data.wTeamGames, data.LadderPoint, data.LadderWin,
                data.LadderLose, data.LadderDraw, data.ComboCount, data.MaxComboCount, data.TotalScore,
                data.BestScore0, data.BestScore1, data.BestScore2, data.BestScore3, data.BESTSCORE4,
                data.MaxPang0, data.MaxPang1, data.MaxPang2, data.MaxPang3, data.MAXPANG4, data.SumPang,
                data.LadderHole, data.GameCountSeason, data.SkinsPang, data.SkinsWin, data.SkinsLose,
                data.SkinsRunHoles, data.SkinsStrikePoint, data.SkinsAllinCount, data.EventValue, data.EventFlag,
                data.UID
            );
        }

        public int UpdatePang(int uid, int pang)
        {
            string sql = "UPDATE pangya_user_statistics SET Pang = @p0 WHERE UID = @p1";
            return _db.Database.ExecuteSqlCommand(sql, pang, uid);
        }

        public int UpdateGameLevel(int uid, short level, int points)
        {
            string sql = "UPDATE pangya_user_statistics SET Game_Level = @p0, Game_Point = @p1 WHERE UID = @p2";
            return _db.Database.ExecuteSqlCommand(sql, level, points, uid);
        }

        #endregion

        #region DELETE Methods

        public int Delete(int uid)
        {
            string sql = "DELETE FROM pangya_user_statistics WHERE UID = @p0";
            return _db.Database.ExecuteSqlCommand(sql, uid);
        }

        #endregion

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
    public class StatisticsData
    {
        public int Drive { get; set; }
        public int Putt { get; set; }
        public int Playtime { get; set; }
        public int ShotTime { get; set; }
        public float Longest { get; set; }
        public int Pangya { get; set; }
        public int Timeout { get; set; }
        public int OB { get; set; }
        public int Distance { get; set; }
        public int Hole { get; set; }
        public int TeamHole { get; set; }
        public int Holeinone { get; set; }
        public int Bunker { get; set; }
        public int Fairway { get; set; }
        public int Albatross { get; set; }
        public int Holein { get; set; }
        public int PuttIn { get; set; }
        public float LongestPutt { get; set; }
        public float LongestChipIn { get; set; }
        public int Game_Point { get; set; }
        public int Game_Level { get; set; }
        public long Pang { get; set; }
        public int TotalScore { get; set; }
        public int BestScore0 { get; set; }
        public int BestScore1 { get; set; }
        public int BestScore2 { get; set; }
        public int BestScore3 { get; set; }
        public int BESTSCORE4 { get; set; }
        public long MaxPang0 { get; set; }
        public long MaxPang1 { get; set; }
        public long MaxPang2 { get; set; }
        public long MaxPang3 { get; set; }
        public long MaxPang4 { get; set; }
        public long SumPang { get; set; }
        public int GameCount { get; set; }
        public int DisconnectGames { get; set; }
        public int wTeamWin { get; set; }
        public int wTeamGames { get; set; }
        public int LadderPoint { get; set; }
        public int LadderWin { get; set; }
        public int LadderLose { get; set; }
        public int LadderDraw { get; set; }
        public int LadderHole { get; set; }
        public int ComboCount { get; set; }
        public int MaxComboCount { get; set; }
        public int NoMannerGameCount { get; set; }
        public long SkinsPang { get; set; }
        public int SkinsWin { get; set; }
        public int SkinsLose { get; set; }
        public int SkinsRunHoles { get; set; }
        public int SkinsStrikePoint { get; set; }
        public int SkinsAllinCount { get; set; }
        public int GameCountSeason { get; set; }
    }
    public class UserStatisticsData
    {
        public int ID { get; set; }
        public int UID { get; set; }
        public int Drive { get; set; }
        public int Putt { get; set; }
        public int Playtime { get; set; }
        public float Longest { get; set; }
        public int Distance { get; set; }
        public int Pangya { get; set; }
        public int Hole { get; set; }
        public int TeamHole { get; set; }
        public int Holeinone { get; set; }
        public int OB { get; set; }
        public int Bunker { get; set; }
        public int Fairway { get; set; }
        public int Albatross { get; set; }
        public int Holein { get; set; }
        public int Pang { get; set; }
        public int Timeout { get; set; }
        public short Game_Level { get; set; }
        public int Game_Point { get; set; }
        public int PuttIn { get; set; }
        public float LongestPuttIn { get; set; }
        public float LongestChipIn { get; set; }
        public int NoMannerGameCount { get; set; }
        public int ShotTime { get; set; }
        public int GameCount { get; set; }
        public int DisconnectGames { get; set; }
        public int wTeamWin { get; set; }
        public int wTeamGames { get; set; }
        public short LadderPoint { get; set; }
        public short LadderWin { get; set; }
        public short LadderLose { get; set; }
        public short LadderDraw { get; set; }
        public int ComboCount { get; set; }
        public int MaxComboCount { get; set; }
        public int TotalScore { get; set; }
        public short BestScore0 { get; set; }
        public short BestScore1 { get; set; }
        public short BestScore2 { get; set; }
        public short BestScore3 { get; set; }
        public short BESTSCORE4 { get; set; }
        public int? MaxPang0 { get; set; }
        public int? MaxPang1 { get; set; }
        public int? MaxPang2 { get; set; }
        public int? MaxPang3 { get; set; }
        public int? MAXPANG4 { get; set; }
        public int SumPang { get; set; }
        public short LadderHole { get; set; }
        public int GameCountSeason { get; set; }
        public long SkinsPang { get; set; }
        public int SkinsWin { get; set; }
        public int SkinsLose { get; set; }
        public int SkinsRunHoles { get; set; }
        public int SkinsStrikePoint { get; set; }
        public int SkinsAllinCount { get; set; }
        public int EventValue { get; set; }
        public int EventFlag { get; set; }
    }
}
