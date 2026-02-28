using PangyaAPI.BinaryModels;
using Game.Defines;
using Connector.DataBase;
using Connector.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static PangyaFileCore.IffBaseManager;
using static Game.GameTools.TCompare;
using static System.Math;
using Game.Client.Inventory.Data;
using Game.Client.Inventory.Data.Warehouse;
using Game.Client.Inventory.Data.Character;
using Game.Client.Inventory.Data.Card;
using Game.Client.Inventory.Data.Caddie;
using Game.Client.Inventory.Data.Mascot;
using Game.Client.Inventory.Data.Transactions;
namespace Game.Client.Inventory
{
    public partial class PlayerInventory
    {
        #region Methods Array of Byte
        public override byte[] GetToolbar()
        {
            PangyaBinaryWriter Reply;

            Reply = new PangyaBinaryWriter();

            Reply.Write(new byte[] { 0x72, 0x00 });
            Reply.Write(GetEquipData());
            return Reply.GetBytes();
        }
        public override byte[] GetCharData()
        {
            return ItemCharacter.GetCharData(CharacterIndex);
        }
        public override byte[] GetCharData(uint Index)
        {

            return ItemCharacter.GetCharData(Index);
        }
        public override byte[] GetMascotData()
        {
            PlayerMascotData MascotInfo;
            MascotInfo = ItemMascot.GetMascotByIndex(MascotIndex);
            if ((MascotInfo != null))
            {
                return MascotInfo.GetMascotInfo();
            }
            return new byte[0x3E];
        }

        public override byte[] GetTrophyInfo()
        {
            return ItemTrophies.GetTrophy();
        }
        /// <summary>
        /// GetSize 116 bytes
        /// </summary>
        /// <returns></returns>
        public override byte[] GetEquipData()
        {
            var result = new PangyaBinaryWriter();

            result.WriteUInt32(CaddieIndex);
            result.WriteUInt32(CharacterIndex);
            result.WriteUInt32(ClubSetIndex);
            result.WriteUInt32(BallTypeID);//16
            result.Write(ItemSlot.GetItemSlot());//56
            result.WriteUInt32(BackGroundIndex);
            result.WriteUInt32(FrameIndex);
            result.WriteUInt32(StickerIndex);
            result.WriteUInt32(SlotIndex);
            result.WriteUInt32(0);//UNKNOWN, value = 0
            result.WriteUInt32(TitleIndex);
            result.WriteStruct(ItemDecoration);//104
            result.WriteUInt32(MascotIndex);
            result.WriteUInt32(Poster1);
            result.WriteUInt32(Poster2);//116
            return result.GetBytes();
        }
        /// <summary>
        /// GetCharacter(513 bytes), GetCaddie(25 bytes),ClubSet(28 bytes), Mascot(62 bytes), Total Size 634 
        /// </summary>
        /// <returns>Select(634 array of byte)</returns>
        public override byte[] GetEquipInfo()
        {
            var Response = new PangyaBinaryWriter();
            Response.Write(GetCharData());
            Response.Write(GetCaddieData());
            Response.Write(GetClubData());
            Response.Write(GetMascotData());
            return Response.GetBytes();
        }

        public override byte[] GetClubData()
        {
            PlayerItemData ClubInfo;
            ClubInfo = ItemWarehouse.GetItem(this.ClubSetIndex);
            if ((ClubInfo == null))
            {
                return new byte[28];
            }
            return ClubInfo.GetClubInfo();
        }

        public override byte[] GetCaddieData()
        {
            PlayerCaddieData CaddieInfo;
            CaddieInfo = ItemCaddie.GetCaddieByIndex(CaddieIndex);
            if (!(CaddieInfo == null))
            {
                return CaddieInfo.GetCaddieInfo();
            }
            return new byte[0x19];
        }
        // transaction
        public override byte[] GetTransaction()
        {
            return ItemTransaction.GetTran();
        }

        public override byte[] GetDecorationData()
        {
            using (var result = new PangyaBinaryWriter())
            {
                result.WriteStruct(ItemDecoration);
                return result.GetBytes();
            }
        }
        public override byte[] GetGolfEQP()
        {
            using (var Packet = new PangyaBinaryWriter())
            {
                Packet.WriteUInt32(BallTypeID);
                Packet.WriteUInt32(ClubSetIndex);
                return Packet.GetBytes();
            }
        }

        public byte[] GetCharacterCardUpdate(uint CharacterIndex)
        {
            using (var Packet = new PangyaBinaryWriter())
            {
                Packet.Write(new byte[] { 0x40, 0x00 });
                Packet.WriteUInt32(CharacterIndex);
                
                var cardMapData = ItemCharacter.Card.MapCard(CharacterIndex);
                if (cardMapData != null && cardMapData.Length > 0)
                {
                    Packet.Write(cardMapData);
                }
                else
                {
                    Packet.WriteZero(40);
                }
                
                return Packet.GetBytes();
            }
        }

        #endregion

        #region Methods Bool
        // poster


        public override bool SetCutInIndex(uint CharIndex, uint CutinIndex)
        {
            if (CutinIndex == 0)
            {
                return true;
            }
            var Item = ItemWarehouse.GetItem(CutinIndex, TGET_ITEM.gcIndex);
            var CharType = ItemCharacter.GetChar(CharIndex, Defines.CharType.bIndex);
            if (Item == null)
            {
                return false;
            }
            if (CharType == null)
            {
                return false;
            }
            CharType.FCutinIndex = Item.ItemIndex;
            ItemCharacter.UpdateCharacter(CharType);
            return true;
        }
        public override bool SetPoster(uint Poster1, uint Poster2)
        {
            this.Poster1 = Poster1;
            this.Poster2 = Poster2;
            return true;
        }

        public override bool IsExist(uint TypeID, uint Index, uint Quantity)
        {
            switch (GetPartGroup(TypeID))
            {
                case 5:
                case 6:
                    // ## normal and ball
                    return ItemWarehouse.IsNormalExist(TypeID, Index, Quantity);
                case 2:
                    // ## part
                    return ItemWarehouse.IsPartExist(TypeID, Index, Quantity);
                case 0x1:
                    // ## card
                    return ItemCard.IsExist(TypeID, Index, Quantity);
            }
            return false;
        }

        // item exists?
        public override bool IsExist(uint TypeId)
        {
            List<Dictionary<uint, uint>> ListSet;
            switch (GetPartGroup(TypeId))
            {
                case 2:
                    return ItemWarehouse.IsPartExist(TypeId);
                case 5:
                case 6:
                    return ItemWarehouse.IsNormalExist(TypeId);
                case 9:
                    ListSet = IffEntry.SetItem.SetList(TypeId);
                    try
                    {
                        if (ListSet.Count <= 0)
                        {
                            return false;
                        }
                        foreach (var __Enum in ListSet)
                        {
                            if (this.IsExist(__Enum.First().Key))
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                    finally
                    {
                        ListSet.Clear();
                    }
                case 14:
                    return ItemWarehouse.IsSkinExist(TypeId);
            }
            return false;
        }

        public override bool Available(uint TypeID, uint Quantity)
        {

            var ListSet = IffEntry.SetItem.SetList(TypeID);

            switch ((TITEMGROUP)GetPartGroup(TypeID))
            {
                case TITEMGROUP.ITEM_TYPE_SETITEM:
                    {
                        if (ListSet.Count <= 0)
                        { return false; }

                        else
                        {
                            foreach (var data in ListSet)
                            {
                                Available(data.Keys.FirstOrDefault(), data.Values.FirstOrDefault());
                            }
                            return true;
                        }
                    }
                case TITEMGROUP.ITEM_TYPE_CHARACTER:
                    {
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_HAIR_STYLE:
                    {
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_PART:
                    {
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_CLUB:
                    {
                        if (ItemWarehouse.IsClubExist(TypeID))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                case TITEMGROUP.ITEM_TYPE_AUX:
                case TITEMGROUP.ITEM_TYPE_BALL:
                case TITEMGROUP.ITEM_TYPE_USE:
                    {
                        if (GetQuantity(TypeID) + Quantity > 32767)
                        {
                            return false;
                        }
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_CADDIE:
                    {
                        if (ItemCaddie.IsExist(TypeID))
                        {
                            return false;
                        }
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_CADDIE_ITEM:
                    {
                        if (ItemCaddie.CanHaveSkin(TypeID))
                        {
                            return true;
                        }
                        return false;
                    }
                case TITEMGROUP.ITEM_TYPE_SKIN:
                    {
                        if (ItemWarehouse.IsSkinExist(TypeID))
                        {
                        }
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_MASCOT:
                    {
                        return true;
                    }
                case TITEMGROUP.ITEM_TYPE_CARD:
                    {
                        return true;
                    }

            }
            return false;
        }

        public override bool SetMascotText(uint MascotIdx, string MascotText)
        {
            PlayerMascotData Mascot;
            Mascot = ItemMascot.GetMascotByIndex(MascotIdx);
            if (!(Mascot == null))
            {
                Mascot.SetText(MascotText);
                return true;
            }
            return false;
        }
        // caddie system
        public override bool SetCaddieIndex(uint Index)
        {
            PlayerCaddieData Caddie;
            if (Index == 0)
            {
                CaddieIndex = 0;
                return true;
            }
            Caddie = ItemCaddie.GetCaddieByIndex(Index);
            if (Caddie == null)
            {
                return false;
            }
            CaddieIndex = Caddie.CaddieIdx;
            return true;
        }

        // mascot system
        public override bool SetMascotIndex(uint Index)
        {
            PlayerMascotData Mascot;
            if (Index == 0)
            {
                MascotIndex = 0;
                return true;
            }
            Mascot = ItemMascot.GetMascotByIndex(Index);
            if (Mascot == null)
            {
                return false;
            }
            MascotIndex = Mascot.MascotIndex;
            return true;
        }

        public override bool SetCharIndex(uint CharID)
        {
            PlayerCharacterData Char;
            Char = ItemCharacter.GetChar(CharID, CharType.bIndex);
            if (Char == null)
            {
                return false;
            }
            CharacterIndex = CharID;
            return true;
        }

        public override bool SetBackgroudIndex(uint typeID)
        {
            var Get = ItemWarehouse.GetItem(typeID, 1);
            if (Get == null)
            {
                return false;
            }
            ItemDecoration.BackGroundTypeID = typeID;
            BackGroundIndex = Get.ItemIndex;
            return true;
        }



        public override bool SetStickerIndex(uint typeID)
        {
            var Get = ItemWarehouse.GetItem(typeID, 1);
            if (Get == null)
            {
                return false;
            }
            ItemDecoration.StickerTypeID = typeID;
            StickerIndex = Get.ItemIndex;
            return true;
        }
        public override bool SetSlotIndex(uint typeID)
        {
            var Get = ItemWarehouse.GetItem(typeID, 1);
            if (Get == null)
            {
                return false;
            }
            ItemDecoration.SlotTypeID = typeID;
            SlotIndex = Get.ItemIndex;
            return true;
        }

        public override bool SetTitleIndex(uint ID)
        {
            var Get = ItemWarehouse.GetItem(ID);
            if (Get == null)
            {
                return false;
            }
            ItemDecoration.TitleTypeID = Get.ItemTypeID;
            TitleIndex = Get.ItemIndex;
            return true;
        }

        public override bool SetDecoration(uint background, uint frame, uint sticker, uint slot, uint un, uint title)
        {
            if (SetBackgroudIndex(background) || SetFrameIndex(frame) || SetStickerIndex(sticker) || SetSlotIndex(slot) || SetTitleIndex(title))
            {
                ItemDecoration.UnknownTypeID = un;
                return true;
            }
            return false;
        }

        // club system
        public override bool SetClubSetIndex(uint Index)
        {
            PlayerItemData Club;
            Club = ItemWarehouse.GetItem(Index);
            
            // ✅ ตรวจสอบว่าไม้นี้มีอยู่จริง และเป็น Club
            if (Club == null)
            {
                PangyaAPI.Tools.WriteConsole.WriteLine($"[SET_CLUB_INDEX] ✗ Failed - Club item_id:{Index} not found in warehouse! (UID:{UID})", ConsoleColor.Red);
                return false;
            }
            
            if (GetItemGroup(Club.ItemTypeID) != 0x4)
            {
                PangyaAPI.Tools.WriteConsole.WriteLine($"[SET_CLUB_INDEX] ✗ Failed - item_id:{Index} is not a club! TypeID:0x{Club.ItemTypeID:X}", ConsoleColor.Red);
                return false;
            }
            
            // ✅ ตรวจสอบซ้ำว่า item_id นี้เป็นของ player จริง (ป้องกัน exploit)
            if (Club.ItemIndex != Index)
            {
                PangyaAPI.Tools.WriteConsole.WriteLine($"[SET_CLUB_INDEX] ✗ Failed - Index mismatch! Requested:{Index}, Found:{Club.ItemIndex}", ConsoleColor.Red);
                return false;
            }
            
            this.ClubSetIndex = Index;
            PangyaAPI.Tools.WriteConsole.WriteLine($"[SET_CLUB_INDEX] ✓ Club changed - item_id:{Index}, TypeID:0x{Club.ItemTypeID:X} (UID:{UID})", ConsoleColor.Green);
            PangyaAPI.Tools.WriteConsole.WriteLine($"[SET_CLUB_INDEX]   Stats: P={Club.ItemC0}, C={Club.ItemC1}, I={Club.ItemC2}, S={Club.ItemC3}, Cv={Club.ItemC4}", ConsoleColor.Cyan);
            
            return true;
        }

        public override bool SetGolfEQP(uint BallTypeID, uint ClubSetIndex)
        {
            // ✅ ใช้ | (bitwise OR) แทน || เพื่อให้เรียกทั้ง 2 functions เสมอ
            // ถ้าใช้ || และ SetBallTypeID() return true จะไม่เรียก SetClubSetIndex()
            bool ballResult = this.SetBallTypeID(BallTypeID);
            bool clubResult = this.SetClubSetIndex(ClubSetIndex);
            return ballResult || clubResult;  // return true ถ้าอย่างใดอย่างหนึ่งสำเร็จ
        }

        public override bool SetBallTypeID(uint TypeID)
        {
            PlayerItemData Ball;
            Ball = ItemWarehouse.GetItem(TypeID, 1);
            if ((Ball == null) || (!(GetItemGroup(Ball.ItemTypeID) == 0x5)))
            {
                return false;
            }
            this.BallTypeID = TypeID;
            return true;
        }

        #endregion

        #region Methods UInt32

       

        public override uint GetTitleTypeID()
        {
            return ItemDecoration.TitleTypeID;
        }
        public override uint GetCharTypeID()
        {
            PlayerCharacterData CharInfo;
            CharInfo = ItemCharacter.GetChar(CharacterIndex, CharType.bIndex);
            if (!(CharInfo == null))
            {
                return CharInfo.TypeID;
            }
            return 0;
        }

        public override uint GetCutinIndex()
        {
            PlayerCharacterData CharInfo;
            CharInfo = ItemCharacter.GetChar(CharacterIndex, CharType.bIndex);
            if (!(CharInfo == null))
            {
                return CharInfo.FCutinIndex;
            }
            return 0;
        }


        public override uint GetMascotTypeID()
        {
            PlayerMascotData MascotInfo;
            MascotInfo = ItemMascot.GetMascotByIndex(MascotIndex);
            if (!(MascotInfo == null))
            {
                return MascotInfo.MascotTypeID;
            }
            return 0;
        }

        public override uint GetQuantity(uint TypeId)
        {
            switch (GetPartGroup(TypeId))
            {
                case 5:
                case 6:
                    // Ball And Normal
                    return ItemWarehouse.GetQuantity(TypeId);
                default:
                    return 0;
            }
        }

        uint GetPartGroup(uint TypeID)
        {
            uint result;
            result = (uint)Round((TypeID & 4227858432) / Pow(2.0, 26.0));
            return result;
        }


        public override bool SetFrameIndex(uint typeID)
        {
            var Get = ItemWarehouse.GetItem(typeID, 1);
            if (Get == null)
            {
                return false;
            }
            ItemDecoration.FrameTypeID = typeID;
            FrameIndex = Get.ItemIndex;
            return true;
        }
        #endregion

        #region Methods GetItem
        public PlayerItemData GetUCC(uint ItemIdx)
        {
            foreach (PlayerItemData ItemUCC in ItemWarehouse)
            {
                if ((ItemUCC.ItemIndex == ItemIdx) && (ItemUCC.ItemUCCUnique.Length >= 8))
                {
                    return ItemUCC;
                }
            }
            return null;
        }

        //THIS IS USE OR UCC THAT ALREADY PAINTED
        public PlayerItemData GetUCC(uint TypeId, string UCC_UNIQUE, byte Status = 1)
        {
            foreach (PlayerItemData ItemUCC in ItemWarehouse)
            {
                if ((ItemUCC.ItemTypeID == TypeId) && (ItemUCC.ItemUCCUnique == UCC_UNIQUE) && (ItemUCC.ItemUCCStatus == Status))
                {
                    return ItemUCC;
                }
            }
            return null;
        }

        //THIS IS USE OR UCC THAT ALREADY {NOT PAINTED}
        public PlayerItemData GetUCC(uint TypeID, string UCC_UNIQUE)
        {
            foreach (PlayerItemData ItemUCC in ItemWarehouse)
            {
                if ((ItemUCC.ItemTypeID == TypeID) && (ItemUCC.ItemUCCUnique == UCC_UNIQUE) && !(ItemUCC.ItemUCCStatus == 1))
                {
                    return ItemUCC;
                }
            }
            return null;
        }

        public PlayerCharacterData GetCharacter(uint TypeID)
        {
            PlayerCharacterData Character;
            Character = ItemCharacter.GetChar(TypeID, CharType.bTypeID);
            if (!(Character == null))
            {
                return Character;
            }
            return null;
        }
        #endregion

        #region Methods AddItems
        public AddData AddItem(AddItem ItemAddData)
        {
            Object TPlayerItemData;
            AddData Result;

            Result = new AddData() { Status = false };


            if (UID == 0)
            {
                return Result;
            }
            switch ((TITEMGROUP)GetPartGroup(ItemAddData.ItemIffId))
            {
                case TITEMGROUP.ITEM_TYPE_CHARACTER:
                    {
                        TPlayerItemData = ItemCharacter.GetChar(ItemAddData.ItemIffId, CharType.bTypeID);

                        if (TPlayerItemData == null)
                        {
                            // Character doesn't exist → Add to database
                            return AddItemToDB(ItemAddData);
                        }
                        else
                        {
                            // Character already exists → Return existing data
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM]: Character 0x{ItemAddData.ItemIffId:X} already exists, returning existing data", ConsoleColor.Yellow);
                            
                            Result.Status = true;
                            Result.ItemIndex = ((PlayerCharacterData)(TPlayerItemData)).Index;
                            Result.ItemTypeID = ((PlayerCharacterData)(TPlayerItemData)).TypeID;
                            Result.ItemOldQty = 1;
                            Result.ItemNewQty = 1;
                            Result.ItemUCCKey = string.Empty;
                            Result.ItemFlag = 0;
                            Result.ItemEndDate = null;

                            if (ItemAddData.Transaction)
                                ItemTransaction.AddChar(2, (PlayerCharacterData)TPlayerItemData);
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_HAIR_STYLE:
                    {
                        var IffHair = IffEntry.GetByHairColor(ItemAddData.ItemIffId);
                        var character = ItemCharacter.GetCharByType((byte)IffHair.CharType);
                        if (character != null)
                        {
                            character.HairColour = IffHair.HairColor;
                            character.Update(character);
                            Result.Status = true;
                            Result.ItemIndex = character.Index;
                            Result.ItemTypeID = ItemAddData.ItemIffId;
                            Result.ItemOldQty = 0;
                            Result.ItemNewQty = 1;
                            Result.ItemUCCKey = null;
                            Result.ItemFlag = 0;
                            Result.ItemEndDate = null;
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_PART:
                    {
                        return AddItemToDB(ItemAddData);
                    }
                case TITEMGROUP.ITEM_TYPE_CLUB:
                    {
                        return AddItemToDB(ItemAddData);
                    }
                case TITEMGROUP.ITEM_TYPE_AUX:
                case TITEMGROUP.ITEM_TYPE_BALL:
                case TITEMGROUP.ITEM_TYPE_USE:
                    {
                        PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM]: Checking if item 0x{ItemAddData.ItemIffId:X} exists in warehouse...", ConsoleColor.Cyan);
                        
                        TPlayerItemData = ItemWarehouse.GetItem(ItemAddData.ItemIffId, 1);
                        if (TPlayerItemData != null)
                        {
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM]: ✓ Item exists in warehouse (Index:{((PlayerItemData)TPlayerItemData).ItemIndex}), updating quantity...", ConsoleColor.Green);

                            Result.Status = true;
                            Result.ItemIndex = ((PlayerItemData)(TPlayerItemData)).ItemIndex;
                            Result.ItemTypeID = ((PlayerItemData)(TPlayerItemData)).ItemTypeID;
                            Result.ItemOldQty = ((PlayerItemData)(TPlayerItemData)).ItemC0;
                            Result.ItemNewQty = ((PlayerItemData)(TPlayerItemData)).ItemC0 + ItemAddData.Quantity;
                            Result.ItemUCCKey = ((PlayerItemData)(TPlayerItemData)).ItemUCCUnique;
                            Result.ItemFlag = (byte)((PlayerItemData)(TPlayerItemData)).ItemFlag;
                            Result.ItemEndDate = null;
                            
                            ((PlayerItemData)(TPlayerItemData)).AddQuantity(ItemAddData.Quantity);

                            if (ItemAddData.Transaction)
                            {
                                ItemTransaction.AddItem(0x02, (PlayerItemData)TPlayerItemData, ItemAddData.Quantity);
                            }
                            
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM]: ✓ Quantity updated: {Result.ItemOldQty} → {Result.ItemNewQty}", ConsoleColor.Green);
                        }
                        else
                        {
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM]: Item not found in warehouse, calling AddItemToDB()...", ConsoleColor.Yellow);
                            return AddItemToDB(ItemAddData);
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_CADDIE:
                    {
                        return AddItemToDB(ItemAddData);
                    }
                case TITEMGROUP.ITEM_TYPE_CADDIE_ITEM:
                    {
                        TPlayerItemData = ItemCaddie.GetCaddieBySkinId(ItemAddData.ItemIffId);

                        if (!(TPlayerItemData == null))
                        {
                            ((PlayerCaddieData)(TPlayerItemData)).Update();
                            ((PlayerCaddieData)(TPlayerItemData)).UpdateCaddieSkin(ItemAddData.ItemIffId, ItemAddData.Day);
                            Result.Status = true;
                            Result.ItemIndex = ((PlayerCaddieData)(TPlayerItemData)).CaddieIdx;
                            Result.ItemTypeID = ((PlayerCaddieData)(TPlayerItemData)).CaddieSkin;
                            Result.ItemOldQty = 1;
                            Result.ItemNewQty = 1;
                            Result.ItemUCCKey = string.Empty;
                            Result.ItemFlag = 0;
                            Result.ItemEndDate = DateTime.Now.AddDays(ItemAddData.Day);
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_SKIN:
                    {
                        return AddItemToDB(ItemAddData);
                    }
                case TITEMGROUP.ITEM_TYPE_MASCOT:
                    {
                        TPlayerItemData = ItemMascot.GetMascotByTypeId(ItemAddData.ItemIffId);

                        if (TPlayerItemData != null)
                        {
                            ((PlayerMascotData)(TPlayerItemData)).AddDay(ItemAddData.Day);
                            Result.Status = true;
                            Result.ItemIndex = ((PlayerMascotData)(TPlayerItemData)).MascotIndex;
                            Result.ItemTypeID = ((PlayerMascotData)(TPlayerItemData)).MascotTypeID;
                            Result.ItemOldQty = 1;
                            Result.ItemNewQty = 1;
                            Result.ItemUCCKey = "";
                            Result.ItemFlag = 0;
                            Result.ItemEndDate = ((PlayerMascotData)(TPlayerItemData)).MascotEndDate;
                        }
                        else if (TPlayerItemData == null)
                        {
                            return AddItemToDB(ItemAddData);
                        }
                    }
                    break;

                case TITEMGROUP.ITEM_TYPE_CARD:
                    {
                        TPlayerItemData = ItemCard.GetCard(ItemAddData.ItemIffId, 1);

                        if (TPlayerItemData == null)
                        {
                            return AddItemToDB(ItemAddData);
                        }
                        else if (TPlayerItemData != null)
                        {
                            Result.Status = true;
                            Result.ItemIndex = ((PlayerCardData)(TPlayerItemData)).CardIndex;
                            Result.ItemTypeID = ((PlayerCardData)(TPlayerItemData)).CardTypeID;
                            Result.ItemOldQty = ((PlayerCardData)(TPlayerItemData)).CardQuantity;
                            Result.ItemNewQty = ((PlayerCardData)(TPlayerItemData)).CardQuantity + ItemAddData.Quantity;
                            Result.ItemUCCKey = string.Empty;
                            Result.ItemFlag = 0;
                            Result.ItemEndDate = null;

                            ((PlayerCardData)(TPlayerItemData)).AddQuantity(ItemAddData.Quantity);

                            if (ItemAddData.Transaction)
                                ItemTransaction.AddCard(0x02, (PlayerCardData)TPlayerItemData, ItemAddData.Quantity);
                        }
                    }
                    break;
            }
            return Result;
        }

        public AddData AddRent(uint TypeID, ushort Day = 7)
        {
            object PRent;
            AddData Result;

            Result = new AddData() { Status = false };

            if (!(GetItemGroup(TypeID) == 2))
            {
                return Result;
            }
            
            using (var _db = DbContextFactory.Create())
            {
                // Note: ProcAddRent stored procedure not available in MySQL
                // Returning empty result for now
                return Result;
            }
        }

        public AddData AddItemToDB(AddItem ItemAddData)
        {
            Object TPlayerItemData;
            PlayerTransactionData Tran;
            AddData Result;

            Result = new AddData() { Status = false };
            
            try
            {
                var itemGroup = (TITEMGROUP)GetPartGroup(ItemAddData.ItemIffId);
                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Adding new item TypeID=0x{ItemAddData.ItemIffId:X}, Group={itemGroup}, Qty={ItemAddData.Quantity}, Day={ItemAddData.Day}", ConsoleColor.Cyan);
                
                switch (itemGroup)
                {
                    case TITEMGROUP.ITEM_TYPE_CHARACTER:
                        {
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Inserting character 0x{ItemAddData.ItemIffId:X} to database...", ConsoleColor.Yellow);
                            
                            using (var dbChar = new DB_pangya_character())
                            {
                                var charData = new CharacterDbData
                                {
                                    UID = (int)UID,
                                    TYPEID = (int)ItemAddData.ItemIffId,
                                    HAIR_COLOR = 0,
                                    GIFT_FLAG = 0,
                                    POWER = 0,
                                    CONTROL = 0,
                                    IMPACT = 0,
                                    SPIN = 0,
                                    CURVE = 0
                                };
                                
                                int charIndex = dbChar.Insert(charData);
                                
                                // Load character into memory
                                var newChar = new PlayerCharacterData
                                {
                                    Index = (uint)charIndex,
                                    TypeID = ItemAddData.ItemIffId,
                                    HairColour = 0,
                                    FCutinIndex = 0,
                                    Power = 0,
                                    Control = 0,
                                    Impact = 0,
                                    Spin = 0,
                                    Curve = 0,
                                    AuxPart = 0,
                                    AuxPart2 = 0,
                                    NEEDUPDATE = false
                                };
                                ItemCharacter.Add(newChar);
                                
                                Result.Status = true;
                                Result.ItemIndex = (uint)charIndex;
                                Result.ItemTypeID = ItemAddData.ItemIffId;
                                Result.ItemOldQty = 0;
                                Result.ItemNewQty = 1;
                                Result.ItemUCCKey = string.Empty;
                                Result.ItemFlag = 0;
                                Result.ItemEndDate = null;
                                
                                if (ItemAddData.Transaction)
                                    ItemTransaction.AddChar(2, newChar);
                                
                                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Character added to DB and memory, CID={charIndex}", ConsoleColor.Green);
                            }
                        }
                        break;
                        
                    case TITEMGROUP.ITEM_TYPE_CADDIE:
                        {
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Inserting caddie 0x{ItemAddData.ItemIffId:X} to database...", ConsoleColor.Yellow);
                            
                            using (var dbCaddie = new DB_pangya_caddie())
                            {
                                var caddieData = new CaddieData
                                {
                                    UID = (int)UID,
                                    TYPEID = (int)ItemAddData.ItemIffId,
                                    EXP = 0,
                                    cLevel = 0,
                                    SKIN_TYPEID = null,
                                    RentFlag = 1,
                                    END_DATE = DateTime.Now.AddYears(10),
                                    SKIN_END_DATE = null,
                                    TriggerPay = 0,
                                    VALID = 1
                                };
                                
                                int caddieIndex = dbCaddie.Insert(caddieData);
                                
                                // Load caddie into memory
                                var newCaddie = new PlayerCaddieData
                                {
                                    CaddieIdx = (uint)caddieIndex,
                                    CaddieTypeId = ItemAddData.ItemIffId,
                                    CaddieSkin = 0,
                                    CaddieSkinEndDate = null,
                                    CaddieLevel = 0,
                                    CaddieExp = 0,
                                    CaddieType = 1,
                                    CaddieDay = 0,
                                    CaddieSkinDay = 0,
                                    CaddieUnknown = 0,
                                    CaddieAutoPay = 0,
                                    CaddieDateEnd = DateTime.Now.AddYears(10),
                                    CaddieNeedUpdate = false
                                };
                                ItemCaddie.Add(newCaddie);
                                
                                Result.Status = true;
                                Result.ItemIndex = (uint)caddieIndex;
                                Result.ItemTypeID = ItemAddData.ItemIffId;
                                Result.ItemOldQty = 0;
                                Result.ItemNewQty = 1;
                                Result.ItemUCCKey = string.Empty;
                                Result.ItemFlag = 0;
                                Result.ItemEndDate = null;
                                
                                if (ItemAddData.Transaction)
                                {
                                    var caddieAsChar = new PlayerCharacterData
                                    {
                                        Index = newCaddie.CaddieIdx,
                                        TypeID = newCaddie.CaddieTypeId
                                    };
                                    ItemTransaction.AddChar(2, caddieAsChar);
                                }
                                
                                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Caddie added to DB and memory, CID={caddieIndex}", ConsoleColor.Green);
                            }
                        }
                        break;
                        
                    case TITEMGROUP.ITEM_TYPE_PART:
                    case TITEMGROUP.ITEM_TYPE_CLUB:
                    case TITEMGROUP.ITEM_TYPE_AUX:
                    case TITEMGROUP.ITEM_TYPE_BALL:
                    case TITEMGROUP.ITEM_TYPE_USE:
                    case TITEMGROUP.ITEM_TYPE_SKIN:
                        {
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Inserting warehouse item 0x{ItemAddData.ItemIffId:X} (Qty={ItemAddData.Quantity}) to database...", ConsoleColor.Yellow);
                            
                            // ✅ สำหรับ Club และ Part ใช้ C0=0 เพราะ C0-C4 เป็น STAT ไม่ใช่ Quantity
                            // สำหรับ Ball/Aux/Use ใช้ C0=Quantity
                            byte c0Value = 0;
                            if (itemGroup == TITEMGROUP.ITEM_TYPE_BALL || 
                                itemGroup == TITEMGROUP.ITEM_TYPE_AUX || 
                                itemGroup == TITEMGROUP.ITEM_TYPE_USE)
                            {
                                c0Value = (byte)Math.Min(255, ItemAddData.Quantity);
                            }
                            
                            using (var dbWarehouse = new DB_pangya_warehouse())
                            {
                                var warehouseData = new WarehouseData
                                {
                                    UID = (int)UID,
                                    TYPEID = (int)ItemAddData.ItemIffId,
                                    C0 = c0Value,
                                    C1 = 0,
                                    C2 = 0,
                                    C3 = 0,
                                    C4 = 0,
                                    RegDate = DateTime.Now,
                                    DateEnd = null,
                                    VALID = 1,
                                    Flag = 0,
                                    ItemType = 0
                                };
                                
                                // ✅ ใช้ Insert() ปกติ - ไม่ระบุ item_id ให้ AUTO_INCREMENT ทำงาน
                                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Calling DB Insert (AUTO_INCREMENT)...", ConsoleColor.Cyan);
                                int itemId = dbWarehouse.Insert(warehouseData);
                                
                                if (itemId <= 0)
                                {
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✗ ERROR - LAST_INSERT_ID() returned {itemId}! Database may be corrupted.", ConsoleColor.Red);
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]:   ℹ️ Run migrations/cleanup_broken_warehouse_items.sql to fix!", ConsoleColor.Yellow);
                                    return Result;
                                }
                                
                                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Inserted to DB with item_id={itemId} (AUTO_INCREMENT)", ConsoleColor.Green);
                                
                                // For club items, also insert club info
                                if (itemGroup == TITEMGROUP.ITEM_TYPE_CLUB)
                                {
                                    using (var dbClubInfo = new DB_pangya_club_info())
                                    {
                                        var clubInfo = new ClubInfoData
                                        {
                                            ITEM_ID = itemId,
                                            UID = (int)UID,
                                            TYPEID = (int)ItemAddData.ItemIffId,
                                            C0_SLOT = 0,
                                            C1_SLOT = 0,
                                            C2_SLOT = 0,
                                            C3_SLOT = 0,
                                            C4_SLOT = 0,
                                            CLUB_POINT = 100000,
                                            CLUB_WORK_COUNT = 0,
                                            CLUB_SLOT_CANCEL = 0,
                                            CLUB_POINT_TOTAL_LOG = 0,
                                            CLUB_UPGRADE_PANG_LOG = 0
                                        };
                                        dbClubInfo.Insert(clubInfo);
                                        PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Club info created for item_id={itemId}, TypeID=0x{ItemAddData.ItemIffId:X}", ConsoleColor.Green);
                                    }
                                }
                                
                                // ✅ Load the newly inserted item back from DB
                                var reloadedData = dbWarehouse.SelectByItemID(itemId, (int)UID);
                                
                                if (reloadedData != null)
                                {
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Reloaded from DB - Creating PlayerItemData...", ConsoleColor.Cyan);
                                    
                                    // ✅ Create WarehouseQueryResult from WarehouseData with safe conversions
                                    var queryResult = new WarehouseQueryResult
                                    {
                                        item_id = reloadedData.item_id,
                                        UID = reloadedData.UID,
                                        TYPEID = reloadedData.TYPEID,
                                        C0 = (int)(reloadedData.C0 ?? 0),
                                        C1 = (int)(reloadedData.C1 ?? 0),
                                        C2 = (int)(reloadedData.C2 ?? 0),
                                        C3 = (int)(reloadedData.C3 ?? 0),
                                        C4 = (int)(reloadedData.C4 ?? 0),
                                        RegDate = reloadedData.RegDate ?? DateTime.Now,
                                        End_Date = reloadedData.DateEnd ?? DateTime.Now.AddYears(10),
                                        VALID = reloadedData.VALID,
                                        Flag = reloadedData.Flag,
                                        HOURLEFT = 0,
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
                                    
                                    // ✅ For clubs, load club info
                                    if (itemGroup == TITEMGROUP.ITEM_TYPE_CLUB)
                                    {
                                        using (var dbClubInfo = new DB_pangya_club_info())
                                        {
                                            var clubInfo = dbClubInfo.SelectByItemID(itemId, (int)UID);
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
                                                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]:   ✓ Club info loaded - Points:{clubInfo.CLUB_POINT}", ConsoleColor.Cyan);
                                            }
                                        }
                                    }
                                    
                                    var newItem = new PlayerItemData(queryResult);
                                    ItemWarehouse.Add(newItem);
                                    
                                    Result.Status = true;
                                    Result.ItemIndex = newItem.ItemIndex;
                                    Result.ItemTypeID = newItem.ItemTypeID;
                                    Result.ItemOldQty = 0;
                                    Result.ItemNewQty = ItemAddData.Quantity;
                                    Result.ItemUCCKey = string.Empty;
                                    Result.ItemFlag = 0;
                                    Result.ItemEndDate = null;
                                    
                                    if (ItemAddData.Transaction)
                                        ItemTransaction.AddItem(2, newItem, ItemAddData.Quantity);
                                    
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Warehouse item added to memory, item_id={itemId}", ConsoleColor.Green);
                                }
                                else
                                {
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ⚠ Failed to reload item from DB, using fallback...", ConsoleColor.Yellow);
                                    
                                    // ✅ Fallback: Create minimal WarehouseQueryResult
                                    var fallbackQueryResult = new WarehouseQueryResult
                                    {
                                        item_id = itemId,
                                        UID = (int)UID,
                                        TYPEID = (int)ItemAddData.ItemIffId,
                                        C0 = c0Value,
                                        C1 = 0,
                                        C2 = 0,
                                        C3 = 0,
                                        C4 = 0,
                                        RegDate = DateTime.Now,
                                        End_Date = DateTime.Now.AddYears(10),
                                        VALID = 1,
                                        Flag = 0,
                                        HOURLEFT = 0,
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
                                    
                                    var newItem = new PlayerItemData(fallbackQueryResult);
                                    ItemWarehouse.Add(newItem);
                                    
                                    Result.Status = true;
                                    Result.ItemIndex = (uint)itemId;
                                    Result.ItemTypeID = ItemAddData.ItemIffId;
                                    Result.ItemOldQty = 0;
                                    Result.ItemNewQty = ItemAddData.Quantity;
                                    Result.ItemUCCKey = string.Empty;
                                    Result.ItemFlag = 0;
                                    Result.ItemEndDate = null;
                                    
                                    if (ItemAddData.Transaction)
                                        ItemTransaction.AddItem(2, newItem, ItemAddData.Quantity);
                                    
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Fallback item added to memory", ConsoleColor.Yellow);
                                }
                            }
                        }
                        break;
                        
                    case TITEMGROUP.ITEM_TYPE_MASCOT:
                        {
                            // ✅ Safety: Ensure Day is valid (default to 7 if 0)
                            uint mascotDays = ItemAddData.Day > 0 ? ItemAddData.Day : 7;
                            DateTime endDate = DateTime.Now.AddDays(mascotDays);
                            
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Inserting mascot 0x{ItemAddData.ItemIffId:X} (expires in {mascotDays} days)...", ConsoleColor.Yellow);
                            
                            using (var dbMascot = new DB_pangya_mascot())
                            {
                                var mascotData = new MascotData
                                {
                                    UID = (int)UID,
                                    MASCOT_TYPEID = (int)ItemAddData.ItemIffId,
                                    DateEnd = endDate,
                                    MESSAGE = "",
                                    VALID = 1
                                };
                                
                                int mascotIndex = dbMascot.Insert(mascotData);
                                
                                // Load mascot into memory
                                var newMascot = new PlayerMascotData
                                {
                                    MascotIndex = (uint)mascotIndex,
                                    MascotTypeID = ItemAddData.ItemIffId,
                                    MascotEndDate = endDate,
                                    MascotMessage = "",
                                    MascotIsValid = 1,
                                    MascotNeedUpdate = false
                                };
                                ItemMascot.Add(newMascot);
                                
                                Result.Status = true;
                                Result.ItemIndex = (uint)mascotIndex;
                                Result.ItemTypeID = ItemAddData.ItemIffId;
                                Result.ItemOldQty = 0;
                                Result.ItemNewQty = 1;
                                Result.ItemUCCKey = string.Empty;
                                Result.ItemFlag = 0;
                                Result.ItemEndDate = endDate;
                                
                                if (ItemAddData.Transaction)
                                {
                                    var mascotAsChar = new PlayerCharacterData
                                    {
                                        Index = newMascot.MascotIndex,
                                        TypeID = newMascot.MascotTypeID
                                    };
                                    ItemTransaction.AddChar(2, mascotAsChar);
                                }
                                
                                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Mascot added to DB and memory, MID={mascotIndex}", ConsoleColor.Green);
                            }
                        }
                        break;
                        
                    case TITEMGROUP.ITEM_TYPE_CARD:
                        {
                            PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Processing card 0x{ItemAddData.ItemIffId:X} (Qty={ItemAddData.Quantity})...", ConsoleColor.Yellow);

                            using (var dbCard = new DB_pangya_card())
                            {
                                // ✅ Step 1: Check if card exists
                                var existingCardData = dbCard.SelectByUIDAndTypeID((int)UID, (int)ItemAddData.ItemIffId);
                                
                                if (existingCardData != null)
                                {
                                    // ✅ Card exists in DB → Update quantity
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Card exists in DB (CardIdx:{existingCardData.CARD_IDX}, OldQty:{existingCardData.QTY}), updating...", ConsoleColor.Cyan);
                                    
                                    uint oldQty = (uint)existingCardData.QTY;
                                    uint newQty = oldQty + ItemAddData.Quantity;
                                    
                                    // Update DB
                                    dbCard.UpdateQuantity(existingCardData.CARD_IDX, (int)newQty);
                                    
                                    // Update memory
                                    var existingCardInMemory = ItemCard.GetCard(ItemAddData.ItemIffId, 1);
                                    if (existingCardInMemory != null)
                                    {
                                        existingCardInMemory.CardQuantity = newQty;
                                    }
                                    
                                    Result.Status = true;
                                    Result.ItemIndex = (uint)existingCardData.CARD_IDX;
                                    Result.ItemTypeID = (uint)existingCardData.CARD_TYPEID;
                                    Result.ItemOldQty = oldQty;
                                    Result.ItemNewQty = newQty;
                                    Result.ItemUCCKey = string.Empty;
                                    Result.ItemFlag = 0;
                                    Result.ItemEndDate = null;
                                    
                                    if (ItemAddData.Transaction && existingCardInMemory != null)
                                        ItemTransaction.AddCard(2, existingCardInMemory, ItemAddData.Quantity);
                                    
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ Card updated - OldQty:{oldQty} → NewQty:{newQty}", ConsoleColor.Green);
                                }
                                else
                                {
                                    // ✅ Card doesn't exist → Insert new
                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Card not in DB, inserting new...", ConsoleColor.Yellow);
                                    
                                    var cardData = new CardData
                                    {
                                        UID = (int)UID,
                                        CARD_TYPEID = (int)ItemAddData.ItemIffId,
                                        QTY = (int)ItemAddData.Quantity,
                                        VALID = 1,
                                        RegData = DateTime.Now
                                    };
                                    
                                    int cardIndex = dbCard.Insert(cardData);

                                    // Load card into memory
                                    var newCard = new PlayerCardData
                                    {
                                        CardIndex = (uint)cardIndex,
                                        CardTypeID = ItemAddData.ItemIffId,
                                        CardQuantity = ItemAddData.Quantity,
                                        CardIsValid = 1,
                                        CardNeedUpdate = false
                                    };
                                    ItemCard.Add(newCard);

                                    Result.Status = true;
                                    Result.ItemIndex = (uint)cardIndex;
                                    Result.ItemTypeID = ItemAddData.ItemIffId;
                                    Result.ItemOldQty = 0;
                                    Result.ItemNewQty = ItemAddData.Quantity;
                                    Result.ItemUCCKey = string.Empty;
                                    Result.ItemFlag = 0;
                                    Result.ItemEndDate = null;

                                    if (ItemAddData.Transaction)
                                        ItemTransaction.AddCard(2, newCard, ItemAddData.Quantity);

                                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: ✓ New card added - CardIdx:{cardIndex}, Qty:{ItemAddData.Quantity}", ConsoleColor.Green);
                                }
                            }
                        }
                        break;
                        
                    default:
                        PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_DB]: Unsupported item group {itemGroup} for TypeID 0x{ItemAddData.ItemIffId:X}", ConsoleColor.Yellow);
                        break;
                }
            }
            catch (Exception ex)
            {
                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_ERROR]: Failed to add item 0x{ItemAddData.ItemIffId:X}", ConsoleColor.Red);
                PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_ERROR]: {ex.Message}", ConsoleColor.Red);
                if (ex.InnerException != null)
                {
                    PangyaAPI.Tools.WriteConsole.WriteLine($"[ADD_ITEM_INNER]: {ex.InnerException.Message}", ConsoleColor.Red);
                }
            }
            
            return Result;
        }
        #endregion

        #region RemoveItems
        public AddData Remove(uint ItemIffId, uint Quantity, bool Transaction = true)
        {
            AddData ItemDeletedData;
            PlayerItemData Items;
            PlayerCardData Cards;
            PlayerTransactionData Tran;
            ItemDeletedData = new AddData() { Status = false };
            if (UID <= 0)
            { return ItemDeletedData; }

            if (ItemIffId <= 0 && Quantity <= 0)
            { return ItemDeletedData; }


            switch ((TITEMGROUP)GetPartGroup(ItemIffId))
            {
                case TITEMGROUP.ITEM_TYPE_CLUB:
                case TITEMGROUP.ITEM_TYPE_USE:
                    {
                        Items = ItemWarehouse.GetItem(ItemIffId, Quantity);

                        if (!(Items == null))
                        {
                            ItemDeletedData.Status = true;
                            ItemDeletedData.ItemIndex = Items.ItemIndex;
                            ItemDeletedData.ItemTypeID = Items.ItemTypeID;
                            ItemDeletedData.ItemOldQty = Items.ItemC0;
                            ItemDeletedData.ItemNewQty = Items.ItemC0 - Quantity;
                            ItemDeletedData.ItemUCCKey = Items.ItemUCCUnique;
                            ItemDeletedData.ItemFlag = 0;
                            ItemDeletedData.ItemEndDate = null;
                            if (Transaction)
                            {
                                Tran = new PlayerTransactionData() { UCC = "", Types = 2, TypeID = Items.ItemTypeID, Index = Items.ItemIndex, PreviousQuan = Items.ItemC0, NewQuan = Items.ItemC0 - Quantity };
                                ItemTransaction.Add(Tran);
                            }

                            // update item info
                            Items.RemoveQuantity(Quantity);
                        }
                        return ItemDeletedData;
                    }
                case TITEMGROUP.ITEM_TYPE_CARD:
                    {
                        Cards = ItemCard.GetCard(ItemIffId, Quantity);

                        if (!(Cards == null))
                        {
                            ItemDeletedData.Status = true;
                            ItemDeletedData.ItemIndex = Cards.CardIndex;
                            ItemDeletedData.ItemTypeID = Cards.CardTypeID;
                            ItemDeletedData.ItemOldQty = Cards.CardQuantity;
                            ItemDeletedData.ItemNewQty = Cards.CardQuantity - Quantity;
                            ItemDeletedData.ItemUCCKey = string.Empty;
                            ItemDeletedData.ItemFlag = 0;
                            ItemDeletedData.ItemEndDate = null;
                            if (Transaction)
                            {
                                Tran = new PlayerTransactionData() { UCC = "", Types = 2, TypeID = Cards.CardTypeID, Index = Cards.CardIndex, PreviousQuan = Cards.CardQuantity, NewQuan = Cards.CardQuantity - Quantity };
                                ItemTransaction.Add(Tran);
                            }
                        }
                        // update item info
                        Cards.RemoveQuantity(Quantity);
                        return ItemDeletedData;
                    }
            }
            ItemDeletedData.SetData(false, 0, 0, 0, 0, string.Empty, 0, DateTime.Now);
            return (ItemDeletedData);
        }

        public AddData Remove(uint ItemIffId, uint Index, uint Quantity, bool Transaction = true)
        {
            AddData ItemDeletedData;
            PlayerItemData Items;
            PlayerCardData Cards;
            PlayerTransactionData Tran;
            ItemDeletedData = new AddData() { Status = false };
            if (UID <= 0)
            { return ItemDeletedData; }

            if (ItemIffId <= 0 && Quantity <= 0)
            { return ItemDeletedData; }


            switch ((TITEMGROUP)GetPartGroup(ItemIffId))
            {
                case TITEMGROUP.ITEM_TYPE_CLUB:
                case TITEMGROUP.ITEM_TYPE_USE:
                    {
                        Items = ItemWarehouse.GetItem(ItemIffId, Index, Quantity);

                        if (!(Items == null))
                        {
                            ItemDeletedData.Status = true;
                            ItemDeletedData.ItemIndex = Items.ItemIndex;
                            ItemDeletedData.ItemTypeID = Items.ItemTypeID;
                            ItemDeletedData.ItemOldQty = Items.ItemC0;
                            ItemDeletedData.ItemNewQty = Items.ItemC0 - Quantity;
                            ItemDeletedData.ItemUCCKey = Items.ItemUCCUnique;
                            ItemDeletedData.ItemFlag = 0;
                            ItemDeletedData.ItemEndDate = null;
                            if (Transaction)
                            {
                                Tran = new PlayerTransactionData() { UCC = "", Types = 2, TypeID = Items.ItemTypeID, Index = Items.ItemIndex, PreviousQuan = Items.ItemC0, NewQuan = Items.ItemC0 - Quantity };
                                ItemTransaction.Add(Tran);
                            }

                        }
                        // update item info
                        Items.RemoveQuantity(Quantity);
                        return ItemDeletedData;
                    }
                case TITEMGROUP.ITEM_TYPE_PART:
                    {
                        Items = ItemWarehouse.GetItem(ItemIffId, Index, 0); // ## part should be zero

                        if (!(Items == null))
                        {
                            ItemDeletedData.Status = true;
                            ItemDeletedData.ItemIndex = Items.ItemIndex;
                            ItemDeletedData.ItemTypeID = Items.ItemTypeID;
                            ItemDeletedData.ItemOldQty = 1;
                            ItemDeletedData.ItemNewQty = 0;
                            ItemDeletedData.ItemUCCKey = Items.ItemUCCUnique;
                            ItemDeletedData.ItemFlag = 0;
                            ItemDeletedData.ItemEndDate = null;
                            if (Transaction)
                            {
                                Tran = new PlayerTransactionData() { UCC = "", Types = 2, TypeID = Items.ItemTypeID, Index = Items.ItemIndex, PreviousQuan = 1, NewQuan = 0 };
                                ItemTransaction.Add(Tran);
                            }
                        }
                        // update item info
                        Items.RemoveQuantity(Quantity);
                        return ItemDeletedData;
                    }
                case TITEMGROUP.ITEM_TYPE_CARD:
                    {
                        Cards = ItemCard.GetCard(ItemIffId, Index, Quantity);

                        if (!(Cards == null))
                        {
                            ItemDeletedData.Status = true;
                            ItemDeletedData.ItemIndex = Cards.CardIndex;
                            ItemDeletedData.ItemTypeID = Cards.CardTypeID;
                            ItemDeletedData.ItemOldQty = Cards.CardQuantity;
                            ItemDeletedData.ItemNewQty = Cards.CardQuantity - Quantity;
                            ItemDeletedData.ItemUCCKey = string.Empty;
                            ItemDeletedData.ItemFlag = 0;
                            ItemDeletedData.ItemEndDate = null;
                            if (Transaction)
                            {
                                Tran = new PlayerTransactionData() { UCC = "", Types = 2, TypeID = Cards.CardTypeID, Index = Cards.CardIndex, PreviousQuan = Cards.CardQuantity, NewQuan = Cards.CardQuantity - Quantity };
                                ItemTransaction.Add(Tran);
                            }
                        }
                        // update item info
                        Cards.RemoveQuantity(Quantity);
                        return ItemDeletedData;
                    }
            }
            ItemDeletedData.SetData(false, 0, 0, 0, 0, string.Empty, 0, DateTime.Now);
            return (ItemDeletedData);
        }

        #endregion
    }
}
