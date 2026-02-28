using PangyaAPI.BinaryModels;
using Connector.DataBase;
using Game.Client.Inventory.Data.Caddie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Client.Inventory.Collection
{
    public class CaddieQueryResult
    {
        public int CID { get; set; }
        public int TYPEID { get; set; }
        public int? SKIN_TYPEID { get; set; }
        public DateTime? SKIN_END_DATE { get; set; }
        public byte cLevel { get; set; }
        public int EXP { get; set; }
        public int RentFlag { get; set; }
        public int? DAY_LEFT { get; set; }
        public int? SKIN_HOUR_LEFT { get; set; }
        public int TriggerPay { get; set; }
        public DateTime? END_DATE { get; set; }
    }

    public class CaddieCollection : List<PlayerCaddieData>
    {
        public CaddieCollection(int PlayerUID)
        {
            Build(PlayerUID);
        }
        // SerialPlayerCaddieData
        public int CadieAdd(PlayerCaddieData Value)
        {
            Value.CaddieNeedUpdate = false;
            Add(Value);
            return Count;
        }

        
        void Build(int UID)
        {
            using (var _db = DbContextFactory.Create())
            {
                var sql = @"
                    SELECT 
                        CID, TYPEID, SKIN_TYPEID, SKIN_END_DATE, cLevel, EXP, RentFlag,
                        DATEDIFF(END_DATE, NOW()) as DAY_LEFT,
                        TIMESTAMPDIFF(HOUR, NOW(), SKIN_END_DATE) as SKIN_HOUR_LEFT,
                        TriggerPay, END_DATE
                    FROM Pangya_Caddie
                    WHERE UID = @p0";

                var caddies = _db.Database.SqlQuery<CaddieQueryResult>(sql, UID).ToList();

                foreach (var info in caddies)
                {
                    if (info.DAY_LEFT == null)
                        info.DAY_LEFT = 0;

                    var SkinHour = (info.SKIN_HOUR_LEFT == null ? (ushort)0 : (ushort)Math.Max(0, info.SKIN_HOUR_LEFT.Value));
                    
                    var caddie = new PlayerCaddieData()
                    {
                        CaddieIdx = (uint)info.CID,
                        CaddieTypeId = (uint)info.TYPEID,
                        CaddieSkin = (uint)(info.SKIN_TYPEID ?? 0),
                        CaddieSkinEndDate = info.SKIN_END_DATE ?? DateTime.MinValue,
                        CaddieLevel = info.cLevel,
                        CaddieExp = (uint)info.EXP,
                        CaddieType = (byte)info.RentFlag,
                        CaddieDay = (ushort)Math.Max(0, info.DAY_LEFT.Value),
                        CaddieSkinDay = SkinHour,
                        CaddieAutoPay = (ushort)info.TriggerPay,
                        CaddieDateEnd = info.END_DATE ?? DateTime.MinValue,
                        CaddieNeedUpdate = false
                    };
                    CadieAdd(caddie);
                }
            }
        }


        public byte[] Build()
        {
            PangyaBinaryWriter Reply;

            using (Reply = new PangyaBinaryWriter())
            {
                Reply.Write(new byte[] { 0x71, 0x00 });
                Reply.WriteUInt16((ushort)Count);
                Reply.WriteUInt16((ushort)Count);
                foreach (PlayerCaddieData CaddieInfo in this)
                {
                    Reply.Write(CaddieInfo.GetCaddieInfo());
                }
                return Reply.GetBytes();
            }
        }

        public byte[] BuildExpiration()
        {
            PangyaBinaryWriter Reply;

            using (Reply = new PangyaBinaryWriter())
            {
                Reply.Write(new byte[] { 0xD4, 0x00 });
                foreach (PlayerCaddieData CaddieInfo in this)
                {
                    Reply.Write(CaddieInfo.GetExpirationNotice());
                }
                return Reply.GetBytes();
            }
        }
        public byte[] GetCaddie()
        {
            PangyaBinaryWriter Result;
            Result = new PangyaBinaryWriter();
            try
            {
                foreach (PlayerCaddieData CaddieInfo in this)
                {
                    Result.Write(CaddieInfo.GetCaddieInfo());
                }
                return Result.GetBytes();
            }
            finally
            {
                Result.Dispose();
            }
        }

        public bool IsExist(UInt32 TypeId)
        {
            foreach (PlayerCaddieData CaddieInfo in this)
            {
                if ((CaddieInfo.CaddieTypeId == TypeId))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CanHaveSkin(UInt32 SkinTypeId)
        {
            foreach (PlayerCaddieData CaddieInfo in this)
            {
                if (CaddieInfo.Exist(SkinTypeId))
                {
                    return true;
                }
            }
            return false;
        }

        public PlayerCaddieData GetCaddieByIndex(UInt32 Index)
        {
            foreach (PlayerCaddieData CaddieInfo in this)
            {
                if (CaddieInfo.CaddieIdx == Index)
                {
                    return CaddieInfo;
                }
            }
            return null;
        }

        public PlayerCaddieData GetCaddieBySkinId(UInt32 SkinTypeId)
        {
            foreach (PlayerCaddieData CaddieInfo in this)
            {
                if (CaddieInfo.Exist(SkinTypeId))
                {
                    return CaddieInfo;
                }
            }
            return null;
        }


        public string GetSqlUpdateCaddie()
        {
            StringBuilder SQLString;
            SQLString = new StringBuilder();
            try
            {
                foreach (PlayerCaddieData CaddieInfo in this)
                {
                    if (CaddieInfo.CaddieNeedUpdate)
                    {
                        SQLString.Append(CaddieInfo.GetSQLUpdateString());
                        // update to false when get string
                        CaddieInfo.CaddieNeedUpdate = false;
                    }
                }
                return SQLString.ToString();
            }
            finally
            {
                SQLString = null;
            }
        }
    }
}
