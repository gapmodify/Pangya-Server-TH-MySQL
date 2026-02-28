using System;
using System.Linq;
using System.Text;
using System.Data.Entity;
using PangyaAPI.PangyaClient.Data;
using Connector.DataBase;
using Connector.Table;
using Game.Client.Inventory.Data.ItemDecoration;
using Game.Client.Inventory.Data.Slot;
using Game.Client.Inventory.Collection;
using PangyaAPI.Tools;

namespace Game.Client.Inventory
{
    public partial class PlayerInventory : InventoryAbstract
    {
        public ItemDecorationData ItemDecoration;
        public ItemSlotData ItemSlot { get; set; }
        public WarehouseCollection ItemWarehouse { get; set; }
        public CaddieCollection ItemCaddie { get; set; }
        public CharacterCollection ItemCharacter { get; set; }
        public MascotCollection ItemMascot { get; set; }
        public CardCollection ItemCard { get; set; }
        public CardEquipCollection ItemCardEquip { get; set; }
        public FurnitureCollection ItemRoom { get; set; }
        public TrophyCollection ItemTrophies { get; set; }
        public TrophySpecialCollection ItemTrophySpecial { get; set; }
        public TrophyGPCollection ItemTrophyGP { get; set; }
        public TransactionsCollection ItemTransaction { get; set; }
        public uint UID { get; set; }
        public uint CharacterIndex { get; set; }
        public uint CaddieIndex { get; set; }
        public uint MascotIndex { get; set; }
        public uint BallTypeID { get; set; }
        public uint ClubSetIndex { get; set; }
        public uint CutinIndex { get; set; }
        public uint TitleIndex { get; set; }
        public uint BackGroundIndex { get; set; }
        public uint FrameIndex { get; set; }
        public uint StickerIndex { get; set; }
        public uint SlotIndex { get; set; }
        public uint Poster1 { get; set; }
        public uint Poster2 { get; set; }
        public uint TranCount
        {
            get { return (uint)ItemTransaction.Count; }
        }

        public PlayerInventory(UInt32 TUID)
        {
            UID = TUID;
            ItemCardEquip = new CardEquipCollection((int)UID);
            ItemCharacter = new CharacterCollection((int)UID);
            ItemMascot = new MascotCollection((int)UID);
            ItemWarehouse = new WarehouseCollection((int)UID);
            ItemCaddie = new CaddieCollection((int)UID);
            ItemCard = new CardCollection((int)UID);
            ItemTransaction = new TransactionsCollection();
            ItemRoom = new FurnitureCollection((int)UID);
            ItemSlot = new ItemSlotData();
            ItemDecoration = new ItemDecorationData();
            ItemTrophies = new TrophyCollection();
            ItemTrophyGP = new TrophyGPCollection();
            ItemTrophySpecial = new TrophySpecialCollection();
            
            // Load equipment data using DB_pangya_user_equip
            using (var dbEquip = new DB_pangya_user_equip())
            {
                var equipData = dbEquip.SelectByUID((int)UID);
                
                if (equipData != null)
                {
                    // Create ItemSlotData with loaded values
                    var slotData = new ItemSlotData
                    {
                        Slot1 = (uint)equipData.ITEM_SLOT_1,
                        Slot2 = (uint)equipData.ITEM_SLOT_2,
                        Slot3 = (uint)equipData.ITEM_SLOT_3,
                        Slot4 = (uint)equipData.ITEM_SLOT_4,
                        Slot5 = (uint)equipData.ITEM_SLOT_5,
                        Slot6 = (uint)equipData.ITEM_SLOT_6,
                        Slot7 = (uint)equipData.ITEM_SLOT_7,
                        Slot8 = (uint)equipData.ITEM_SLOT_8,
                        Slot9 = (uint)equipData.ITEM_SLOT_9,
                        Slot10 = (uint)equipData.ITEM_SLOT_10
                    };
                    
                    ItemSlot.SetItemSlot(slotData);
                    
                    // Load decoration items from Skin_1-6
                    BackGroundIndex = (uint)equipData.Skin_1;
                    FrameIndex = (uint)equipData.Skin_2;
                    StickerIndex = (uint)equipData.Skin_3;
                    SlotIndex = (uint)equipData.Skin_4;
                    TitleIndex = (uint)equipData.Skin_5;
                    // Skin_6 is reserved/unused
                    
                    // Set equipped items
                    SetCharIndex((uint)equipData.CHARACTER_ID);
                    SetCaddieIndex((uint)equipData.CADDIE);
                    SetBallTypeID((uint)equipData.BALL_ID);
                    SetClubSetIndex((uint)equipData.CLUB_ID);
                    SetMascotIndex((uint)equipData.MASCOT_ID);
                    SetPoster((uint)equipData.POSTER_1, (uint)equipData.POSTER_2);
                }
            }

            ItemCharacter.Card = ItemCardEquip;
        }
    
        // PlayerSave - Use DB classes instead of raw SQL
        public void Save(DbContext _db)
        {
            try
            {
                WriteConsole.WriteLine($"[INVENTORY_SAVE] Starting save for UID:{UID}", ConsoleColor.Cyan);
                
                // 1. Save Toolbar (Equipment) using DB_pangya_user_equip
                using (var dbEquip = new DB_pangya_user_equip())
                {
                    var equipData = dbEquip.SelectByUID((int)UID);
                    
                    if (equipData != null)
                    {
                        // Update existing equipment
                        equipData.CHARACTER_ID = (int)CharacterIndex;
                        equipData.CADDIE = (int)CaddieIndex;
                        equipData.MASCOT_ID = (int)MascotIndex;
                        equipData.BALL_ID = (int)BallTypeID;
                        equipData.CLUB_ID = (int)ClubSetIndex;
                        equipData.ITEM_SLOT_1 = (int)ItemSlot.Slot1;
                        equipData.ITEM_SLOT_2 = (int)ItemSlot.Slot2;
                        equipData.ITEM_SLOT_3 = (int)ItemSlot.Slot3;
                        equipData.ITEM_SLOT_4 = (int)ItemSlot.Slot4;
                        equipData.ITEM_SLOT_5 = (int)ItemSlot.Slot5;
                        equipData.ITEM_SLOT_6 = (int)ItemSlot.Slot6;
                        equipData.ITEM_SLOT_7 = (int)ItemSlot.Slot7;
                        equipData.ITEM_SLOT_8 = (int)ItemSlot.Slot8;
                        equipData.ITEM_SLOT_9 = (int)ItemSlot.Slot9;
                        equipData.ITEM_SLOT_10 = (int)ItemSlot.Slot10;
                        equipData.Skin_1 = (int)BackGroundIndex;
                        equipData.Skin_2 = (int)FrameIndex;
                        equipData.Skin_3 = (int)StickerIndex;
                        equipData.Skin_4 = (int)SlotIndex;
                        equipData.Skin_5 = (int)TitleIndex;
                        equipData.Skin_6 = 0;
                        equipData.POSTER_1 = (int)Poster1;
                        equipData.POSTER_2 = (int)Poster2;
                        
                        dbEquip.Update(equipData);
                        WriteConsole.WriteLine($"[INVENTORY_SAVE] ✓ Equipment saved", ConsoleColor.Green);
                    }
                }
                
                // 2. Save Character Parts (เสื้อผ้า) using DB_pangya_character
                if (ItemCharacter != null && ItemCharacter.Count > 0)
                {
                    WriteConsole.WriteLine($"[INVENTORY_SAVE] Saving {ItemCharacter.Count} character(s) parts...", ConsoleColor.Yellow);
                    
                    using (var dbChar = new DB_pangya_character())
                    {
                        foreach (var character in ItemCharacter)
                        {
                            try
                            {
                                var charData = dbChar.SelectByCID((int)character.Index);
                                
                                if (charData != null)
                                {
                                    // Update character parts (24 slots)
                                    charData.PART_TYPEID_1 = (int?)character.EquipTypeID[0];
                                    charData.PART_TYPEID_2 = (int?)character.EquipTypeID[1];
                                    charData.PART_TYPEID_3 = (int?)character.EquipTypeID[2];
                                    charData.PART_TYPEID_4 = (int?)character.EquipTypeID[3];
                                    charData.PART_TYPEID_5 = (int?)character.EquipTypeID[4];
                                    charData.PART_TYPEID_6 = (int?)character.EquipTypeID[5];
                                    charData.PART_TYPEID_7 = (int?)character.EquipTypeID[6];
                                    charData.PART_TYPEID_8 = (int?)character.EquipTypeID[7];
                                    charData.PART_TYPEID_9 = (int?)character.EquipTypeID[8];
                                    charData.PART_TYPEID_10 = (int?)character.EquipTypeID[9];
                                    charData.PART_TYPEID_11 = (int?)character.EquipTypeID[10];
                                    charData.PART_TYPEID_12 = (int?)character.EquipTypeID[11];
                                    charData.PART_TYPEID_13 = (int?)character.EquipTypeID[12];
                                    charData.PART_TYPEID_14 = (int?)character.EquipTypeID[13];
                                    charData.PART_TYPEID_15 = (int?)character.EquipTypeID[14];
                                    charData.PART_TYPEID_16 = (int?)character.EquipTypeID[15];
                                    charData.PART_TYPEID_17 = (int?)character.EquipTypeID[16];
                                    charData.PART_TYPEID_18 = (int?)character.EquipTypeID[17];
                                    charData.PART_TYPEID_19 = (int?)character.EquipTypeID[18];
                                    charData.PART_TYPEID_20 = (int?)character.EquipTypeID[19];
                                    charData.PART_TYPEID_21 = (int?)character.EquipTypeID[20];
                                    charData.PART_TYPEID_22 = (int?)character.EquipTypeID[21];
                                    charData.PART_TYPEID_23 = (int?)character.EquipTypeID[22];
                                    charData.PART_TYPEID_24 = (int?)character.EquipTypeID[23];
                                    
                                    charData.PART_IDX_1 = (int?)character.EquipIndex[0];
                                    charData.PART_IDX_2 = (int?)character.EquipIndex[1];
                                    charData.PART_IDX_3 = (int?)character.EquipIndex[2];
                                    charData.PART_IDX_4 = (int?)character.EquipIndex[3];
                                    charData.PART_IDX_5 = (int?)character.EquipIndex[4];
                                    charData.PART_IDX_6 = (int?)character.EquipIndex[5];
                                    charData.PART_IDX_7 = (int?)character.EquipIndex[6];
                                    charData.PART_IDX_8 = (int?)character.EquipIndex[7];
                                    charData.PART_IDX_9 = (int?)character.EquipIndex[8];
                                    charData.PART_IDX_10 = (int?)character.EquipIndex[9];
                                    charData.PART_IDX_11 = (int?)character.EquipIndex[10];
                                    charData.PART_IDX_12 = (int?)character.EquipIndex[11];
                                    charData.PART_IDX_13 = (int?)character.EquipIndex[12];
                                    charData.PART_IDX_14 = (int?)character.EquipIndex[13];
                                    charData.PART_IDX_15 = (int?)character.EquipIndex[14];
                                    charData.PART_IDX_16 = (int?)character.EquipIndex[15];
                                    charData.PART_IDX_17 = (int?)character.EquipIndex[16];
                                    charData.PART_IDX_18 = (int?)character.EquipIndex[17];
                                    charData.PART_IDX_19 = (int?)character.EquipIndex[18];
                                    charData.PART_IDX_20 = (int?)character.EquipIndex[19];
                                    charData.PART_IDX_21 = (int?)character.EquipIndex[20];
                                    charData.PART_IDX_22 = (int?)character.EquipIndex[21];
                                    charData.PART_IDX_23 = (int?)character.EquipIndex[22];
                                    charData.PART_IDX_24 = (int?)character.EquipIndex[23];
                                    
                                    charData.AuxPart = (int?)character.AuxPart;
                                    charData.AuxPart2 = (int?)character.AuxPart2;
                                    charData.CUTIN = (int?)character.FCutinIndex;
                                    charData.POWER = character.Power;
                                    charData.CONTROL = character.Control;
                                    charData.IMPACT = character.Impact;
                                    charData.SPIN = character.Spin;
                                    charData.CURVE = character.Curve;
                                    
                                    dbChar.Update(charData);
                                    WriteConsole.WriteLine($"[INVENTORY_SAVE]   ✓ Saved character CID:{character.Index} (TypeID:{character.TypeID})", ConsoleColor.Green);
                                }
                            }
                            catch (Exception charEx)
                            {
                                WriteConsole.WriteLine($"[INVENTORY_SAVE]   ✗ Failed to save character CID:{character.Index}: {charEx.Message}", ConsoleColor.Red);
                            }
                        }
                    }
                }
                
                // 3. Save Warehouse Items using DB_pangya_warehouse
                if (ItemWarehouse != null && ItemWarehouse.Count > 0)
                {
                    WriteConsole.WriteLine($"[INVENTORY_SAVE] Checking {ItemWarehouse.Count} warehouse items for updates...", ConsoleColor.Yellow);
                    
                    int savedCount = 0;
                    int clubInfoCount = 0;
                    using (var dbWarehouse = new DB_pangya_warehouse())
                    using (var dbClubInfo = new DB_pangya_club_info())
                    {
                        foreach (var item in ItemWarehouse)
                        {
                            if (item.ItemNeedUpdate)
                            {
                                try
                                {
                                    var warehouseData = dbWarehouse.SelectByItemID((int)item.ItemIndex, (int)UID);
                                    
                                    if (warehouseData != null)
                                    {
                                        warehouseData.C0 = (byte)item.ItemC0;
                                        warehouseData.C1 = (byte)item.ItemC1;
                                        warehouseData.C2 = (byte)item.ItemC2;
                                        warehouseData.C3 = (byte)item.ItemC3;
                                        warehouseData.C4 = (byte)item.ItemC4;
                                        warehouseData.VALID = (byte)item.ItemIsValid;
                                        warehouseData.Flag = (byte)(item.ItemFlag ?? 0);
                                        
                                        dbWarehouse.Update(warehouseData);
                                        WriteConsole.WriteLine($"[INVENTORY_SAVE]   ✓ Warehouse item_id:{item.ItemIndex}, TypeID:0x{item.ItemTypeID:X}", ConsoleColor.Green);
                                        savedCount++;
                                        
                                        // ✅ ถ้าเป็น Club → บันทึก club_info ด้วย
                                        uint itemGroup = GameTools.Tools.GetItemGroup(item.ItemTypeID);
                                        if (itemGroup == 4) // ITEM_TYPE_CLUB
                                        {
                                            try
                                            {
                                                var clubInfo = dbClubInfo.SelectByItemID((int)item.ItemIndex, (int)UID);
                                                
                                                if (clubInfo != null)
                                                {
                                                    // ✅ Update existing club info
                                                    clubInfo.TYPEID = (int)item.ItemTypeID;
                                                    clubInfo.C0_SLOT = (short)item.ItemC0Slot;
                                                    clubInfo.C1_SLOT = (short)item.ItemC1Slot;
                                                    clubInfo.C2_SLOT = (short)item.ItemC2Slot;
                                                    clubInfo.C3_SLOT = (short)item.ItemC3Slot;
                                                    clubInfo.C4_SLOT = (short)item.ItemC4Slot;
                                                    clubInfo.CLUB_POINT = (int?)item.ItemClubPoint;
                                                    clubInfo.CLUB_WORK_COUNT = (int?)item.ItemClubWorkCount;
                                                    clubInfo.CLUB_SLOT_CANCEL = (int?)item.ItemClubSlotCancelledCount;
                                                    clubInfo.CLUB_POINT_TOTAL_LOG = (int?)item.ItemClubPointLog;
                                                    clubInfo.CLUB_UPGRADE_PANG_LOG = (int?)item.ItemClubPangLog;
                                                    
                                                    dbClubInfo.Update(clubInfo);
                                                    WriteConsole.WriteLine($"[INVENTORY_SAVE]     ✓ Club info updated (item_id:{item.ItemIndex}, TYPEID:0x{item.ItemTypeID:X}, Slots: P={item.ItemC0Slot},C={item.ItemC1Slot},I={item.ItemC2Slot})", ConsoleColor.Cyan);
                                                    clubInfoCount++;
                                                }
                                                else
                                                {
                                                    WriteConsole.WriteLine($"[INVENTORY_SAVE]     ⚠ Club info not found for item_id:{item.ItemIndex}, skipping", ConsoleColor.Yellow);
                                                }
                                            }
                                            catch (Exception clubEx)
                                            {
                                                WriteConsole.WriteLine($"[INVENTORY_SAVE]     ✗ Failed to save club info for item_id:{item.ItemIndex}: {clubEx.Message}", ConsoleColor.Red);
                                            }
                                        }
                                        
                                        item.ItemNeedUpdate = false;
                                    }
                                }
                                catch (Exception itemEx)
                                {
                                    WriteConsole.WriteLine($"[INVENTORY_SAVE]   ✗ Failed item_id:{item.ItemIndex}: {itemEx.Message}", ConsoleColor.Red);
                                }
                            }
                        }
                    }
                    
                    if (savedCount > 0)
                    {
                        WriteConsole.WriteLine($"[INVENTORY_SAVE] ✓ {savedCount} warehouse item(s) saved to DB", ConsoleColor.Green);
                    }
                    if (clubInfoCount > 0)
                    {
                        WriteConsole.WriteLine($"[INVENTORY_SAVE] ✓ {clubInfoCount} club info(s) saved to DB", ConsoleColor.Green);
                    }
                }
                
                WriteConsole.WriteLine($"[INVENTORY_SAVE] ✅ Save completed for UID:{UID}", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[INVENTORY_SAVE_ERROR] UID:{UID} - {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[INVENTORY_SAVE_ERROR] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
            }
        }

        public string GetSqlUpdateToolbar()
        {
            StringBuilder SQLString;
            SQLString = new StringBuilder();

            SQLString.Append('^');
            SQLString.Append(CharacterIndex);
            SQLString.Append('^');
            SQLString.Append(CaddieIndex);
            SQLString.Append('^');
            SQLString.Append(MascotIndex);
            SQLString.Append('^');
            SQLString.Append(BallTypeID);
            SQLString.Append('^');
            SQLString.Append(ClubSetIndex);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot1);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot2);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot3);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot4);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot5);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot6);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot7);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot8);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot9);
            SQLString.Append('^');
            SQLString.Append(ItemSlot.Slot10);
            SQLString.Append('^');
            SQLString.Append(ItemDecoration.BackGroundTypeID);
            SQLString.Append('^');
            SQLString.Append(ItemDecoration.FrameTypeID);
            SQLString.Append('^');
            SQLString.Append(ItemDecoration.StickerTypeID);
            SQLString.Append('^');
            SQLString.Append(ItemDecoration.SlotTypeID);
            SQLString.Append('^');
            SQLString.Append(ItemDecoration.UnknownTypeID);//is zero, for typeID unknown
            SQLString.Append('^');
            SQLString.Append(ItemDecoration.TitleTypeID);
            SQLString.Append(',');
            // close for next player
            return SQLString.ToString();
        }
    }
}