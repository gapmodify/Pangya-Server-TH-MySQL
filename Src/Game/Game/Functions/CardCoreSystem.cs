using System;
using System.Collections.Generic;
using System.Linq;
using Game.Defines;
using Game.Client;
using PangyaAPI;
using static PangyaFileCore.IffBaseManager;
using static Game.GameTools.Tools;
using System.Runtime.InteropServices;
using Game.Client.Inventory.Data.Transactions;
using Game.Client.Inventory.Data.Character;
using Game.Client.Inventory.Data.Card;
using Game.Client.Inventory.Data;
using Game.Client.Inventory.Data.CardEquip;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using Game.ItemList;

namespace Game.Functions
{
    public class CardSystem
    {
        #region Struct For PacketRead 
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct TCardDataChange
        {
            public ulong PangSum;
            public uint CardTypeID;
            public uint CardTypeID2;
            public uint CardTypeID3;
        }
        protected struct TPData
        {
            public uint TypeID;
            public uint CardIndex;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct TCardData
        {
            public uint CharTypeID;
            public uint CharIndex;
            public uint CardTypeID;
            public uint CardIndex;
            public uint Position;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct TCardRemove
        {
            public uint CharTypeID;
            public uint CharIndex;
            public uint RemoverTypeID;
            public uint RemoverIndex;
            public uint Slot;
        }
        #endregion

        protected CardList Items;
        
        public CardSystem()
        {
            Items = new CardList();
        }

        // Add CardCheckPosition method
        private bool CardCheckPosition(uint CardTypeID, uint Position)
        {
            try
            {
                var card = new PangyaFileCore.Data.Card();
                if (!IffEntry.Card.LoadCard(CardTypeID, ref card))
                {
                    WriteConsole.WriteLine($"[CARD_CHECK_POS] Card not found in IFF: 0x{CardTypeID:X}", ConsoleColor.Yellow);
                    return false;
                }
                
                // Position should be between 1-10 for card slots
                bool isValidPos = Position >= 1 && Position <= 10;
                bool isPosMatch = card.Position == Position;
                
                WriteConsole.WriteLine($"[CARD_CHECK_POS] Card:0x{CardTypeID:X} ClientPos:{Position} CardPos:{card.Position} Valid:{isValidPos} Match:{isPosMatch}", ConsoleColor.Gray);
                
                // ใช้แค่ check range ก็พอ ไม่ต้อง match position
                return isValidPos;
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[CARD_CHECK_POS_ERROR] {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        public void PlayerPutCard(GPlayer PL, Packet packet)
        {
            TCardData Data;
            PlayerCardData PLCard;
            PlayerCharacterData PLCharacter;
            AddData ItemData;
            PlayerTransactionData Transac;

            Data = (TCardData)packet.Read(new TCardData());

            WriteConsole.WriteLine($"[PUT_CARD_DEBUG] Player:{PL.GetNickname} CharIdx:{Data.CharIndex} CardIdx:{Data.CardIndex} CardType:0x{Data.CardTypeID:X} Pos:{Data.Position}", ConsoleColor.Cyan);

            PLCard = PL.Inventory.ItemCard.GetCard(Data.CardIndex);
            PLCharacter = PL.Inventory.ItemCharacter.GetChar(Data.CharIndex, CharType.bIndex);

            if (PLCard == null || PLCharacter == null)
            {
                WriteConsole.WriteLine($"[PUT_CARD_ERROR] Card or Character not found! Card:{PLCard != null} Char:{PLCharacter != null}", ConsoleColor.Red);
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                return;
            }

            if (!(PLCard.CardTypeID == Data.CardTypeID) || (!(PLCharacter.TypeID == Data.CharTypeID)))
            {
                WriteConsole.WriteLine($"[PUT_CARD_ERROR] TypeID mismatch! CardType:0x{PLCard.CardTypeID:X} vs 0x{Data.CardTypeID:X}, CharType:0x{PLCharacter.TypeID:X} vs 0x{Data.CharTypeID:X}", ConsoleColor.Red);
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                return;
            }

            if (!(CardCheckPosition(Data.CardTypeID, Data.Position)))
            {
                WriteConsole.WriteLine($"[PUT_CARD_ERROR] Invalid position! CardType:0x{Data.CardTypeID:X} Position:{Data.Position}", ConsoleColor.Red);
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                return;
            }


            ItemData = PL.Inventory.Remove(Data.CardTypeID, 1, true);

            if (!ItemData.Status)
            {
                WriteConsole.WriteLine($"[PUT_CARD_ERROR] Failed to remove card from inventory", ConsoleColor.Red);
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                return;
            }

            WriteConsole.WriteLine($"[PUT_CARD_DEBUG] Updating card in DB... CharIdx:{PLCharacter.Index} CardType:0x{ItemData.ItemTypeID:X} Slot:{Data.Position}", ConsoleColor.Yellow);

            try
            {
                var updateResult = PL.Inventory.ItemCharacter.Card.UpdateCard(PL.GetUID, PLCharacter.Index, PLCharacter.TypeID, ItemData.ItemTypeID, (byte)Data.Position, 0, 0);
                
                if (updateResult != null && updateResult.First().Key)
                {
                    WriteConsole.WriteLine($"[PUT_CARD_SUCCESS] Card equipped successfully!", ConsoleColor.Green);

                    Transac = new PlayerTransactionData()
                    {
                        Types = 0xCB,
                        TypeID = PLCharacter.TypeID,
                        Index = PLCharacter.Index,
                        PreviousQuan = 0,
                        NewQuan = 0,
                        UCC = "",
                        CardTypeID = Data.CardTypeID,
                        CharSlot = (byte)Data.Position,
                    };

                    PL.Inventory.ItemTransaction.Add(Transac);
                    PL.SendTransaction();

                    PL.Response.Write(new byte[] { 0x71, 0x02, 0x00, 0x00, 0x00, 0x00 });
                    PL.Response.Write(Data.CardTypeID);
                    PL.Response.WriteByte((byte)Data.Position);
                    PL.SendResponse();
                    
                    WriteConsole.WriteLine($"[PUT_CARD_PACKET] Sent success response - CardType:0x{Data.CardTypeID:X} Slot:{Data.Position}", ConsoleColor.Cyan);
                }
                else
                {
                    WriteConsole.WriteLine($"[PUT_CARD_ERROR] Database update failed!", ConsoleColor.Red);
                    PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[PUT_CARD_EXCEPTION] {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[PUT_CARD_EXCEPTION] {ex.StackTrace}", ConsoleColor.DarkRed);
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
            }
        }

        public void PlayerPutBonusCard(GPlayer PL, Packet packet)
        {
            const uint BongdariClip = 0x1A00018F;
            TCardData Data;
            PlayerCardData PLCard;
            PlayerCharacterData PLCharacter;
            AddData ItemData;
            PlayerTransactionData Transac;

            Data = (TCardData)packet.Read(new TCardData());


            if (!PL.Inventory.IsExist(BongdariClip))
            {
                PL.SendResponse(new byte[] { 0x72, 0x02, 0xB3, 0xF9, 0x56, 0x00 });

            }

            PLCard = PL.Inventory.ItemCard.GetCard(Data.CardIndex);
            PLCharacter = PL.Inventory.ItemCharacter.GetChar(Data.CharIndex, CharType.bIndex);


            if (PLCard == null || PLCharacter == null)
            {
                return;
            }

            if (!(PLCard.CardTypeID == Data.CardTypeID) || (!(PLCharacter.TypeID == Data.CharTypeID)))
            {
                return;
            }

            if (!(CardCheckPosition(Data.CardTypeID, Data.Position)))
            {
                return;
            }

            // ## (* DELETE BONGDARI CLIP *)
            ItemData = PL.Inventory.Remove(BongdariClip, 1, true);

            if (!ItemData.Status)
            {
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                return;
            }

            ItemData = PL.Inventory.Remove(Data.CardTypeID, 1, true);

            if (!ItemData.Status)
            {
                PL.SendResponse(new byte[] { 0x71, 0x02, 0xB3, 0xF9, 0x56, 0x00 });
                return;
            }
            // ## (* UPDATE PLAYER *)
            if (PL.Inventory.ItemCharacter.Card.UpdateCard(PL.GetUID, PLCharacter.Index, PLCharacter.TypeID, ItemData.ItemTypeID, (byte)Data.Position, 0, 0).First().Key)
            {
                Transac = new PlayerTransactionData()
                {
                    Types = 0xCB,
                    TypeID = PLCharacter.TypeID,
                    Index = PLCharacter.Index,
                    PreviousQuan = 0,
                    NewQuan = 0,
                    UCC = "",
                    CardTypeID = Data.CardTypeID,
                    CharSlot = (byte)Data.Position,
                };

                PL.Inventory.ItemTransaction.Add(Transac);

                PL.SendTransaction();

                PL.Response.Write(new byte[] { 0x71, 0x02, 0x00, 0x00, 0x00, 0x00 });
                PL.Response.Write(Data.CardTypeID);
                PL.SendResponse();
            }
        }

        public void PlayerCardRemove(GPlayer PL, Packet packet)
        {
            TCardRemove Data;
            PlayerCardEquipData CARDDATA;
            PlayerCharacterData PLCharacter;
            AddData ItemData;
            AddItem ItemAdd;
            PlayerTransactionData Transac;


            Data = (TCardRemove)packet.Read(new TCardRemove());

            if (!(Data.RemoverTypeID == 0x1A0000C2))
            {
                PL.SendResponse(new byte[] { 0x73, 0x02, 0x62, 0x73, 0x55, 0x00 });
                return;
            }

            PLCharacter = PL.Inventory.ItemCharacter.GetChar(Data.CharIndex, CharType.bIndex);

            CARDDATA = PL.Inventory.ItemCharacter.Card.GetCard(PLCharacter.Index, Data.Slot);

            if (!(CARDDATA == null || CARDDATA.CARD_TYPEID == 0) || !(GetItemGroup((uint)CARDDATA.CARD_TYPEID) == 0x1F))
            {
                PL.SendResponse(new byte[] { 0x73, 0x02, 0x62, 0x73, 0x55, 0x00 });
                return;
            }


            ItemData = PL.Inventory.Remove(Data.RemoverTypeID, 1, true);

            if (!ItemData.Status)
            {
                PL.SendResponse(new byte[] { 0x73, 0x02, 0x62, 0x73, 0x55, 0x00 });

                return;
            }

            ItemAdd = new AddItem()
            {
                ItemIffId = CARDDATA.CHAR_TYPEID,
                Transaction = true,
                Quantity = 1,
                Day = 0
            };
            PL.Inventory.AddItem(ItemAdd);

            if (PL.Inventory.ItemCharacter.Card.UpdateCard(PL.GetUID, PLCharacter.Index, PLCharacter.TypeID, 0, (byte)Data.Slot, 0, 0).First().Key)
            {
                Transac = new PlayerTransactionData()
                {
                    Types = 0xCB,
                    TypeID = PLCharacter.TypeID,
                    Index = PLCharacter.Index,
                    PreviousQuan = 0,
                    NewQuan = 0,
                    UCC = "",
                    CardTypeID = 0,
                    CharSlot = (byte)Data.Slot,
                };

                PL.Inventory.ItemTransaction.Add(Transac);

                PL.SendTransaction();

                PL.Response.Write(new byte[] { 0x73, 0x00 });
                PL.Response.WriteUInt32(0);
                PL.Response.WriteUInt32(CARDDATA.CARD_TYPEID);
                PL.SendResponse();
            }
        }

        public void PlayerOpenCardPack(GPlayer PL, Packet packet)
        {
            TPData CardData;
            PlayerCardData PlayerCard;
            List<Dictionary<UInt32, byte>> CardList;

            bool CheckCard(uint ID)
            {
                switch (ID)
                {
                    case 0x7CC00000: // Pack 1
                    case 0x7CC00001: // Golden Ticket
                    case 0x7CC00002: // Silver Ticket
                    case 0x7CC00003: // Bronze Ticket
                    case 0x7CC00004: // Pack 2
                    case 0x7CC00005: // Pack 3
                    case 0x7CC00006: // Platinum Ticket
                    case 0x7CC00007: // Pack 4
                    case 0x7CC00008: // Grand Prix
                    case 0x7CC0000A: // Fresh Up
                    case 0x7D000001: // Card Box No.2
                    case 0x7D000002: // Card Box No.3
                    case 0x7D000003: // Card Box No.4
                        {
                            return true;
                        }
                    default:
                        { return false; }
                }
            }

            CardData = (TPData)packet.Read(new TPData());

            // 🔍 Debug Log
            WriteConsole.WriteLine($"[OPEN_CARD_DEBUG] Player: {PL.GetNickname} (UID:{PL.GetUID})", ConsoleColor.Cyan);
            WriteConsole.WriteLine($"[OPEN_CARD_DEBUG] Client sent - TypeID: 0x{CardData.TypeID:X}, CardIndex: {CardData.CardIndex}", ConsoleColor.Yellow);
            
            // ✅ Clean up invalid cards
            var invalidCards = PL.Inventory.ItemCard.Where(c => c.CardQuantity == 0 || c.CardIsValid == 0).ToList();
            if (invalidCards.Count > 0)
            {
                WriteConsole.WriteLine($"[OPEN_CARD_CLEANUP] Removing {invalidCards.Count} invalid cards...", ConsoleColor.Yellow);
                foreach (var invalid in invalidCards)
                {
                    PL.Inventory.ItemCard.Remove(invalid);
                }
            }
            
            WriteConsole.WriteLine($"[OPEN_CARD_DEBUG] Valid cards in inventory: {PL.Inventory.ItemCard.Count}", ConsoleColor.Cyan);

            // แสดงการ์ดทั้งหมด
            if (PL.Inventory.ItemCard.Count > 0)
            {
                WriteConsole.WriteLine($"[OPEN_CARD_DEBUG] Player's card packs:", ConsoleColor.White);
                foreach (var card in PL.Inventory.ItemCard)
                {
                    WriteConsole.WriteLine($"  • Index:{card.CardIndex} TypeID:0x{card.CardTypeID:X} Qty:{card.CardQuantity} Valid:{card.CardIsValid}", ConsoleColor.Gray);
                }
            }

            // ✅ Step 1: ลองหา CardPack โดยใช้ TypeID ก่อน (เพราะ TypeID น่าเชื่อถือกว่า CardIndex)
            PlayerCard = PL.Inventory.ItemCard.FirstOrDefault(c => 
                c.CardTypeID == CardData.TypeID && 
                c.CardQuantity > 0 && 
                c.CardIsValid == 1);

            if (PlayerCard != null)
            {
                WriteConsole.WriteLine($"[OPEN_CARD_SUCCESS] ✅ Found card pack by TypeID! CardIndex={PlayerCard.CardIndex}", ConsoleColor.Green);
            }
            else
            {
                // Step 2: ถ้าหาด้วย TypeID ไม่เจอ ลองใช้ CardIndex
                WriteConsole.WriteLine($"[OPEN_CARD_DEBUG] Card not found by TypeID 0x{CardData.TypeID:X}, trying CardIndex={CardData.CardIndex}...", ConsoleColor.Yellow);
                
                PlayerCard = PL.Inventory.ItemCard.GetCard(CardData.CardIndex);

                if (PlayerCard != null && PlayerCard.CardQuantity > 0 && PlayerCard.CardIsValid == 1)
                {
                    WriteConsole.WriteLine($"[OPEN_CARD_SUCCESS] ✅ Found card by CardIndex! TypeID=0x{PlayerCard.CardTypeID:X}", ConsoleColor.Green);
                }
                else
                {
                    WriteConsole.WriteLine($"[OPEN_CARD_ERROR] ❌ Card pack not found in inventory!", ConsoleColor.Red);
                    WriteConsole.WriteLine($"[OPEN_CARD_ERROR]   Requested: TypeID=0x{CardData.TypeID:X}, CardIndex={CardData.CardIndex}", ConsoleColor.Red);
                    PL.SendResponse(new byte[] { 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 });
                    return;
                }
            }

            if (!CheckCard(PlayerCard.CardTypeID))
            {
                PL.SendResponse(new byte[] { 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 });
                WriteConsole.WriteLine($"PlayerOpenCardPack: CardType Not Found - TypeID: 0x{PlayerCard.CardTypeID:X} ({PlayerCard.CardTypeID})", ConsoleColor.Red);
                return;
            }

            // ## delete PL card - ใช้ CardIndex แทน TypeID
            if (!PL.Inventory.Remove(PlayerCard.CardTypeID, 1, false).Status)
            {
                PL.SendResponse(new byte[] { 0x54, 0x01, 0x01, 0x00, 0x00, 0x00 });
                WriteConsole.WriteLine("PlayerOpenCardPack: Failed to remove card from inventory", ConsoleColor.Red);
                return;
            }

            WriteConsole.WriteLine($"[OPEN_CARD_SUCCESS] Opening pack TypeID:0x{PlayerCard.CardTypeID:X}", ConsoleColor.Green);

            // ## get random card
            CardList = Items.GetCard(PlayerCard.CardTypeID);
            
            if (CardList == null || CardList.Count == 0)
            {
                PL.SendResponse(new byte[] { 0x54, 0x01, 0x02, 0x00, 0x00, 0x00 });
                WriteConsole.WriteLine($"[OPEN_CARD_ERROR] No cards available from pack TypeID:0x{PlayerCard.CardTypeID:X}", ConsoleColor.Red);
                return;
            }

            try
            {
                packet.Write(new byte[] { 0x54, 0x01 });
                packet.WriteUInt32(0); // ## 0 = success
                packet.WriteUInt32(PlayerCard.CardIndex);
                packet.WriteUInt32(PlayerCard.CardTypeID);
                packet.WriteZero(0xC);
                packet.WriteUInt32(1);
                packet.WriteZero(0x20);
                packet.WriteUInt16(1);
                packet.WriteByte((byte)CardList.Count);

                WriteConsole.WriteLine($"[OPEN_CARD_RESULT] Cards received: {CardList.Count}", ConsoleColor.Cyan);

                foreach (var data in CardList)
                {
                    uint cardTypeID = data.FirstOrDefault().Key;
                    byte rarityFromDict = data.FirstOrDefault().Value;
                    
                    var AddData = new AddItem
                    {
                        ItemIffId = cardTypeID,
                        Transaction = false,
                        Quantity = 1,
                        Day = 0
                    };
                    // ## add item
                    var ResultAdd = PL.AddItem(AddData);

                    // Get actual rarity from IFF file
                    var cardInfo = new PangyaFileCore.Data.Card();
                    byte actualRarity = 0;
                    if (IffEntry.Card.LoadCard(cardTypeID, ref cardInfo))
                    {
                        actualRarity = cardInfo.Rarity;
                    }

                    WriteConsole.WriteLine($"  • Got card: TypeID=0x{ResultAdd.ItemTypeID:X} Index={ResultAdd.ItemIndex} Rarity={actualRarity} (Dict said: {rarityFromDict})", ConsoleColor.White);

                    packet.WriteUInt32(ResultAdd.ItemIndex);
                    packet.WriteUInt32(ResultAdd.ItemTypeID);
                    packet.WriteZero(0xC);
                    packet.WriteUInt32(ResultAdd.ItemNewQty);
                    packet.WriteZero(0x20);
                    packet.WriteUInt16(1);
                    packet.WriteUInt32(1);
                }
                PL.SendResponse(packet.GetBytes());
            }
            finally
            {
                packet.Dispose();
                CardList.Clear();
            }
        }

        public void PlayerCardSpecial(GPlayer PL, Packet packet)
        {
            Dictionary<bool, PlayerCardEquipData> CP;
            AddData Remove;
            DateTime? GetDate = DateTime.Now;


            if (!packet.ReadUInt32(out uint TypeID))
            {
                return;
            }
            var C = Items.GetCardSPCL(TypeID);

            if (false == C.Keys.FirstOrDefault() || (!(GetCardType(TypeID) == CARDTYPE.tSpecial)))
            {
                return;
            }

            Remove = PL.Inventory.Remove(TypeID, 1, false);


            if (!Remove.Status) return;

            switch (C.Values.ToList().FirstOrDefault().Base.TypeID)
            {
                case 0x7C800000:
                case 0x7C800022:
                case 0x7C800034:
                    {
                        PL.AddExp(C.FirstOrDefault().Value.EffectQty);
                    }
                    break;
                case 0x7C80001F:
                    PL.AddPang(C.FirstOrDefault().Value.Effect);
                    break;
                default:
                    CP = PL.Inventory.ItemCharacter.Card.UpdateCard(PL.GetUID, 0, 0, TypeID, 0, 1, (byte)C.FirstOrDefault().Value.Time);
                    if (!CP.Keys.FirstOrDefault())
                    {
                        return;
                    }
                    GetDate = CP.Values.FirstOrDefault().ENDDATE;
                    break;
            }

            PL.Response.Write(new byte[] { 0x60, 0x01 });
            PL.Response.WriteUInt32(0);
            PL.Response.WriteUInt32(Remove.ItemIndex);
            PL.Response.WriteUInt32(Remove.ItemTypeID);
            PL.Response.WriteZero(0xC);
            PL.Response.Write(1);
            PL.Response.Write(GetFixTime(DateTime.Now));
            PL.Response.Write(GetFixTime(GetDate));
            PL.Response.WriteZero(2);
            PL.SendResponse();


            switch (C.Values.ToList().FirstOrDefault().Base.TypeID)
            {
                case 0x7C800000:
                case 0x7C800022:
                case 0x7C800034:
                    {
                        PL.SendExp();
                    }
                    break;
                case 0x7C80001F:
                    PL.SendPang();
                    break;

            }

        }

        public void PlayerLoloCardDeck(GPlayer PL, Packet packet)
        {
            TCardDataChange CardData;
            PlayerCardData PlayerCard;
            PlayerCardData PlayerCard2;
            PlayerCardData PlayerCard3;


            CardData = (TCardDataChange)packet.Read(new TCardDataChange());


            //// ## get card
            PlayerCard = PL.Inventory.ItemCard.GetCard(CardData.CardTypeID, 1);
            PlayerCard2 = PL.Inventory.ItemCard.GetCard(CardData.CardTypeID2, 1);
            PlayerCard3 = PL.Inventory.ItemCard.GetCard(CardData.CardTypeID3, 1);

            try
            {

                //Check is Card Exist
                if (PlayerCard == null || PlayerCard2 == null || PlayerCard3 == null)
                {
                    PL.SendResponse(new byte[] { 0x2A, 0x02, 0x01, 0x00, 0x00, 0x00, });
                    WriteConsole.WriteLine("HandlePlayerLoloDeck: is null", ConsoleColor.Red);
                    return;
                }
                // ## delete PL card
                if (!PL.Inventory.Remove(PlayerCard.CardTypeID, 1, true).Status || !PL.Inventory.Remove(PlayerCard2.CardTypeID, 1, true).Status || !PL.Inventory.Remove(PlayerCard3.CardTypeID, 1, true).Status)
                {
                    PL.SendResponse(new byte[] { 0x2A, 0x02, 0x01, 0x00, 0x00, 0x00, });
                    WriteConsole.WriteLine("HandlePlayerLoloDeck: Card No Found", ConsoleColor.Red);
                    return;
                }
                if (!PL.RemovePang((uint)CardData.PangSum))
                {
                    PL.SendResponse(new byte[] { 0x2A, 0x02, 0x01, 0x00, 0x00, 0x00, });
                    WriteConsole.WriteLine("HandlePlayerLoloDeck: Pangs null", ConsoleColor.Red);
                    return;
                }

                // ## get random card
                var Card = Items.GetCard(PlayerCard.CardTypeID, PlayerCard2.CardTypeID, PlayerCard3.CardTypeID);
                if (Card.TypeID > 0)
                {
                    var result = new byte[] { 0x2E, 0x02, 0x00, 0x00, 0x00, 0x00 };
                    PL.SendResponse(result);

                    result = new byte[] { 0x20, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    PL.SendResponse(result);

                    result = new byte[] { 0x29, 0x02, 0x02, 0x00, 0x00, 0x00 };
                    PL.SendResponse(result);

                    PL.SendPang();

                    PL.AddItem(new AddItem()
                    {
                        ItemIffId = Card.TypeID,
                        Quantity = 1,
                        Day = 0,
                        Transaction = true
                    });
                    PL.SendTransaction();

                    PL.Response.Write(new byte[] { 0x2A, 0x02 });
                    PL.Response.Write(0);
                    PL.Response.Write(Card.TypeID);
                    PL.SendResponse();
                }
                else
                {
                    PL.SendResponse(new byte[] { 0x2A, 0x02, 0x00, 0x00, 0x00, 0x00 });
                    WriteConsole.WriteLine("HandlePlayerLoloDeck: Fatal Error", ConsoleColor.Red);
                    return;
                }
            }
            catch
            {
                PL.Close();
            }
        }
    }
}
