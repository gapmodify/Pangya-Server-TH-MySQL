using PangyaAPI.BinaryModels;
using Connector.DataBase;
using Connector.Table;
using Game.Defines;
using Game.Client.Inventory.Data.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Game.GameTools.Tools;

namespace Game.Client.Inventory.Collection
{
    public class WarehouseCollection : List<PlayerItemData>
    {
        public WarehouseCollection(int PlayerUID)
        {
            Build(PlayerUID);
        }

        void Build(int UID)
        {
            using (var dbWarehouse = new DB_pangya_warehouse())
            using (var dbClubInfo = new DB_pangya_club_info())
            {
                // ✅ Load all warehouse items for this user
                var warehouseItems = dbWarehouse.SelectByUID(UID);
                
                foreach (var warehouseData in warehouseItems)
                {
                    // ✅ Create WarehouseQueryResult from WarehouseData
                    var queryResult = new WarehouseQueryResult
                    {
                        item_id = warehouseData.item_id,
                        UID = warehouseData.UID,
                        TYPEID = warehouseData.TYPEID,
                        C0 = (int)(warehouseData.C0 ?? 0),
                        C1 = (int)(warehouseData.C1 ?? 0),
                        C2 = (int)(warehouseData.C2 ?? 0),
                        C3 = (int)(warehouseData.C3 ?? 0),
                        C4 = (int)(warehouseData.C4 ?? 0),
                        RegDate = warehouseData.RegDate ?? DateTime.Now,
                        End_Date = warehouseData.DateEnd ?? DateTime.Now.AddYears(10),
                        VALID = warehouseData.VALID,
                        Flag = warehouseData.Flag,
                        HOURLEFT = 0, // Safe default
                        C0_SLOT = 0,
                        C1_SLOT = 0,
                        C2_SLOT = 0,
                        C3_SLOT = 0,
                        C4_SLOT = 0,
                        CLUB_SLOT_CANCEL = 0,
                        CLUB_WORK_COUNT = 0,
                        CLUB_POINT = 100000,
                        CLUB_POINT_TOTAL_LOG = 0,
                        CLUB_UPGRADE_PANG_LOG = 0,
                        UCC_UNIQUE = "",
                        UCC_STAT = 0,
                        UCC_COIDX = 0,
                        UCC_DRAWER_UID = 0
                    };
                    
                    // ✅ If it's a club item, load club info
                    if (GetItemGroup((uint)warehouseData.TYPEID) == 4) // ITEM_TYPE_CLUB
                    {
                        var clubInfo = dbClubInfo.SelectByItemID(warehouseData.item_id, UID);
                        if (clubInfo != null)
                        {
                            queryResult.C0_SLOT = clubInfo.C0_SLOT;
                            queryResult.C1_SLOT = clubInfo.C1_SLOT;
                            queryResult.C2_SLOT = clubInfo.C2_SLOT;
                            queryResult.C3_SLOT = clubInfo.C3_SLOT;
                            queryResult.C4_SLOT = clubInfo.C4_SLOT;
                            queryResult.CLUB_WORK_COUNT = clubInfo.CLUB_WORK_COUNT;
                            queryResult.CLUB_POINT = clubInfo.CLUB_POINT;
                            queryResult.CLUB_POINT_TOTAL_LOG = clubInfo.CLUB_POINT_TOTAL_LOG;
                            queryResult.CLUB_UPGRADE_PANG_LOG = clubInfo.CLUB_UPGRADE_PANG_LOG;
                            queryResult.CLUB_SLOT_CANCEL = clubInfo.CLUB_SLOT_CANCEL;
                        }
                    }
                    
                    // ✅ Create PlayerItemData and add to collection
                    var item = new PlayerItemData(queryResult);
                    Add(item);
                }
            }
        }

        public byte[] Build()
        {
            using (var result = new PangyaBinaryWriter())
            {
                result.Write(new byte[] { 0x73, 0x00 });
                result.WriteUInt16(Convert.ToUInt16(Count));
                result.WriteUInt16(Convert.ToUInt16(Count));
                foreach (var item in this)
                {
                    result.Write(item.GetItems());
                }
                return result.GetBytes();
            }
        }
        public int ItemAdd(PlayerItemData Value)
        {
            Value.ItemNeedUpdate = false;
            Add(Value);
            return Count;
        }


        public PlayerItemData GetUCC(uint ItemIdx)
        {
            var item = this.Where(c => c.ItemIndex == ItemIdx && c.ItemUCCUnique.Length >= 8);
            if (item.Any()) { return item.FirstOrDefault(); }

            return null;
        }

        public PlayerItemData GetUCC(uint TypeID, string UCCUnique, bool Status)
        {
            var item = this.Where(c => c.ItemTypeID == TypeID && c.ItemUCCUnique == UCCUnique && c.ItemUCCStatus == 1);
            if (item.Any()) { return item.FirstOrDefault(); }

            return null;
        }
        public PlayerItemData GetUCC(uint TypeID, string UCCUnique)
        {
            var item = this.Where(c => c.ItemTypeID == TypeID && c.ItemUCCUnique == UCCUnique && c.ItemUCCStatus >= 0);
            if (item.Any()) { return item.FirstOrDefault(); }

            return null;
        }
        public PlayerItemData GetItem(uint Index)
        {
            foreach (var Items in this)
            {
                if ((Items.ItemIndex == Index) && (Items.ItemIsValid == 1))
                {
                    return Items;
                }
            }
            return null;
        }
        public PlayerItemData GetItem(uint ID, TGET_ITEM type)
        {
            switch (type)
            {
                case TGET_ITEM.gcTypeID:
                    {
                        var Item = this.Where(c => c.ItemTypeID == ID);

                        if (Item.Any())
                        {
                            return Item.FirstOrDefault();
                        }
                        else
                        {
                            return null;
                        }
                    }
                case TGET_ITEM.gcIndex:
                    {
                        var Item = this.Where(c => c.ItemTypeID == ID);

                        if (Item.Any())
                        {
                            return Item.FirstOrDefault();
                        }
                        else
                        {
                            return null;
                        }
                    }
            }

            return null;
        }

        public PlayerItemData GetItem(uint TypeID, uint Quantity)
        {
            foreach (var Items in this)
            {
                if (Items.ItemTypeID == TypeID && Items.ItemC0 >= Quantity && Items.ItemIsValid == 1)
                {
                    return Items;
                }
            }
            return null;
        }

        public PlayerItemData GetItem(uint TypeID, uint Index, uint Quantity)
        {
            foreach (var Items in this)
            {
                if (Items.ItemTypeID == TypeID && (Items.ItemIndex == Index) && Items.ItemC0 >= Quantity && Items.ItemIsValid == 1)
                {
                    return Items;
                }
            }
            return null;
        }


        public PlayerItemData GetClub(uint ID, TGET_CLUB type)
        {
            switch (type)
            {
                case TGET_CLUB.gcTypeID:
                    {
                        var ClubInfo = this.Where(c => c.ItemTypeID == ID && c.ItemGroup == (byte)TITEMGROUP.ITEM_TYPE_CLUB);

                        if (ClubInfo.Any())
                        {
                            return ClubInfo.FirstOrDefault();
                        }
                        else
                        {
                            return null;
                        }
                    }
                case TGET_CLUB.gcIndex:
                    {
                        var ClubInfo = this.Where(c => c.ItemIndex == ID && c.ItemGroup == (byte)TITEMGROUP.ITEM_TYPE_CLUB);

                        if (ClubInfo.Any())
                        {
                            return ClubInfo.FirstOrDefault();
                        }
                        else
                        {
                            return null;
                        }
                    }
            }

            return null;
        }


        public PlayerItemData GetItem(uint ID, TGET_CLUB type, uint Quantity = 0)
        {
            switch (type)
            {
                case TGET_CLUB.gcTypeID:
                    {
                        var Item = this.Where(c => c.ItemTypeID == ID && c.ItemC0 >= Quantity && c.ItemIsValid == 1);

                        if (Item.Any())
                        {
                            return Item.FirstOrDefault();
                        }
                    }
                    break;
                case TGET_CLUB.gcIndex:
                    {
                        var Item = this.Where(c => c.ItemIndex == ID && c.ItemC0 >= Quantity && c.ItemIsValid == 1);

                        if (Item.Any())
                        {
                            return Item.FirstOrDefault();
                        }
                        break;
                    }
            }

            return null;
        }


        public bool IsSkinExist(uint typeID)
        {
            var Items = this.FirstOrDefault(c => c.ItemTypeID == typeID && c.ItemIsValid == 1);


            if (Items != null && GetItemGroup(Items.ItemTypeID) == 14)
            {
                return true;
            }
            return false;
        }

        public bool IsClubExist(uint typeID)
        {
            var Items = this.FirstOrDefault(c => c.ItemTypeID == typeID && c.ItemIsValid == 1);
            if (Items != null && GetItemGroup(Items.ItemTypeID) == 4)
            {
                return true;
            }
            return false;
        }

        public bool IsNormalExist(uint typeID)
        {
            var Items = this.Where(c => c.ItemTypeID == typeID && c.ItemC0 > 0 && c.ItemIsValid == 1);
            return Items.Any();
        }
        public bool IsNormalExist(uint typeID, uint index, uint Quantity)
        {
            var Items = this.Where(c => c.ItemTypeID == typeID && c.ItemIndex == index && c.ItemC0 >= Quantity && c.ItemIsValid == 1);
            return Items.Any();
        }

        public bool IsPartExist(uint typeID)
        {
            var Items = this.Where(c => c.ItemTypeID == typeID && c.ItemIsValid == 1);

            return Items.Any();
        }

        public bool IsHairStyleExist(int typeID)
        {
            var Items = this.Where(c => c.ItemTypeID == typeID && c.ItemIsValid == 1);

            return Items.Any();
        }

        public bool IsPartExist(uint typeID, uint index, uint Quantity)
        {
            var Items = this.Where(c => c.ItemTypeID == typeID && c.ItemIndex == index && c.ItemIsValid == 1);

            return Items.Any();
        }

        public uint GetQuantity(uint TypeID)
        {
            var Items = this.FirstOrDefault(c => c.ItemTypeID == TypeID);

            if (Items != null)
            {
                return Items.ItemC0;
            }
            else
            {
                return 0;
            }
        }

        public string GetSqlUpdateItems()
        {
            StringBuilder SQLString;
            SQLString = new StringBuilder();

            foreach (var Items in this)
            {
                if (Items.ItemNeedUpdate)
                {
                    SQLString.Append(Items.GetSqlUpdateString());
                    Items.ItemNeedUpdate = false;
                }
            }
            return SQLString.ToString();
        }

      
        public List<PlayerItemData> GetClubData()
        {
            return this.Where(c => c.ItemGroup == (byte)TITEMGROUP.ITEM_TYPE_CLUB && c.ItemNeedUpdate == true).ToList();
        }

        internal bool RemoveItem(uint TypeId, uint Count)
        {
            switch (GetItemGroup(TypeId))
            {
                case 5:
                case 6:
                    {
                        foreach (var Items in this)
                        {
                            if (Items.ItemTypeID == TypeId && Items.ItemC0 >= Count && Items.ItemIsValid == 1)
                            {
                                Items.ItemC0 -= (ushort)Count;

                                if (Items.ItemC0 == 0)
                                {
                                    Items.ItemIsValid = 0;
                                }
                            }
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }
        public void Update(PlayerItemData Item)
        {
            foreach (var upgrade in this)
            {
                if (upgrade.ItemIndex == Item.ItemIndex && upgrade.ItemTypeID == Item.ItemTypeID)
                {
                    upgrade.Update(Item);
                }
            }
        }
        internal bool RemoveItem(PlayerItemData Item)
        {
            return this.Remove(Item);
        }
    }
}
