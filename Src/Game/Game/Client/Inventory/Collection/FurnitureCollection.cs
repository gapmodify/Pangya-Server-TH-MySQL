using PangyaAPI.BinaryModels;
using Connector.DataBase;
using Game.Client.Inventory.Data.Furniture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client.Inventory.Collection
{
    public class FurnitureQueryResult
    {
        public int IDX { get; set; }
        public int TYPEID { get; set; }
        public decimal POS_X { get; set; }
        public decimal POS_Y { get; set; }
        public decimal POS_Z { get; set; }
        public decimal POS_R { get; set; }
    }

    public class FurnitureCollection : List<PlayerFurnitureData>
    {
        public FurnitureCollection(int PlayerUID)
        {
            Build(PlayerUID);
        }
        public int FurnitureAdd(PlayerFurnitureData Value)
        {
            Value.Update = false;
            Add(Value);
            return Count;
        }

        void Build(int UID)
        {
            using (var _db = DbContextFactory.Create())
            {
                var sql = @"
                    SELECT IDX, TYPEID, POS_X, POS_Y, POS_Z, POS_R
                    FROM TD_ROOM_DATA
                    WHERE UID = @p0";

                var furnitures = _db.Database.SqlQuery<FurnitureQueryResult>(sql, UID).ToList();

                foreach (var info in furnitures)
                {
                    var Furniture = new PlayerFurnitureData()
                    {
                        Index = (uint)info.IDX,
                        TypeID = (uint)info.TYPEID,
                        PosX = (float)info.POS_X,
                        PosY = (float)info.POS_Y,
                        PosZ = (float)info.POS_Z,
                        PosR = (float)info.POS_R,
                        Valid = 1
                    };
                    Add(Furniture);
                }
            }
        }

        public byte[] GetItemInfo()
        {
            var Packet = new PangyaBinaryWriter();
            Packet.Write(new byte[] { 0x2D, 0x01 });
            Packet.WriteUInt32(1);
            Packet.WriteUInt16((ushort)Count);
            foreach (var Furniture in this)
            {
                Packet.Write(Furniture.GetBytes());
            }
            return Packet.GetBytes();
        }
    }
}
