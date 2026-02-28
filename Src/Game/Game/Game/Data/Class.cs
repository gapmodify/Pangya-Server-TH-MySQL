using Game.Defines;
using System;
using Game.Data;

namespace Game.Game.Data
{
    public class GameInformation
    {
        public byte Unknown1;
        public UInt32 VSTime;
        public UInt32 GameTime;
        public byte MaxPlayer;
        public GAME_TYPE GameType;
        public byte HoleTotal;
        public byte Map;
        public byte Mode;
        // Natural
        public UInt32 NaturalMode;
        public bool GMEvent;
        // Hole Repeater
        public byte HoleNumber;
        public UInt32 LockHole;

        // Game Data
        public string Name;
        public string Password;
        public UInt32 Artifact;
        // Grandprix
        public bool GP;
        public UInt32 GPTypeID;
        public UInt32 GPTypeIDA;
        public UInt32 GPTime;
        public DateTime GPStart;
        public byte Time30S = 0x30;
    }

    public class GameHoleInfo
    {
        public byte Hole;
        public byte Weather;
        public ushort WindPower;
        public ushort WindDirection;
        public byte Map;
        public byte Pos;
        
        // ✅ Tee และ Pin positions สำหรับคำนวณระยะเริ่มต้น
        public Point3D TeePos;
        public Point3D PinPos;
        
        /// <summary>
        /// คำนวณระยะจาก Tee ถึง Pin (เมตร)
        /// </summary>
        public float GetInitialDistance()
        {
            if (TeePos.X == 0 && TeePos.Z == 0 && PinPos.X == 0 && PinPos.Z == 0)
            {
                return 99999999f; // ถ้ายังไม่มีข้อมูล ใช้ค่า infinity
            }
            return TeePos.HoleDistance(PinPos);
        }
    }
}
