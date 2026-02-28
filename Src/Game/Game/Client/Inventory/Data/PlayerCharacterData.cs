using Connector.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client.Inventory.Data.Character
{
    public class PlayerCharacterData
    {
        public UInt32 TypeID { get; set; }
        public UInt32 Index { get; set; }
        public UInt16 HairColour { get; set; }
        public UInt16 GiftFlag { get; set; }
        public UInt32[] EquipTypeID { get; set; } = new UInt32[24];
        public UInt32[] EquipIndex { get; set; } = new UInt32[24];
        public UInt32 AuxPart { get; set; }
        public UInt32 AuxPart2 { get; set; }
        public uint AuxPart3 { get; set; }
        public uint AuxPart4 { get; set; }
        public uint AuxPart5 { get; set; }
        public UInt32 FCutinIndex { get; set; }
        public Byte Power { get; set; }
        public Byte Control { get; set; }
        public Byte Impact { get; set; }
        public Byte Spin { get; set; }
        public Byte Curve { get; set; }
        public Byte MasteryPoint { get; set; }
        public bool NEEDUPDATE { get; set; }

        public bool UpgradeSlot(Byte Slot)
        {
            switch (Slot)
            {
                case 0:
                    this.Power += 1;
                    break;
                case 1:
                    this.Control += 1;
                    break;
                case 2:
                    this.Impact += 1;
                    break;
                case 3:
                    this.Spin += 1;
                    break;
                case 4:
                    this.Curve += 1;
                    break;
                default:
                    return false;
            }
            this.NEEDUPDATE = true;
            return true;
        }

        public bool DowngradeSlot(Byte Slot)
        {
            switch (Slot)
            {
                case 0:
                    if ((this.Power <= 0))
                    {
                        return false;
                    }
                    this.Power -= 1;
                    break;
                case 1:
                    if ((this.Control <= 0))
                    {
                        return false;
                    }
                    this.Control -= 1;
                    break;
                case 2:
                    if ((this.Impact <= 0))
                    {
                        return false;
                    }
                    this.Impact -= 1;
                    break;
                case 3:
                    if ((this.Spin <= 0))
                    {
                        return false;
                    }
                    this.Spin -= 1;
                    break;
                case 4:
                    if ((this.Curve <= 0))
                    {
                        return false;
                    }
                    this.Curve -= 1;
                    break;
            }
            this.NEEDUPDATE = true;
            return true;
        }

        public uint GetPangUpgrade(byte Slot)
        {
            const uint POWPANG = 2100, CONPANG = 1700, IMPPANG = 2400, SPINPANG = 1900, CURVPANG = 1900;

            switch (Slot)
            {
                case 0:
                    return ((this.Power * POWPANG) + POWPANG);
                case 1:
                    return ((this.Control * CONPANG) + CONPANG);
                case 2:
                    return ((this.Impact * IMPPANG) + IMPPANG);
                case 3:
                    return ((this.Spin * SPINPANG) + SPINPANG);
                case 4:
                    return ((this.Curve * CURVPANG) + CURVPANG);
            }
            return 0;
        }

        public void Update(PlayerCharacterData info)
        {
            HairColour = info.HairColour;
            Power = info.Power;
            Control = info.Control;
            Impact = info.Impact;
            Spin = info.Spin;
            Curve = info.Curve;
            FCutinIndex = info.FCutinIndex;
            EquipTypeID = info.EquipTypeID;
            EquipIndex = info.EquipIndex;
            AuxPart = info.AuxPart;
            AuxPart2 = info.AuxPart2;
            NEEDUPDATE = true;
        }

        public void SaveChar(uint UID)
        {
            if (!NEEDUPDATE)
                return;

            try
            {
                using (var _db = DbContextFactory.Create())
                {
                    // ใช้ SQL Query ปกติแทน Stored Procedure (รองรับทั้ง MySQL และ SQL Server)
                    var sql = @"
                        UPDATE pangya_character 
                        SET POWER_SLOT = @p0, 
                            CONTROL_SLOT = @p1, 
                            ACCURACY_SLOT = @p2, 
                            SPIN_SLOT = @p3, 
                            CURVE_SLOT = @p4,
                            CUT_IN_ID = @p5,
                            HAIR_COLOR = @p6,
                            Parts_TypeID_1 = @p7,
                            Parts_TypeID_2 = @p8
                        WHERE UID = @p9 AND C_IDX = @p10";

                    int rowsAffected = _db.Database.ExecuteSqlCommand(sql,
                        Power,           // @p0
                        Control,         // @p1
                        Impact,          // @p2
                        Spin,            // @p3
                        Curve,           // @p4
                        (int)FCutinIndex,// @p5
                        (int)HairColour, // @p6
                        (int)AuxPart,    // @p7
                        (int)AuxPart2,   // @p8
                        (int)UID,        // @p9
                        (int)Index       // @p10
                    );

                    if (rowsAffected > 0)
                    {
                        NEEDUPDATE = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SAVE_CHAR_ERROR]: Failed to save character stats for UID:{UID}, Index:{Index}");
                Console.WriteLine($"[SAVE_CHAR_ERROR]: {ex.Message}");
                throw;
            }
        }

        internal string GetStringCharInfo()
        {
            StringBuilder SQLString;
            SQLString = new StringBuilder();
            try
            {
                SQLString.Append('^');
                SQLString.Append(Index);
                for (int i = 0; i <= 23; i++)
                {
                    SQLString.Append('^');
                    SQLString.Append(EquipTypeID[i]);
                }
                for (int i = 0; i <= 23; i++)
                {
                    SQLString.Append('^');
                    SQLString.Append(EquipIndex[i]);
                }
                SQLString.Append(',');

                return SQLString.ToString();
            }
            finally
            {
                SQLString = null;
            }
        }
    }
}
