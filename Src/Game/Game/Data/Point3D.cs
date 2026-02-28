using PangyaAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Game.Data
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Point3D
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        // ✅ Constant สำหรับ conversion factor (Pangya Units → Meters)
        private const float PANGYA_TO_METERS = 0.312495f;

        public static Point3D operator -(Point3D PosA, Point3D PosB)
        {
            Point3D result = new Point3D()
            {
                X = PosA.X - PosB.X,
                Y = PosA.Y - PosB.Y,
                Z = PosA.Z - PosB.Z,
            };
            return result;
        }
        public static Point3D operator +(Point3D PosA, Point3D PosB)
        {
            Point3D result = new Point3D()
            {
                X = PosA.X + PosB.X,
                Y = PosA.Y + PosB.Y,
                Z = PosA.Z + PosB.Z,
            };
            return result;
        }
        public float Distance(Point3D PlayerPos)
        {
            return (this - PlayerPos).Length();
        }

        public float Length()
        {
            return Convert.ToSingle(Math.Sqrt(X * X + Y * Y));
        }

        /// <summary>
        /// คำนวณระยะ 2D (X, Z) ระหว่าง 2 จุด และแปลงหน่วยเป็นเมตร
        /// สูตร: Distance = sqrt((X1-X2)^2 + (Z1-Z2)^2) * PANGYA_TO_METERS
        /// </summary>
        /// <param name="PosB">ตำแหน่งปลายทาง (เช่น Pin/Hole)</param>
        /// <returns>ระยะทางเป็นเมตร</returns>
        public float HoleDistance(Point3D PosB)
        {
            // ✅ Optimized: ใช้การคูณแทน Math.Pow() และคืนค่าเป็น float โดยตรง
            float dx = X - PosB.X;
            float dz = Z - PosB.Z;
            return (float)(Math.Sqrt(dx * dx + dz * dz) * PANGYA_TO_METERS);
        }
    }
}
