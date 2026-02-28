using PangyaAPI.BinaryModels;
using PangyaAPI.Tools;
using Connector.DataBase;
using Game.Client.Inventory.Data.CardEquip;
using Game.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Client.Inventory.Collection
{
    public class CardEquipQueryResult
    {
        public int ID { get; set; }
        public int CID { get; set; }
        public int CHAR_TYPEID { get; set; }
        public int CARD_TYPEID { get; set; }
        public int SLOT { get; set; }
        public int FLAG { get; set; }
        public DateTime? REGDATE { get; set; }  // ✅ Nullable
        public DateTime? ENDDATE { get; set; }  // ✅ Nullable
    }

    public class CardEquipAddResult
    {
        public int OUT_INDEX { get; set; }
        public int UID { get; set; }
        public int CID { get; set; }
        public int CHARTYPEID { get; set; }
        public int CARDTYPEID { get; set; }
        public int SLOT { get; set; }
        public int FLAG { get; set; }
        public DateTime? REGDATE { get; set; }  // ✅ Nullable
        public DateTime? ENDDATE { get; set; }  // ✅ Nullable
        public int CODE { get; set; }
    }

    public class CardEquipCollection : List<PlayerCardEquipData>
    {
        public CardEquipCollection(int PlayerUID)
        {
            Build(PlayerUID);
        }
        void Build(int UID)
        {
            using (var _db = DbContextFactory.Create())
            {
                var cardEquips = _db.Database.SqlQuery<CardEquipQueryResult>(
                    @"SELECT ID, CID, CHAR_TYPEID, CARD_TYPEID, SLOT, FLAG, REGDATE, ENDDATE
                      FROM pangya_card_equip 
                      WHERE UID = @p0", UID).ToList();

                foreach (var info in cardEquips)
                {
                    // ✅ ตรวจสอบว่าการ์ดหมดอายุหรือไม่
                    DateTime endDate = info.ENDDATE ?? DateTime.MaxValue;
                    bool isExpired = (info.FLAG == 1 || info.FLAG == 2) && endDate < DateTime.Now;
                    
                    if (isExpired)
                    {
                        // การ์ดหมดอายุแล้ว - ข้ามไม่โหลด
                        WriteConsole.WriteLine($"[CARD_EQUIP_BUILD] ⚠️ Skipping expired card - ID:{info.ID} CardType:0x{info.CARD_TYPEID:X} EndDate:{endDate}", ConsoleColor.Yellow);
                        continue;
                    }
                    
                    var cardequip = new PlayerCardEquipData()
                    {
                        ID = (uint)info.ID,
                        CID = (uint)info.CID,
                        CHAR_TYPEID = (uint)info.CHAR_TYPEID,
                        CARD_TYPEID = (uint)info.CARD_TYPEID,
                        SLOT = (byte)info.SLOT,
                        FLAG = (byte)info.FLAG,
                        REGDATE = info.REGDATE ?? DateTime.Now,
                        ENDDATE = endDate,
                        VALID = 1,
                        NEEDUPDATE = false
                    };

                    AddCard(cardequip);
                }
            }
        }

        public void AddCard(PlayerCardEquipData P)
        {
            this.Add(P);
        }

        public byte[] Build()
        {
            PangyaBinaryWriter result;
            result = new PangyaBinaryWriter();

            result.Write(new byte[] { 0x37, 0x01 });
            result.WriteUInt16((ushort)this.Count);
            foreach (PlayerCardEquipData C in this)
            {
                result.Write(C.CardEquipInfo());
            }
            return result.GetBytes();
        }

        // CHARACTER CARD
        public PlayerCardEquipData GetCard(UInt32 CID, UInt32 SLOT)
        {
            foreach (PlayerCardEquipData result in this)
            {
                if (result.CheckCard(CID, SLOT))
                {
                    return result;
                }
            }
            return null;
        }



        public Dictionary<bool, PlayerCardEquipData> UpdateCard(UInt32 UID, UInt32 CID, UInt32 CHARTYPEID, UInt32 CARDTYPEID, byte SLOT, byte FLAG, byte TIME)
        {
            PlayerCardEquipData UP;
            UP = null;
            foreach (PlayerCardEquipData P in this)
            {
                switch (FLAG)
                {
                    case 0:
                        if ((P.CID == CID) && (P.CHAR_TYPEID == CHARTYPEID) && (P.SLOT == SLOT) && (P.FLAG == 0) && (P.VALID == 1))
                        {
                            UP = P;
                            break;
                        }
                        break;
                    case 1:
                        if ((P.CID == CID) && (P.CARD_TYPEID == CARDTYPEID) && (P.SLOT == SLOT) && (P.FLAG == 1) && (P.ENDDATE > DateTime.Now) && (P.VALID == 1))
                        {
                            UP = P;
                            break;
                        }
                        break;
                }
            }
            if (UP == null)
            {
                try
                {
                    using (var _db = DbContextFactory.Create())
                    {
                        // Use raw SQL for MySQL compatibility
                        var sql = @"
                            INSERT INTO pangya_card_equip (UID, CID, CHAR_TYPEID, CARD_TYPEID, SLOT, FLAG, REGDATE, ENDDATE)
                            VALUES (@p0, @p1, @p2, @p3, @p4, @p5, NOW(), @p6);
                            SELECT CAST(LAST_INSERT_ID() AS SIGNED) as OUT_INDEX, 
                                   CAST(@p0 AS SIGNED) as UID, 
                                   CAST(@p1 AS SIGNED) as CID, 
                                   CAST(@p2 AS SIGNED) as CHARTYPEID, 
                                   CAST(@p3 AS SIGNED) as CARDTYPEID, 
                                   CAST(@p4 AS SIGNED) as SLOT, 
                                   CAST(@p5 AS SIGNED) as FLAG, 
                                   NOW() as REGDATE, 
                                   @p6 as ENDDATE, 
                                   0 as CODE";
                        
                        // ✅ กำหนด ENDDATE ตามประเภทการ์ด:
                        // FLAG=0: Normal card (ถาวร) -> NULL
                        // FLAG=1: Temporary card (มีกำหนด) -> เพิ่ม TIME นาที
                        // FLAG=2: Special card (มีกำหนด) -> เพิ่ม TIME นาที
                        DateTime? endDate = null;
                        if (FLAG == 1 || FLAG == 2)
                        {
                            endDate = DateTime.Now.AddMinutes(TIME);
                            WriteConsole.WriteLine($"[CARD_EQUIP_ADD] ⏰ Temporary card - EndDate: {endDate}", ConsoleColor.Yellow);
                        }
                        else
                        {
                            WriteConsole.WriteLine($"[CARD_EQUIP_ADD] ♾️ Permanent card - No expiration", ConsoleColor.Green);
                        }
                        
                        var card = _db.Database.SqlQuery<CardEquipAddResult>(sql, 
                            (int)UID, (int)CID, (int)CHARTYPEID, (int)CARDTYPEID, SLOT, FLAG, endDate).FirstOrDefault();
                        
                        if (card == null || card.CODE != 0)
                        {
                            return new Dictionary<bool, PlayerCardEquipData>() { { false, null } };
                        }

                        this.Clear();
                        Build((int)UID);
                        
                        var addedCard = this.FirstOrDefault(c => c.CID == CID && c.SLOT == SLOT && c.CARD_TYPEID == CARDTYPEID);
                        if (addedCard != null)
                        {
                            WriteConsole.WriteLine($"[CARD_EQUIP_ADD] ✅ Card loaded from collection - ID:{addedCard.ID} CID:{addedCard.CID} Slot:{addedCard.SLOT}", ConsoleColor.Green);
                            return new Dictionary<bool, PlayerCardEquipData>() { { true, addedCard } };
                        }
                        
                        return new Dictionary<bool, PlayerCardEquipData>() { { true, new PlayerCardEquipData()
                        {
                            ID = (uint)card.OUT_INDEX,
                            CID = (uint)card.CID,
                            CHAR_TYPEID = (uint)card.CHARTYPEID,
                            CARD_TYPEID = (uint)card.CARDTYPEID,
                            SLOT = (byte)card.SLOT,
                            REGDATE = card.REGDATE ?? DateTime.Now,
                            ENDDATE = card.ENDDATE ?? DateTime.Now.AddYears(10),
                            FLAG = (byte)card.FLAG,
                            VALID = 1,
                            NEEDUPDATE = false
                        } } };
                    }
                }
                finally
                {
                }
            }
            else
            {
                UP.CARD_TYPEID = CARDTYPEID;
                UP.NEEDUPDATE = true;
                
                // ✅ อัพเดท ENDDATE เฉพาะการ์ดชั่วคราว
                if (FLAG == 1 || FLAG == 2)
                {
                    UP.ENDDATE = DateTime.Now.AddMinutes(TIME);
                    WriteConsole.WriteLine($"[CARD_EQUIP_UPDATE] ⏰ Updated card expiration - EndDate: {UP.ENDDATE}", ConsoleColor.Yellow);
                }
                else
                {
                    // การ์ดถาวร - ไม่ต้องตั้ง ENDDATE
                    UP.ENDDATE = DateTime.Now.AddYears(100);
                    WriteConsole.WriteLine($"[CARD_EQUIP_UPDATE] ♾️ Permanent card - No expiration change", ConsoleColor.Green);
                }
            }
            return new Dictionary<bool, PlayerCardEquipData>() { { true, UP } };
        }

        public byte[] MapCard(UInt32 CID)
        {
            TPCards TC;
            PangyaBinaryWriter Packet;

            TC = new TPCards();
            foreach (var PC in this)
            {
                if (PC.CID == CID)
                {
                    // Validate SLOT range to prevent IndexOutOfRangeException
                    if (PC.SLOT >= 1 && PC.SLOT <= 10)
                    {
                        TC.Card[PC.SLOT] = PC.CARD_TYPEID;
                    }
                }
            }
            Packet = new PangyaBinaryWriter();
            try
            {
                Packet.WriteUInt32(TC.Card[1]);
                Packet.WriteUInt32(TC.Card[2]);
                Packet.WriteUInt32(TC.Card[3]);
                Packet.WriteUInt32(TC.Card[4]);
                Packet.WriteUInt32(TC.Card[5]);
                Packet.WriteUInt32(TC.Card[6]);
                Packet.WriteUInt32(TC.Card[7]);
                Packet.WriteUInt32(TC.Card[8]);
                Packet.WriteUInt32(TC.Card[9]);
                Packet.WriteUInt32(TC.Card[10]);
                return Packet.GetBytes();
            }
            finally
            {
                Packet.Dispose();
            }
        }

        public string GetSqlUpdateCardEquip()
        {
            StringBuilder SQLString;
            SQLString = new StringBuilder();
            try
            {
                foreach (var P in this)
                {
                    if (P.NEEDUPDATE)
                    {
                        SQLString.Append(P.GetSqlUpdateString());

                        // ## set update to false when request string
                        P.NEEDUPDATE = false;
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
