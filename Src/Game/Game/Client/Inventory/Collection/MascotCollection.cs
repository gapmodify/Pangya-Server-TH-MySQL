using PangyaAPI.BinaryModels;
using Connector.DataBase;
using Game.Client.Inventory.Data.Mascot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Client.Inventory.Collection
{
    public class MascotQueryResult
    {
        public int MID { get; set; }
        public int MASCOT_TYPEID { get; set; }
        public string MESSAGE { get; set; }
        public DateTime DateEnd { get; set; }
        public int END_DATE_INT { get; set; }
    }

    public class MascotCollection : List<PlayerMascotData>
    {
        public MascotCollection(int PlayerUID)
        {
            Build(PlayerUID);
        }
        // SerialPlayerMascotData
        public int MascotAdd(PlayerMascotData Value)
        {
            Value.MascotNeedUpdate = false;
            Add(Value);
            return Count;
        }
        
        void Build(int UID)
        {
            using (var _db = DbContextFactory.Create())
            {
                var sql = @"
                    SELECT MID, MASCOT_TYPEID, MESSAGE, DateEnd, 
                           DATEDIFF(DateEnd, NOW()) as END_DATE_INT
                    FROM Pangya_Mascot
                    WHERE UID = @p0 AND DateEnd > NOW()";

                var mascots = _db.Database.SqlQuery<MascotQueryResult>(sql, UID).ToList();

                foreach (var info in mascots)
                {
                    var mascot = new PlayerMascotData()
                    {
                        MascotIndex = (uint)info.MID,
                        MascotTypeID = (uint)info.MASCOT_TYPEID,
                        MascotMessage = info.MESSAGE,
                        MascotEndDate = info.DateEnd,
                        MascotDayToEnd = (ushort)Math.Max(0, info.END_DATE_INT),
                        MascotIsValid = 1,
                        MascotNeedUpdate = false
                    };
                    this.MascotAdd(mascot);
                }
            }
        }
        public byte[] Build()
        {
            PangyaBinaryWriter Packet;

            using (Packet = new PangyaBinaryWriter())
            {
                Packet.Write(new byte[] { 0xE1, 0x00 });
                Packet.WriteByte((byte)Count);
                foreach (var Mascot in this)
                {
                    Packet.Write(Mascot.GetMascotInfo());
                }
                return Packet.GetBytes();
            }

        }
        public PlayerMascotData GetMascotByIndex(UInt32 MascotIndex)
        {
            foreach (PlayerMascotData Mascot in this)
            {
                if ((Mascot.MascotIndex == MascotIndex) && (Mascot.MascotEndDate > DateTime.MinValue))
                {
                    return Mascot;
                }
            }
            return null;
        }

        public PlayerMascotData GetMascotByTypeId(UInt32 MascotTypeId)
        {
            foreach (PlayerMascotData Mascot in this)
            {
                if ((Mascot.MascotTypeID == MascotTypeId) && (Mascot.MascotEndDate > DateTime.Now))
                {
                    return Mascot;
                }
            }
            return null;
        }

        public bool MascotExist(UInt32 TypeId)
        {
            foreach (PlayerMascotData Mascot in this)
            {
                if ((Mascot.MascotTypeID == TypeId) && (Mascot.MascotEndDate > DateTime.Now))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetSqlUpdateMascots()
        {

            StringBuilder SQLString;
            SQLString = new StringBuilder();
            try
            {
                foreach (var mascot in this)
                {
                    if (mascot.MascotNeedUpdate)
                    {
                        SQLString.Append(mascot.GetSqlUpdateString());
                        // ## set update to false when request string
                        mascot.MascotNeedUpdate = false;
                    }
                }
                return SQLString.ToString();
            }
            finally
            {

                SQLString.Clear();
            }
        }
    }
}
