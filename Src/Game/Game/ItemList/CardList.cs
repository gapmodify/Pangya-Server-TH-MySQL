using PangyaAPI.Tools;
using PangyaFileCore.Data;
using PangyaFileCore.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using static PangyaFileCore.IffBaseManager;
namespace Game.ItemList
{
    public class CardList : ItemRandomClass
    {
        public class TCardPack
        {
            public TypeCard CardTypePack { get; set; }
            public byte Quantity { get; set; }
        }

        public class TCardData
        {
            public uint TypeID { get; set; }
            public uint Quantity { get; set; }
            public TCardData(uint ID)
            {
                TypeID = ID;
                Quantity = 1;
            }
            public TCardData()
            {
            }
        }

        Dictionary<uint, TCardPack> PackData { get; set; }
        List<Card> ListCard { get; set; }

        // Static list of known Card Pack TypeIDs (consumable items that can be purchased multiple times)
        private static readonly HashSet<uint> CardPackTypeIDs = new HashSet<uint>
        {
            2092957696, // Pangya Card Pack No.1
            2092957697, // Golden Card Ticket
            2092957698, // Silver Card Ticket
            2092957699, // Bronze card ticket
            2092957700, // Pangya Card Pack No.2
            2092957701, // Card Pack No.3
            2092957702, // Platinum Ticket
            2092957703, // Card Pack No.4
            2092957704, // Grand Prix Card Pack
            2092957706, // Fresh Up! Card Pack
            2097152001, // Pangya Card Box No.2
            2097152002, // Card Box No.3
            2097152003, // Pangya Card Box #4
        };

        public static bool IsCardPack(uint typeId)
        {
            return CardPackTypeIDs.Contains(typeId);
        }

        public CardList()
        {
            // โหลดข้อมูลการ์ดจาก IFF เข้า list ก่อน
            ListCard = IffEntry.Card?.Values?.ToList() ?? new List<Card>();
            PackData = new Dictionary<uint, TCardPack>();

            // Debug: แสดงจำนวนการ์ดแต่ละ Rarity
            var rarityCount = ListCard.GroupBy(c => c.Rarity).Select(g => new { Rarity = g.Key, Count = g.Count() }).OrderBy(x => x.Rarity);
            WriteConsole.WriteLine($"[CARD_LIST_INIT] Total cards loaded: {ListCard.Count}", ConsoleColor.Cyan);
            foreach (var r in rarityCount)
            {
                WriteConsole.WriteLine($"  • Rarity {r.Rarity}: {r.Count} cards", ConsoleColor.White);
            }

            // ไม่เรียก AddCardItem() ที่นี่ - เพราะจะทำให้ pool ปนกัน
            // AddCardItem();
            AddPack();
        }
        public List<Dictionary<uint, byte>> GetCard(uint PackTypeID)
        {

            ushort GetProb(byte RareType)
            {
                switch (RareType)
                {
                    case 0:
                        return 100;
                    case 1:
                        return 6;
                    case 2:
                        return 2;
                    case 3:
                        return 1;
                    default:
                        return (ushort)GameTools.MathRand.Rand.Next(0, 120);
                }
            }

            ushort GetFreshUPProb(byte RareType)
            {
                switch (RareType)
                {
                    case 1:
                        return 100;
                    case 2:
                        return 10;
                    case 3:
                        return 4;
                    default:
                        return (ushort)GameTools.MathRand.Rand.Next(0, 120);
                }
            }

            List<Dictionary<uint, byte>> result;
            byte CQty;
            ItemRandom CItem;
            result = new List<Dictionary<uint, byte>>();
            try
            {
                PackData.TryGetValue(PackTypeID, out TCardPack CPack);
                if (CPack == null)
                {
                    return result;
                }
                switch (CPack.CardTypePack)
                {
                    case TypeCard.Pack1:
                        foreach (var PZCard in ListCard)
                        {
                            if (PZCard.Volumn == 1)
                            {
                                AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetProb(PZCard.Rarity));
                            }
                        }
                        break;
                    case TypeCard.Pack2:
                        foreach (var PZCard in ListCard)
                        {
                            if (PZCard.Volumn == 2)
                            {
                                AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetProb(PZCard.Rarity));
                            }
                        }
                        break;
                    case TypeCard.Pack3:
                        foreach (var PZCard in ListCard)
                        {
                            if (PZCard.Volumn == 3)
                            {
                                AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetProb(PZCard.Rarity));
                            }
                        }
                        break;
                    case TypeCard.Pack4:
                        foreach (var PZCard in ListCard)
                        {
                            if (PZCard.Volumn == 4)
                            {
                                AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetProb(PZCard.Rarity));
                            }
                        }
                        break;
                    case TypeCard.Rare:
                        foreach (var PZCard in ListCard)
                        {
                            if (PZCard.Rarity >= 1)
                            {
                                AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetFreshUPProb(PZCard.Rarity));
                            }
                        }
                        break;
                    case TypeCard.All:
                        foreach (var PZCard in ListCard)
                        {
                            AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetProb(PZCard.Rarity));
                        }
                        break;
                    case TypeCard.SecretOnly:
                        {
                            int secretCardCount = 0;
                            foreach (var PZCard in ListCard)
                            {
                                if (PZCard.Rarity == 3) // SC/Secret Card only
                                {
                                    AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetFreshUPProb(PZCard.Rarity));
                                    secretCardCount++;
                                }
                            }
                            
                            if (secretCardCount == 0)
                            {
                                WriteConsole.WriteLine($"[CARD_WARNING] No Secret Cards (Rarity=3) found! Adding Rare cards instead...", ConsoleColor.Yellow);
                                // Fallback to Rare cards if no Secret cards exist
                                foreach (var PZCard in ListCard)
                                {
                                    if (PZCard.Rarity >= 2) // Rare and Secret
                                    {
                                        AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetFreshUPProb(PZCard.Rarity));
                                    }
                                }
                            }
                            else
                            {
                                WriteConsole.WriteLine($"[CARD_INFO] Found {secretCardCount} Secret Cards (Rarity=3)", ConsoleColor.Green);
                            }
                        }
                        break;
                }
                // ## set random class
                SetCanDup(false);
                Arrange();
                for (CQty = 1; CQty <= CPack.Quantity; CQty++)
                {
                    CItem = GetItems();
                    if (CItem != null)
                    {
                        // Load card info to get actual rarity
                        Card cardInfo = new Card();
                        byte actualRarity = (byte)CItem.RareType; // Default to what GetItems() returned
                        
                        if (IffEntry.Card.LoadCard(CItem.TypeId, ref cardInfo))
                        {
                            actualRarity = cardInfo.Rarity;
                        }
                        
                        result.Add(new Dictionary<uint, byte>
                        {
                            { CItem.TypeId, actualRarity }
                        });
                    }
                }
                return result;
            }
            finally
            {
                Clear();
            }
        }

        public TCardData GetCard(uint TypeID, uint TypeID2, uint TypeID3)
        {
            TCardData result;
            byte CQty;
            ItemRandom CItem;
            result = new TCardData();
            Card Card3 = new Card();
            Card Card2 = new Card();
            Card Card = new Card();

            if (!IffEntry.Card.LoadCard(TypeID, ref Card) || !IffEntry.Card.LoadCard(TypeID2, ref Card2) || !IffEntry.Card.LoadCard(TypeID3, ref Card3))
            {
                return result;
            }

            for (CQty = 1; CQty <= 1; CQty++)
            {
                CItem = GetItems(TypeID, TypeID2, TypeID3);
                if (CItem == null)
                {
                    CItem = GetItems();
                    result.TypeID = CItem.TypeId;
                    result.Quantity = 1;
                }
                else
                {
                    result.TypeID = CItem.TypeId;
                    result.Quantity = 1;
                }
            }
            return result;
        }

        public Dictionary<bool, Card> GetCardSPCL(UInt32 TypeID)
        {
            Card C = new Card();
            var result = new Dictionary<bool, Card>();
            if (!IffEntry.Card.LoadCard(TypeID, ref C))
            {
                return new Dictionary<bool, Card>();
            }
            result.Add(true, C);
            return result;
        }

        private void AddCardItem()
        {
            ushort GetProb(byte RareType)
            {
                switch (RareType)
                {
                    case 0:
                        return 100;
                    case 1:
                        return 6;
                    case 2:
                        return 2;
                    case 3:
                        return 1;
                    default:
                        return (ushort)GameTools.MathRand.Rand.Next(0, 120);
                }
            }

            foreach (var PZCard in ListCard)
            {
                switch (PZCard.Base.TypeID)
                {
                    case 2092957696: //{ Pangya Card Pack No.1 }
                    case 2092957697: //{ Golden Card Ticket }
                    case 2092957698: //{ Silver Card Ticket }
                    case 2092957699: //{ Bronze card ticket }
                    case 2092957700: //{ Pangya Card Pack No.2 }
                    case 2092957701:// { Card Pack No.3 }
                    case 2092957702: //{ Platinum Ticket }
                    case 2092957703: //{ Card Pack No.4 }
                    case 2092957704: //{ Grand Prix Card Pack }
                    case 2092957706: //{ Fresh Up! Card Pack }
                    case 2097152001: //{ Pangya Card Box No.2 }
                    case 2097152002: //{ Card Box No.3 }
                    case 2097152003: //{ Pangya Card Box #4 }
                    case 2084569125: //{ Unknown Name }
                    case 2084569128: //{ Unknown Name }          
                        continue;
                }

                AddItems(PZCard.Base.TypeID, 1, PZCard.Rarity, GetProb(PZCard.Rarity));
            }
        }

        private void AddPack()
        {
            // ไม่ต้องพยายามเพิ่ม ListCard เข้า ListCard (และห้ามแก้ list ตอนวน)

            // ## Pack 1
            PackData[2092957696] = new TCardPack()
            {
                CardTypePack = TypeCard.Pack1,
                Quantity = 3
            };
            // ## Golden Card Ticket
            PackData[2092957697] = new TCardPack()
            {
                CardTypePack = TypeCard.All,
                Quantity = 1
            };
            // ## Silver Card Ticket 
            PackData[2092957698] = new TCardPack()
            {
                CardTypePack = TypeCard.All,
                Quantity = 1
            };
            // ## Bronze card ticket
            PackData[2092957699] = new TCardPack()
            {
                CardTypePack = TypeCard.All,
                Quantity = 1
            };
            // ## Pack 2           
            PackData[2092957700] = new TCardPack
            {
                CardTypePack = TypeCard.Pack2,
                Quantity = 3
            };
            // ## Pack 3
            PackData[2092957701] = new TCardPack
            {
                CardTypePack = TypeCard.Pack3,
                Quantity = 3
            };
            // ## Platinum Card Ticket (SC Only)
            PackData[2092957702] = new TCardPack()
            {
                CardTypePack = TypeCard.All,
                Quantity = 1
            };
            // ## Pack 4
            PackData[2092957703] = new TCardPack
            {
                CardTypePack = TypeCard.Pack4,
                Quantity = 3
            };
            // ## Grand Prix Card Pack
            PackData[2092957704] = new TCardPack
            {
                CardTypePack = TypeCard.Rare,
                Quantity = 3
            };
            // ## FRESH UP! (SC Only - Secret Cards Rarity 3 only)
            PackData[2092957706] = new TCardPack
            {
                CardTypePack = TypeCard.SecretOnly,
                Quantity = 3
            };
            // ## Card Box No.2
            PackData[2097152001] = new TCardPack
            {
                CardTypePack = TypeCard.Pack2,
                Quantity = 3
            };
            // ## Card Box No.3
            PackData[2097152002] = new TCardPack
            {
                CardTypePack = TypeCard.Pack3,
                Quantity = 3
            };
            // ## Card Box No.4
            PackData[2097152003] = new TCardPack
            {
                CardTypePack = TypeCard.Pack4,
                Quantity = 3
            };
        }
    }
}
