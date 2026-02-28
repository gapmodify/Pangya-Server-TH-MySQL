using Connector.DataBase;
using Game.Client.Inventory;
using Game.Client.Inventory.Data;
using Game.Functions.EXP;
using PangyaAPI.BinaryModels;
using System;
using System.Linq;

namespace Game.Client
{
    public partial class GPlayer
    {
        public bool RemoveCookie(uint Amount)
        {
            if (GetCookie < Amount)
            {
                return (false);
            }
            GetCookie -= (int)Amount;
            var table1 = $"UPDATE pangya_personal SET CookieAmt = {GetCookie} WHERE UID = {GetUID}";
            _db.Database.ExecuteSqlCommand(table1);
            return (true);
        }

        public bool RemoveLockerPang(uint Amount)
        {
            if ((LockerPang < Amount)) return (false);
            LockerPang -= (int)Amount;
            var table1 = $"UPDATE pangya_personal SET PangLockerAmt = {LockerPang} WHERE UID = {GetUID}";
            _db.Database.ExecuteSqlCommand(table1);
            return (true);
        }

        public bool RemovePang(uint Amount)
        {
            if (UserStatistic.Pang < Amount)
            {
                return (false);
            }
            UserStatistic.Pang -= Amount;
            var table1 = $"UPDATE pangya_user_statistics SET Pang = {UserStatistic.Pang} WHERE UID = {GetUID}";
            _db.Database.ExecuteSqlCommand(table1);
            return (true);
        }

        public bool AddLockerPang(uint Amount)
        {
            LockerPang += (int)Amount;
            var table1 = $"UPDATE pangya_personal SET PangLockerAmt = {LockerPang} WHERE UID = {GetUID}";
            _db.Database.ExecuteSqlCommand(table1);
            return (true);
        }

        public bool AddPang(uint Amount)
        {
            if (UserStatistic.Pang >= uint.MaxValue)
            {
                return false;
            }
            UserStatistic.Pang += Amount;
            var table1 = $"UPDATE pangya_user_statistics SET Pang = {UserStatistic.Pang} WHERE UID = {GetUID}";
            _db.Database.ExecuteSqlCommand(table1);
            return true;
        }

        public bool AddCookie(uint Amount)
        {
            if (GetCookie >= uint.MaxValue)
            {
                return false;
            }
            GetCookie += (int)Amount;
            var table1 = $"UPDATE pangya_personal SET CookieAmt = {GetCookie} WHERE UID = {GetUID}";
            _db.Database.ExecuteSqlCommand(table1);
            return true;
        }
        public bool UpdateMapStatistic(Data.StatisticData Statistic, byte Map, sbyte Score, uint MaxPang)
        {
            var query = $" Exec dbo.ProcUpdateMapStatistics @UID = '{GetUID}', @MAP ='{Map}',@DRIVE = '{Statistic.Drive}', @PUTT = '{Statistic.Putt}', @HOLE = '{Statistic.Hole}',  @FAIRWAY = '{Statistic.Fairway}', @HOLEIN = '{Statistic.Holein}', @PUTTIN = '{Statistic.Puttin}', @TOTALSCORE = '{Score}',  @BESTSCORE = '{Score}',  @MAXPANG = '{MaxPang}',  @CHARTYPEID = '{this.Inventory.GetCharTypeID()}',  @ASSIST = '{this.Assist}'";

            var IsNewRecord = _db.Database.SqlQuery<int>(query).FirstOrDefault();
            return IsNewRecord == 1;
        }

        public bool AddExp(uint Count)
        {
            new EXPSystem(this, Count);
            return true;
        }

        public void SetCookie(int Cookie)
        {
            GetCookie = Cookie;
        }

        public bool SetAuthKey1(string TAUTH_KEY_1)
        {
            GetAuth1 = TAUTH_KEY_1;
            return true;
        }

        public bool SetAuthKey2(string TAUTH_KEY_2)
        {
            GetAuth2 = TAUTH_KEY_2;
            return true;
        }

        public bool SetCapabilities(byte TCapa)
        {
            GetCapability = TCapa;
            if (TCapa == 4)
            {
                Visible = 4;
            }
            return true;
        }

        public void SetExp(uint Amount)
        {
            UserStatistic.EXP = Amount;
        }

        public void SetGameID(uint ID)
        {
            GameID = (ushort)ID;
        }

        public void SetLevel(byte Amount)
        {
            UserStatistic.Level = Amount;
        }

        public bool SetLogin(string TLogin)
        {
            GetLogin = TLogin;
            return true;
        }

        public bool SetNickname(string TNickname)
        {
            GetNickname = TNickname;
            return true;
        }

        public bool SetSex(Byte TSex)
        {
            GetSex = TSex;
            return true;
        }

        public bool SetUID(uint TUID)
        {
            GetUID = TUID;
            if (Inventory == null)
            {
                Inventory = new PlayerInventory(TUID);
            }
            return true;
        }

        public void SetTutorial(uint Type, uint value)
        {
            // แทน ProcTutorialSet ด้วย raw SQL
            _db.Database.ExecuteSqlCommand(
                "UPDATE pangya_member SET Tutorial = @p0 WHERE UID = @p1",
                value, (int)GetUID);

            SetTutorial();
        }

        public void SetTutorial()
        {
            // แทน pangya_member.Any() ด้วย raw SQL query
            var tutorialStatus = _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM pangya_member WHERE UID = @p0 AND Tutorial = 2",
                GetUID).FirstOrDefault();
            
            TutorialCompleted = tutorialStatus > 0;
        }

      

      

        public AddData AddItem(AddItem ItemAddData)
        {
            switch (ItemAddData.ItemIffId)
            {
                // case pang and exp pocket
                case 0x1A00015D: // exp pocket
                    AddExp(ItemAddData.Quantity);
                    break;
                case 0x1A000010:// pang pocket
                    AddPang(ItemAddData.Quantity);
                    break;
            }
            return Inventory.AddItem(ItemAddData);
        }

        public void ReloadAchievement()
        {
            if (Achievements == null)
                Achievements = new System.Collections.Generic.List<Client.Data.TAchievement>();
            if (AchievementQuests == null)
                AchievementQuests = new System.Collections.Generic.List<Client.Data.TAchievementQuest>();
            if (AchievemetCounters == null)
                AchievemetCounters = new System.Collections.Generic.Dictionary<uint, Client.Data.TAchievementCounter>();
            
            // TODO: Load real achievement data from database
            // For now, add mock data to prevent "No Records" popup
            if (Achievements.Count == 0)
            {
                // Add sample achievement for testing
                Achievements.Add(new Client.Data.TAchievement
                {
                    ID = 1,
                    TypeID = 1,
                    AchievementType = 0
                });
                
                AchievementQuests.Add(new Client.Data.TAchievementQuest
                {
                    ID = 1,
                    AchievementIndex = 1,
                    AchievementTypeID = 1,
                    CounterIndex = 1,
                    SuccessDate = 0,
                    Total = 100
                });
                
                AchievemetCounters[1] = new Client.Data.TAchievementCounter
                {
                    ID = 1,
                    TypeID = 1,
                    Quantity = 100
                };
            }
        }
    }
}
