using PangyaAPI.BinaryModels;
using Connector.Table;
using Game.GameTools;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Game.Client
{
    // Result classes for raw SQL queries
    public class StatisticResult
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

    public class GuildResult
    {
        public string GUILD_NAME { get; set; }
        public int GUILD_INDEX { get; set; }
        public byte GUILD_POSITION { get; set; }
        public string GUILD_IMAGE { get; set; }
        public string GUILD_INTRODUCING { get; set; }
        public string GUILD_LEADER_NICKNAME { get; set; }
        public string GUILD_NOTICE { get; set; }
        public int GUILD_LEADER_UID { get; set; }
        public int GUILD_TOTAL_MEMBER { get; set; }
        public DateTime GUILD_CREATE_DATE { get; set; }
    }

    public partial class GPlayer
    {
        #region Load Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadStatistic()
        {
            using (var dbStats = new DB_pangya_user_statistics())
            {
                var Data = dbStats.SelectByUID((int)GetUID);
                
                if (Data != null)
                {
                    UserStatistic = new Data.StatisticData()
                    {
                        Drive = (uint)Data.Drive,
                        Putt = (uint)Data.Putt,
                        PlayTime = (uint)Data.Playtime,
                        ShotTime = (uint)Data.ShotTime,
                        LongestDistance = Data.Longest,
                        Pangya = (uint)Data.Pangya,
                        TimeOut = (uint)Data.Timeout,
                        OB = (uint)Data.OB,
                        DistanceTotal = (uint)Data.Distance,
                        Hole = (uint)Data.Hole,
                        TeamHole = (uint)Data.TeamHole,
                        HIO = (uint)Data.Holeinone,
                        Bunker = (ushort)Data.Bunker,
                        Fairway = (uint)Data.Fairway,
                        Albratoss = (uint)Data.Albatross,
                        Holein = (uint)Data.Holein,
                        Puttin = (uint)Data.PuttIn,
                        LongestPutt = Data.LongestPuttIn,
                        LongestChip = Data.LongestChipIn,
                        EXP = (uint)Data.Game_Point,
                        Level = (byte)Data.Game_Level,
                        Pang = (ulong)Data.Pang,
                        TotalScore = (uint)Data.TotalScore,
                        Score = new byte[5] 
                        { 
                            (byte)Data.BestScore0, 
                            (byte)Data.BestScore1, 
                            (byte)Data.BestScore2, 
                            (byte)Data.BestScore3, 
                            (byte)Data.BESTSCORE4 
                        },
                        Unknown = 0,
                        MaxPang0 = (ulong)(Data.MaxPang0 ?? 0),
                        MaxPang1 = (ulong)(Data.MaxPang1 ?? 0),
                        MaxPang2 = (ulong)(Data.MaxPang2 ?? 0),
                        MaxPang3 = (ulong)(Data.MaxPang3 ?? 0),
                        MaxPang4 = (ulong)(Data.MAXPANG4 ?? 0),
                        SumPang = (ulong)Data.SumPang,
                        GamePlayed = (uint)Data.GameCount,
                        Disconnected = (uint)Data.DisconnectGames,
                        TeamWin = (uint)Data.wTeamWin,
                        TeamGame = (uint)Data.wTeamGames,
                        LadderPoint = (uint)Data.LadderPoint,
                        LadderWin = (uint)Data.LadderWin,
                        LadderLose = (uint)Data.LadderLose,
                        LadderDraw = (uint)Data.LadderDraw,
                        LadderHole = (uint)Data.LadderHole,
                        ComboCount = (uint)Data.ComboCount,
                        MaxCombo = (uint)Data.MaxComboCount,
                        NoMannerGameCount = (uint)Data.NoMannerGameCount,
                        SkinsPang = (ulong)Data.SkinsPang,
                        SkinsWin = (uint)Data.SkinsWin,
                        SkinsLose = (uint)Data.SkinsLose,
                        SkinsRunHole = (uint)Data.SkinsRunHoles,
                        SkinsStrikePoint = (uint)Data.SkinsStrikePoint,
                        SKinsAllinCount = (uint)Data.SkinsAllinCount,
                        Unknown1 = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },
                        GameCountSeason = (uint)Data.GameCountSeason,
                        Unknown2 = new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, }
                    };
                }
                else
                {
                    // No statistics found - initialize with defaults
                    UserStatistic = new Data.StatisticData()
                    {
                        Level = 1,
                        LadderPoint = 1000,
                        Score = new byte[5] { 127, 127, 127, 127, 127 },
                        Unknown = 0,
                        Unknown1 = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF },
                        Unknown2 = new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }
                    };
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadGuildData()
        {
            var sql = @"
                SELECT 
                    g.GUILD_NAME as GUILD_NAME,
                    g.GUILD_INDEX as GUILD_INDEX,
                    gm.GUILD_POSITION as GUILD_POSITION,
                    g.GUILD_IMAGE as GUILD_IMAGE,
                    g.GUILD_INTRODUCING as GUILD_INTRODUCING,
                    m.Nickname as GUILD_LEADER_NICKNAME,
                    g.GUILD_NOTICE as GUILD_NOTICE,
                    g.GUILD_LEADER_UID as GUILD_LEADER_UID,
                    (SELECT COUNT(*) FROM pangya_guild_member WHERE GUILD_ID = g.GUILD_INDEX) as GUILD_TOTAL_MEMBER,
                    g.GUILD_CREATE_DATE as GUILD_CREATE_DATE
                FROM pangya_guild_member gm
                LEFT JOIN pangya_guild_info g ON gm.GUILD_ID = g.GUILD_INDEX
                LEFT JOIN pangya_member m ON g.GUILD_LEADER_UID = m.UID
                WHERE gm.GUILD_MEMBER_UID = @p0";

            var Data = _db.Database.SqlQuery<GuildResult>(sql, (int)GetUID).FirstOrDefault();
            
            if (Data != null)
            {
                GuildInfo = new Data.GuildData
                {
                    Name = Data.GUILD_NAME,
                    ID = (uint)Data.GUILD_INDEX,
                    Position = Data.GUILD_POSITION,
                    Image = Data.GUILD_IMAGE ?? string.Empty,
                    Introducing = Data.GUILD_INTRODUCING,
                    LeaderNickname = Data.GUILD_LEADER_NICKNAME,
                    Notice = Data.GUILD_NOTICE,
                    LeaderUID = (uint)Data.GUILD_LEADER_UID,
                    TotalMember = (uint)Data.GUILD_TOTAL_MEMBER,
                    Create_Date = Data.GUILD_CREATE_DATE
                };
            }
            else
            {
                // ไม่มี guild
                GuildInfo = new Data.GuildData();
            }

            if (GuildInfo.LeaderUID == 0)
            {
                GuildInfo.LeaderUID = uint.MaxValue;
            }
        }
        #endregion  
    }
}
