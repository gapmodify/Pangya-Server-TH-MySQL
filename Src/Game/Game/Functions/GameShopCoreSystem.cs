using static PangyaFileCore.IffBaseManager;
using static Game.GameTools.Tools;
using static Game.GameTools.PacketCreator;
using System;
using System.Linq;
using Game.Client;
using Game.Defines;
using PangyaAPI;
using PangyaAPI.BinaryModels;
using Connector.DataBase;
using Game.Data;
using Game.Client.Inventory.Data;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Functions
{
    public class GameShopCoreSystem
    {
        public void PlayerEnterGameShop(GPlayer player)
        {
            // Send empty shop list to prevent crash when cards are in shop
            // Client expects shop list packet, not cancel packet
            WriteConsole.WriteLine($"[SHOP_ENTER]: Player {player.GetNickname} entering shop", ConsoleColor.Cyan);
            
            // Send proper shop enter response (0x0E 0x02 = Cancel/Empty response is OK)
            player.SendResponse(new byte[] { 0x0E, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            
            WriteConsole.WriteLine($"[SHOP_ENTER]: ✓ Shop enter packet sent", ConsoleColor.Green);
        }

        public void PlayerBuyItemGameShop(GPlayer player, Packet packet)
        {
            ShopItemRequest ShopItem;

            WriteConsole.WriteLine($"[SHOP_BUY]: ========== START ==========", ConsoleColor.Cyan);
            WriteConsole.WriteLine($"[SHOP_BUY]: Player UID={player.GetUID} ({player.GetNickname}) attempting to buy item", ConsoleColor.Cyan);

            if (!packet.ReadByte(out byte BuyType))
            {
                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]: Failed to read BuyType", ConsoleColor.Red);
                return;
            }

            if (!packet.ReadUInt16(out ushort BuyTotal))
            {
                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]: Failed to read BuyTotal", ConsoleColor.Red);
                return;
            }

            WriteConsole.WriteLine($"[SHOP_BUY]: BuyType={BuyType} ({(TGAME_SHOP_ACTION)BuyType}), Total Items={BuyTotal}", ConsoleColor.Yellow);
            WriteConsole.WriteLine($"[SHOP_BUY]: Player Pang: {player.GetPang}, Cookie: {player.GetCookie}", ConsoleColor.Yellow);

            switch ((TGAME_SHOP_ACTION)BuyType)
            {
                case TGAME_SHOP_ACTION.Normal:
                    {
                        WriteConsole.WriteLine($"[SHOP_BUY]: Processing Normal purchase...", ConsoleColor.Yellow);
                        
                        for (int Count = 0; Count <= BuyTotal - 1; Count++)
                        {
                            WriteConsole.WriteLine($"[SHOP_BUY]: --- Item #{Count + 1}/{BuyTotal} ---", ConsoleColor.Cyan);
                            
                            ShopItem = (ShopItemRequest)packet.Read(new ShopItemRequest());
                            
                            WriteConsole.WriteLine($"[SHOP_BUY]:   TypeID: {ShopItem.IffTypeId} (0x{ShopItem.IffTypeId:X})", ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   Quantity: {ShopItem.IffQty}", ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   Days: {ShopItem.IffDay}", ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   Pang Price: {ShopItem.PangPrice}", ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   Cookie Price: {ShopItem.CookiePrice}", ConsoleColor.Yellow);

                            if (!IffEntry.IsExist(ShopItem.IffTypeId))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Item does not exist in IFF!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.PASSWORD_WRONG));
                                return;
                            }
                            
                            WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Item exists in IFF", ConsoleColor.Green);
                            
                            // Special handling for Characters - allow purchase even if not marked as buyable
                            var itemGroup = GetItemGroup(ShopItem.IffTypeId);
                            var isBuyable = IffEntry.IsBuyable(ShopItem.IffTypeId);

                            WriteConsole.WriteLine($"[SHOP_BUY]:   Item Group: {itemGroup}", ConsoleColor.Yellow);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   IFF Buyable Flag: {isBuyable}", ConsoleColor.Yellow);

                            // 🎴 Special handling for CARDS - Direct purchase using GM-style transaction
                            if (itemGroup == 31) // ITEM_TYPE_CARD
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY]:   🎴 Card detected! Processing via Transaction system...", ConsoleColor.Magenta);
                                
                                // Check if this is a Card Pack (consumable) or actual collectible Card
                                bool isCardPack = ItemList.CardList.IsCardPack(ShopItem.IffTypeId);
                                
                                WriteConsole.WriteLine($"[SHOP_BUY]:   Is Card Pack: {isCardPack}", ConsoleColor.Yellow);
                                
                                // Only check duplicate for collectible cards, NOT for card packs
                                if (!isCardPack && player.Inventory.ItemCard.Any(c => c.CardTypeID == ShopItem.IffTypeId && c.CardQuantity > 0))
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Player already has this collectible card!", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }

                                // Get card price from IFF or use provided price
                                var cardPrice = IffEntry.GetPrice(ShopItem.IffTypeId, 0);
                                if (cardPrice <= 0)
                                {
                                    // If IFF price is 0, use the price from client packet
                                    cardPrice = ShopItem.PangPrice > 0 ? ShopItem.PangPrice : ShopItem.CookiePrice;
                                }

                                WriteConsole.WriteLine($"[SHOP_BUY]:   Card Price: {cardPrice}", ConsoleColor.Yellow);

                                // Check price type
                                var cardPriceType = IffEntry.GetShopPriceType(ShopItem.IffTypeId);
                                bool cardUsePang = (cardPriceType == 0 || cardPriceType == 2 || cardPriceType == 6 || cardPriceType == 32 || cardPriceType == 96);
                                bool cardUseCookie = (cardPriceType == 1 || cardPriceType == 33 || cardPriceType == 97);

                                // Default to Pang if no price type specified
                                if (!cardUsePang && !cardUseCookie)
                                {
                                    cardUsePang = ShopItem.PangPrice > 0;
                                    cardUseCookie = ShopItem.CookiePrice > 0 && !cardUsePang;
                                }

                                uint totalCost = cardPrice;

                                // Deduct currency
                                if (cardUsePang)
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   Payment: Pang (Total: {totalCost})", ConsoleColor.Yellow);

                                    if (player.GetPang < totalCost)
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Pang! Need: {totalCost}, Have: {player.GetPang}", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.PANG_NOTENOUGHT));
                                        return;
                                    }

                                    if (!player.RemovePang(totalCost))
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Failed to deduct Pang!", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                        return;
                                    }
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Pang deducted. New balance: {player.GetPang}", ConsoleColor.Green);
                                }
                                else if (cardUseCookie)
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   Payment: Cookie (Total: {totalCost})", ConsoleColor.Yellow);

                                    if (player.GetCookie < totalCost)
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Cookie! Need: {totalCost}, Have: {player.GetCookie}", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.COOKIE_NOTENOUGHT));
                                        return;
                                    }

                                    if (!player.RemoveCookie(totalCost))
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Failed to deduct Cookie!", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                        return;
                                    }
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Cookie deducted. New balance: {player.GetCookie}", ConsoleColor.Green);
                                }
                                else
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Unknown payment type!", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }

                                // Add Card to inventory using GM-style Transaction system
                                WriteConsole.WriteLine($"[SHOP_BUY]:   Adding card via Transaction...", ConsoleColor.Yellow);

                                var itemAddData = new Client.Inventory.Data.AddItem
                                {
                                    ItemIffId = ShopItem.IffTypeId,
                                    Quantity = 1,
                                    Transaction = true, // ✅ Use Transaction like GM command!
                                    Day = 0
                                };

                                var result = player.AddItem(itemAddData);

                                if (result.Status)
                                {
                                    // Send Transaction packet (like GM additem does)
                                    player.SendTransaction();
                                    
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Card 0x{ShopItem.IffTypeId:X} added via Transaction", ConsoleColor.Green);
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Item Index: {result.ItemIndex}, TypeID: {result.ItemTypeID}", ConsoleColor.Cyan);
                                    
                                    // ✅ Send final SUCCESS packet and EXIT immediately (don't send twice!)
                                    WriteConsole.WriteLine($"[SHOP_BUY]: ✅ Card purchase completed! Pang: {player.GetPang}, Cookie: {player.GetCookie}", ConsoleColor.Green);
                                    WriteConsole.WriteLine($"[SHOP_BUY]: ========== END (CARD) ==========", ConsoleColor.Cyan);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_SUCCESS, player.GetPang, (uint)player.GetCookie));
                                    return; // ✅ EXIT here - don't continue loop or send SUCCESS again!
                                }
                                else
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Failed to add card!", ConsoleColor.Red);

                                    // Refund currency
                                    if (cardUsePang)
                                    {
                                        player.AddPang(totalCost);
                                        WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Pang refunded", ConsoleColor.Yellow);
                                    }
                                    else if (cardUseCookie)
                                    {
                                        player.AddCookie(totalCost);
                                        WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Cookie refunded", ConsoleColor.Yellow);
                                    }

                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }
                            }

                            WriteConsole.WriteLine($"[SHOP_BUY]:   ⚠ Ignoring IFF buyable flag for GameShop purchase (server config)", ConsoleColor.Yellow);
                            
                            // 🎭 Special handling for CHARACTERS - Add via AddShopItem but send proper packet
                            if (itemGroup == 1) // ITEM_TYPE_CHARACTER
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY]:   🎭 Character detected! Processing purchase...", ConsoleColor.Magenta);
                                
                                // Check if already have this character
                                if (player.Inventory.ItemCharacter.Any(c => c.TypeID == ShopItem.IffTypeId))
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Player already has this character!", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }

                                // Get price
                                var charPrice = IffEntry.GetPrice(ShopItem.IffTypeId, 0);
                                if (charPrice <= 0)
                                {
                                    charPrice = ShopItem.PangPrice > 0 ? ShopItem.PangPrice : ShopItem.CookiePrice;
                                }

                                WriteConsole.WriteLine($"[SHOP_BUY]:   Character Price: {charPrice}", ConsoleColor.Yellow);

                                // Check price type
                                var charPriceType = IffEntry.GetShopPriceType(ShopItem.IffTypeId);
                                bool charUsePang = (charPriceType == 0 || charPriceType == 2 || charPriceType == 6 || charPriceType == 32 || charPriceType == 96);
                                bool charUseCookie = (charPriceType == 1 || charPriceType == 33 || charPriceType == 97);

                                if (!charUsePang && !charUseCookie)
                                {
                                    charUsePang = ShopItem.PangPrice > 0;
                                    charUseCookie = ShopItem.CookiePrice > 0 && !charUsePang;
                                }

                                uint totalCost = charPrice;

                                // Deduct currency
                                if (charUsePang)
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   Payment: Pang (Total: {totalCost})", ConsoleColor.Yellow);

                                    if (player.GetPang < totalCost)
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Pang!", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.PANG_NOTENOUGHT));
                                        return;
                                    }

                                    if (!player.RemovePang(totalCost))
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Failed to deduct Pang!", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                        return;
                                    }
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Pang deducted. New balance: {player.GetPang}", ConsoleColor.Green);
                                }
                                else if (charUseCookie)
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   Payment: Cookie (Total: {totalCost})", ConsoleColor.Yellow);

                                    if (player.GetCookie < totalCost)
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Cookie!", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.COOKIE_NOTENOUGHT));
                                        return;
                                    }

                                    if (!player.RemoveCookie(totalCost))
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Failed to deduct Cookie!", ConsoleColor.Red);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                        return;
                                    }
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Cookie deducted. New balance: {player.GetCookie}", ConsoleColor.Green);
                                }
                                else
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Unknown payment type!", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }

                                // Add Character to database
                                WriteConsole.WriteLine($"[SHOP_BUY]:   Adding character to database...", ConsoleColor.Yellow);

                                var itemAddData = new Client.Inventory.Data.AddItem
                                {
                                    ItemIffId = ShopItem.IffTypeId,
                                    Quantity = 1,
                                    Transaction = false,
                                    Day = 0
                                };

                                var result = player.AddItem(itemAddData);

                                if (result.Status)
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Character 0x{ShopItem.IffTypeId:X} added to DB, CID={result.ItemIndex}", ConsoleColor.Green);
                                    
                                    // Get the character data that was just added
                                    var newCharacter = player.Inventory.ItemCharacter.GetChar(result.ItemIndex, CharType.bIndex);
                                    
                                    if (newCharacter != null)
                                    {
                                        WriteConsole.WriteLine($"[SHOP_BUY]:   Building character data packet...", ConsoleColor.Yellow);
                                        
                                        // Build 0xAA packet with character data (513 bytes)
                                        using (var charPacket = new PangyaBinaryWriter())
                                        {
                                            charPacket.Write(new byte[] { 0xAA, 0x00 }); // ShowBuyItem header
                                            charPacket.WriteUInt16(1); // Item count = 1
                                            
                                            // Write full character data (513 bytes)
                                            var charData = player.Inventory.ItemCharacter.CreateChar(newCharacter, player.Inventory.ItemCharacter.Card.MapCard(newCharacter.Index));
                                            charPacket.Write(charData);
                                            
                                            // Write currency at the end
                                            charPacket.WriteUInt64(player.GetPang);
                                            charPacket.WriteUInt64((uint)player.GetCookie);
                                            
                                            WriteConsole.WriteLine($"[SHOP_BUY]:   Character Packet Size: {charPacket.GetBytes().Length} bytes", ConsoleColor.Cyan);
                                            player.SendResponse(charPacket.GetBytes());
                                            WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Character packet sent", ConsoleColor.Green);
                                        }
                                    }
                                    else
                                    {
                                        // Fallback: Send standard success
                                        WriteConsole.WriteLine($"[SHOP_BUY]:   ⚠ Character not found in memory, sending standard response", ConsoleColor.Yellow);
                                        player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_SUCCESS, player.GetPang, (uint)player.GetCookie));
                                    }
                                    
                                    WriteConsole.WriteLine($"[SHOP_BUY]: ✅ Character purchase completed! Pang: {player.GetPang}, Cookie: {player.GetCookie}", ConsoleColor.Green);
                                    WriteConsole.WriteLine($"[SHOP_BUY]: ========== END (CHARACTER) ==========", ConsoleColor.Cyan);
                                    return;
                                }
                                else
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Failed to add character!", ConsoleColor.Red);

                                    // Refund
                                    if (charUsePang)
                                    {
                                        player.AddPang(totalCost);
                                        WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Pang refunded", ConsoleColor.Yellow);
                                    }
                                    else if (charUseCookie)
                                    {
                                        player.AddCookie(totalCost);
                                        WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Cookie refunded", ConsoleColor.Yellow);
                                    }

                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }
                            }
                            
                            if (!player.Inventory.Available(ShopItem.IffTypeId, ShopItem.IffQty))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Inventory not available or already have item!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                return;
                            }
                            
                            WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Inventory available", ConsoleColor.Green);
                            
                            // ✅ ใช้ราคาจาก IFF เป็นหลัก (Server config)
                            var itemPrice = IffEntry.GetPrice(ShopItem.IffTypeId, ShopItem.IffDay);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   IFF Price: {itemPrice}", ConsoleColor.Yellow);
                            
                            // ถ้า IFF ไม่มีราคา ให้ใช้ราคาจาก Client Packet
                            bool usePang = false;
                            bool useCookie = false;
                            
                            if (itemPrice <= 0)
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY]:   ⚠ IFF price is 0, using client packet price...", ConsoleColor.Yellow);
                                
                                if (ShopItem.PangPrice > 0)
                                {
                                    itemPrice = ShopItem.PangPrice;
                                    usePang = true;
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   Using Pang Price from packet: {itemPrice}", ConsoleColor.Green);
                                }
                                else if (ShopItem.CookiePrice > 0)
                                {
                                    itemPrice = ShopItem.CookiePrice;
                                    useCookie = true;
                                    WriteConsole.WriteLine($"[SHOP_BUY]:   Using Cookie Price from packet: {itemPrice}", ConsoleColor.Green);
                                }
                                else
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   No valid price found! IFF=0, Pang=0, Cookie=0", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                    return;
                                }
                            }
                            else
                            {
                                // มีราคาจาก IFF - ตรวจสอบว่าเป็น Pang หรือ Cookie
                                var priceType = IffEntry.GetShopPriceType(ShopItem.IffTypeId);
                                usePang = (priceType == 0 || priceType == 2 || priceType == 6 || priceType == 32 || priceType == 96);
                                useCookie = (priceType == 1 || priceType == 33 || priceType == 97);
                                
                                WriteConsole.WriteLine($"[SHOP_BUY]:   Price Type: {priceType} → {(usePang ? "Pang" : useCookie ? "Cookie" : "Unknown")}", ConsoleColor.Yellow);
                            }
                            
                            // ✅ ราคารวม = ราคาต่อชิ้น (ไม่คูณ Quantity เพราะราคาที่ได้มาคือราคาสำหรับจำนวนนั้นแล้ว)
                            uint totalPrice = itemPrice;
                            WriteConsole.WriteLine($"[SHOP_BUY]:   Total Price: {totalPrice} ({(usePang ? "Pang" : "Cookie")})", ConsoleColor.Cyan);
                            
                            // หักเงิน
                            if (usePang)
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY]:   Payment: Pang (Total: {totalPrice})", ConsoleColor.Yellow);

                                if (!player.RemovePang(totalPrice))
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Pang! Need: {totalPrice}, Have: {player.GetPang}", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.PANG_NOTENOUGHT));
                                    return;
                                }
                                WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Pang deducted. New balance: {player.GetPang}", ConsoleColor.Green);
                            }
                            else if (useCookie)
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY]:   Payment: Cookie (Total: {totalPrice})", ConsoleColor.Yellow);

                                if (!player.RemoveCookie(totalPrice))
                                {
                                    WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Cookie! Need: {totalPrice}, Have: {player.GetCookie}", ConsoleColor.Red);
                                    player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.COOKIE_NOTENOUGHT));
                                    return;
                                }
                                WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Cookie deducted. New balance: {player.GetCookie}", ConsoleColor.Green);
                            }
                            else
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Unknown payment type!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                                return;
                            }

                            WriteConsole.WriteLine($"[SHOP_BUY]:   Adding item to inventory...", ConsoleColor.Yellow);
                            AddShopItem(player, ShopItem);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Item added successfully", ConsoleColor.Green);
                        }
                    }
                    break;
                case TGAME_SHOP_ACTION.Rental:
                    {
                        WriteConsole.WriteLine($"[SHOP_BUY]: Processing Rental purchase...", ConsoleColor.Yellow);
                        
                        for (int Count = 0; Count <= BuyTotal - 1; Count++)
                        {
                            WriteConsole.WriteLine($"[SHOP_BUY]: --- Rental Item #{Count + 1}/{BuyTotal} ---", ConsoleColor.Cyan);

                            ShopItem = (ShopItemRequest)packet.Read(new ShopItemRequest());
                            
                            WriteConsole.WriteLine($"[SHOP_BUY]:   TypeID: {ShopItem.IffTypeId} (0x{ShopItem.IffTypeId:X})", ConsoleColor.Yellow);

                            if (!(GetItemGroup(ShopItem.IffTypeId) == 2))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not a Part item! Group: {GetItemGroup(ShopItem.IffTypeId)}", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.ITEM_CANNOT_PURCHASE));
                                return;
                            }

                            if (!IffEntry.IsExist(ShopItem.IffTypeId))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Item does not exist in IFF!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.PASSWORD_WRONG));
                                return;
                            }
                            if (!IffEntry.IsBuyable(ShopItem.IffTypeId))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Item is not buyable!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.CANNOT_BUY_ITEM));
                                return;
                            }
                            if (player.Inventory.IsExist(ShopItem.IffTypeId))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Already have this rental item!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.ITEM_CANNOT_PURCHASE));
                                return;
                            }
                            
                            uint rentalPrice = IffEntry.GetRentalPrice(ShopItem.IffTypeId);

                            WriteConsole.WriteLine($"[SHOP_BUY]:   Rental Price: {rentalPrice}", ConsoleColor.Yellow);

                            if (rentalPrice <= 0)
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Invalid rental price!", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.ITEM_CANNOT_PURCHASE));
                                return;
                            }
                            if (!player.RemovePang(rentalPrice))
                            {
                                WriteConsole.WriteLine($"[SHOP_BUY_ERROR]:   Not enough Pang for rental! Need: {rentalPrice}, Have: {player.GetPang}", ConsoleColor.Red);
                                player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.PANG_NOTENOUGHT));
                            }
                            
                            WriteConsole.WriteLine($"[SHOP_BUY]:   Adding rental item...", ConsoleColor.Yellow);
                            AddShopRentItem(player, ShopItem);
                            WriteConsole.WriteLine($"[SHOP_BUY]:   ✓ Rental item added", ConsoleColor.Green);
                        }
                    }
                    break;
            }
            
            WriteConsole.WriteLine($"[SHOP_BUY]: Sending success response...", ConsoleColor.Green);
            player.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_SUCCESS, player.GetPang, (uint)player.GetCookie));
            WriteConsole.WriteLine($"[SHOP_BUY]: ✅ Purchase completed! New Pang: {player.GetPang}, Cookie: {player.GetCookie}", ConsoleColor.Green);
            WriteConsole.WriteLine($"[SHOP_BUY]: ========== END ==========", ConsoleColor.Cyan);
        }

       
        void AddShopItem(GPlayer PL, ShopItemRequest shop)
        {
            WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: START - TypeID: 0x{shop.IffTypeId:X}", ConsoleColor.Cyan);
            
            var itemGroup = GetItemGroup(shop.IffTypeId);
            WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Item Group: {itemGroup}", ConsoleColor.Yellow);
            
            var ListSet = IffEntry.SetItem.SetList(shop.IffTypeId);
            AddData ItemAddedData;
            AddItem ItemAddData;
            TBuyItem DataBuy;
            
            //group set item
            if (itemGroup == 9)
            {
                WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Processing SET ITEM...", ConsoleColor.Yellow);
                
                if (ListSet.Count <= 0)// ## should not be happened
                {
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM_ERROR]: Empty set list!", ConsoleColor.Red);
                    PL.SendResponse(ShowBuyItemSucceed(TGAME_SHOP.BUY_FAIL));
                    return;
                }
                else
                {
                    foreach (var datas in ListSet)
                    {
                        ItemAddData = new AddItem
                        {
                            ItemIffId = datas.FirstOrDefault().Key,
                            Quantity = datas.FirstOrDefault().Value,
                            Transaction = false,
                            Day = 0
                        };
                        ItemAddedData = PL.AddItem(ItemAddData);
                        DataBuy = CheckData(ItemAddedData);
                        PL.SendResponse(ShowBuyItem(ItemAddedData, DataBuy, PL.GetPang, (uint)PL.GetCookie));
                    }
                }
            }
            else
            {
                WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Processing SINGLE ITEM...", ConsoleColor.Yellow);
                WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Quantity: {shop.IffQty}, Days: {shop.IffDay}", ConsoleColor.Yellow);
                
                var realQty = IffEntry.GetRealQuantity(shop.IffTypeId, shop.IffQty);
                WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Real Quantity: {realQty}", ConsoleColor.Yellow);
                
                ItemAddData = new AddItem
                {
                    ItemIffId = shop.IffTypeId,
                    Quantity = realQty,
                    Transaction = false,
                    Day = shop.IffDay
                };
                
                WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Calling PL.AddItem()...", ConsoleColor.Yellow);
                try
                {
                    ItemAddedData = PL.AddItem(ItemAddData);
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: ✓ Item added to inventory", ConsoleColor.Green);
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Added TypeID: 0x{ItemAddedData.ItemTypeID:X}", ConsoleColor.Green);
                    
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Calling CheckData()...", ConsoleColor.Yellow);
                    DataBuy = CheckData(ItemAddedData);
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: ✓ CheckData completed - Flag: {DataBuy.Flag}, DayTotal: {DataBuy.DayTotal}", ConsoleColor.Green);
                    
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: Sending ShowBuyItem packet...", ConsoleColor.Yellow);
                    PL.SendResponse(ShowBuyItem(ItemAddedData, DataBuy, PL.GetPang, (uint)PL.GetCookie));
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: ✓ Packet sent successfully", ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM_ERROR]: Exception occurred!", ConsoleColor.Red);
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM_ERROR]: {ex.Message}", ConsoleColor.Red);
                    WriteConsole.WriteLine($"[ADD_SHOP_ITEM_ERROR]: {ex.StackTrace}", ConsoleColor.Red);
                    throw;
                }
            }
            
            WriteConsole.WriteLine($"[ADD_SHOP_ITEM]: END", ConsoleColor.Cyan);
        }

        void AddShopRentItem(GPlayer PL, ShopItemRequest ShopItem)
        {
            AddData ItemAddedData;
            TBuyItem DataBuy;
            ItemAddedData = PL.Inventory.AddRent(ShopItem.IffTypeId);

            DataBuy = new TBuyItem
            {
                Flag = 0x6,
                DayTotal = 0x7,
                EndDate = null
            };
            
            PL.SendResponse(ShowBuyItem(ItemAddedData, DataBuy, PL.GetPang, (uint)PL.GetCookie));
        }

        TBuyItem CheckData(AddData AddData)
        {
            TBuyItem Result;

            switch ((TITEMGROUP)GetItemGroup(AddData.ItemTypeID))
            {
                case TITEMGROUP.ITEM_TYPE_CHARACTER:
                    {
                        Result = new TBuyItem
                        {
                            Flag = 0,
                            DayTotal = 0,
                            EndDate = null
                        };
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_CADDIE:
                    {
                        if (AddData.ItemEndDate != null && AddData.ItemEndDate > DateTime.Now)
                        {
                            Result = new TBuyItem
                            {
                                Flag = 4,
                                DayTotal = (ushort)(DaysBetween(AddData.ItemEndDate, DateTime.Now) + 1),
                                EndDate = AddData.ItemEndDate
                            };
                        }
                        else
                        {
                            Result = new TBuyItem
                            {
                                Flag = 0,
                                DayTotal = 0,
                                EndDate = null
                            };
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_CADDIE_ITEM:
                    {
                        if (AddData.ItemEndDate != null)
                        {
                            Result = new TBuyItem
                            {
                                Flag = 4,
                                DayTotal = (ushort)(DaysBetween(AddData.ItemEndDate, DateTime.Now) * 24),
                                EndDate = AddData.ItemEndDate
                            };
                        }
                        else
                        {
                            Result = new TBuyItem
                            {
                                Flag = 0,
                                DayTotal = 0,
                                EndDate = null
                            };
                        }

                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_SKIN:
                    {
                        if (AddData.ItemEndDate != null)
                        {
                            Result = new TBuyItem
                            {
                                Flag = 4,
                                DayTotal = (ushort)(DaysBetween(AddData.ItemEndDate, DateTime.Now) + 1),
                                EndDate = AddData.ItemEndDate
                            };
                        }
                        else
                        {
                            Result = new TBuyItem
                            {
                                Flag = 0,
                                DayTotal = 0,
                                EndDate = null
                            };
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_MASCOT:
                    {
                        if (AddData.ItemEndDate > DateTime.Now)
                        {
                            Result = new TBuyItem
                            {
                                Flag = 4,
                                DayTotal = (ushort)(DaysBetween(AddData.ItemEndDate, DateTime.Now) + 1),
                                EndDate = AddData.ItemEndDate
                            };
                        }
                        else
                        {
                            Result = new TBuyItem
                            {
                                Flag = 0,
                                DayTotal = 0,
                                EndDate = null
                            };
                        }
                    }
                    break;
                case TITEMGROUP.ITEM_TYPE_CARD:
                    {
                        // Cards are permanent items with no expiration
                        Result = new TBuyItem
                        {
                            Flag = 0,
                            DayTotal = 0,
                            EndDate = null
                        };
                    }
                    break;              
                default:
                    {
                        Result = new TBuyItem
                        {
                            Flag = 0,
                            DayTotal = 0,
                            EndDate = null
                        };
                    }
                    break;
            }
            return Result;
        }       
    }
}
