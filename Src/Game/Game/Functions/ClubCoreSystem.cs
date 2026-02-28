using PangyaAPI;
using static Game.Data.ClubData;
using System;
using Game.Client;
using Game.Defines;
using static Game.GameTools.PacketCreator;
using static Game.GameTools.Tools;
using static Game.GameTools.ErrorCode;
using static System.Math;
using Game.Client.Inventory.Data.Warehouse;
using Game.Client.Data;
using Game.Client.Inventory.Data.Transactions;
using Game.Client.Inventory.Data;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;
using Connector.DataBase;

namespace Game.Functions
{
    public class ClubSystem
    {
        // ✅ Helper: Get current stat value for a slot
        private int GetCurrentStat(PlayerItemData club, TCLUB_STATUS slot)
        {
            switch (slot)
            {
                case TCLUB_STATUS.csPower:   return club.ItemC0;
                case TCLUB_STATUS.csControl: return club.ItemC1;
                case TCLUB_STATUS.csImpact:  return club.ItemC2;
                case TCLUB_STATUS.csSpin:    return club.ItemC3;
                case TCLUB_STATUS.csCurve:   return club.ItemC4;
                default: return 0;
            }
        }
        
        // ✅ Helper: Calculate upgrade cost for a specific level
        private uint CalculateUpgradeCost(TCLUB_STATUS slot, int currentLevel)
        {
            const uint Power = 2100, Con = 1700, Impact = 2400, Spin = 1900, Curve = 1900;
            
            uint baseCost = 0;
            switch (slot)
            {
                case TCLUB_STATUS.csPower:   baseCost = Power; break;
                case TCLUB_STATUS.csControl: baseCost = Con; break;
                case TCLUB_STATUS.csImpact:  baseCost = Impact; break;
                case TCLUB_STATUS.csSpin:    baseCost = Spin; break;
                case TCLUB_STATUS.csCurve:   baseCost = Curve; break;
            }
            
            // Cost = baseCost * (currentLevel + 1)
            return baseCost * (uint)(currentLevel + 1);
        }
        
        public void PlayerUpgradeClubSlot(GPlayer player, Packet packet)
        {
            WriteConsole.WriteLine($"[CLUB_UPGRADE_SLOT_PACKET] Packet Size: {packet.GetSize}, Current Pos: {packet.GetPos}", ConsoleColor.Yellow);
            
            var remainingData = packet.GetRemainingData;
            if (remainingData != null && remainingData.Length > 0)
            {
                WriteConsole.WriteLine($"[CLUB_UPGRADE_SLOT_PACKET] Remaining Data ({remainingData.Length} bytes): {BitConverter.ToString(remainingData).Replace("-", " ")}", ConsoleColor.Yellow);
            }
            
            TCLUB_ACTION Action = (TCLUB_ACTION)packet.ReadByte();
            packet.Skip(1); // ✅ ข้าม 1 byte (0x46) ที่ไม่รู้จัก
            TCLUB_STATUS Slot = (TCLUB_STATUS)packet.ReadByte();
            uint ClubIndex = packet.ReadUInt32(); // ✅ PacketValue คือ ClubIndex ที่จะอัปเกรด

            WriteConsole.WriteLine($"[CLUB_UPGRADE_SLOT_DEBUG] Player: {player.GetUID}, Action: {Action} ({(byte)Action}), Slot: {Slot} ({(byte)Slot}), ClubIndex: {ClubIndex} (0x{ClubIndex:X})", ConsoleColor.Cyan);
            
            // ✅ ดึงไม้จาก Warehouse โดยใช้ ClubIndex ที่ Client ส่งมา
            var Club = player.Inventory.ItemWarehouse.GetClub(ClubIndex, TGET_CLUB.gcIndex);

            if (Club == null)
            {
                WriteConsole.WriteLine($"[CLUB_UPGRADE_SLOT_ERROR] Club not found! Index: {ClubIndex}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[CLUB_UPGRADE_SLOT_ERROR] Warehouse items count: {player.Inventory.ItemWarehouse.Count}", ConsoleColor.Red);
                player.SendResponse(new byte[] { 0xA5, 0x00, 0x04 });
                return;
            }
            
            WriteConsole.WriteLine($"[CLUB_UPGRADE_SLOT_DEBUG] Club found: TypeID: 0x{Club.ItemTypeID:X}, Index: {Club.ItemIndex}", ConsoleColor.Green);
            switch (Action)
            {
                case TCLUB_ACTION.Upgrade:
                    {
                        var GetClub = Club.ClubSlotAvailable(Slot);

                        if (!GetClub.Able)
                        {
                            WriteConsole.WriteLine("PLAYER_CLUB_UPGRADE_FALIED 1");
                            player.SendResponse(new byte[] { 0xA5, 0x00, 0x04 });
                            return;
                        }
                        if (!player.RemovePang(GetClub.Pang))
                        {
                            WriteConsole.WriteLine("PLAYER_CLUB_UPGRADE_FALIED 2");
                            player.SendResponse(new byte[] { 0xA5, 0x00, 0x03 });
                            return;
                        }

                        if (Club.ClubAddStatus(Slot))
                        {
                            Club.ItemClubPangLog = Club.ItemClubPangLog += GetClub.Pang;
                            GetClub.Pang = (uint)Club.ItemClubPangLog;
                            
                            // ✅ ตั้งค่า flag เพื่อบอกให้ save ลง DB
                            Club.ItemNeedUpdate = true;
                            
                            player.Inventory.ItemWarehouse.Update(Club);
                            
                            // ✅ บันทึกลง Database
                            try
                            {
                                using (var db = DbContextFactory.Create())
                                {
                                    player.Inventory.Save(db);
                                    WriteConsole.WriteLine($"[CLUB_UPGRADE] ✅ Saved to DB - Club Index:{Club.ItemIndex}, Slot:{Slot}, Stats: P={Club.ItemC0},C={Club.ItemC1},I={Club.ItemC2},S={Club.ItemC3},Cv={Club.ItemC4}, Slots: P={Club.ItemC0Slot},C={Club.ItemC1Slot},I={Club.ItemC2Slot}", ConsoleColor.Green);
                                }
                            }
                            catch (Exception saveEx)
                            {
                                WriteConsole.WriteLine($"[CLUB_UPGRADE] ⚠ DB save failed: {saveEx.Message}", ConsoleColor.Yellow);
                            }

                            player.Write(ShowClubStatus(TCLUB_ACTION.Upgrade, TCLUB_ACTION.Upgrade, Slot, Club.ItemIndex, GetClub.Pang));
                            player.SendPang();
                        }
                    }
                    break;
                case TCLUB_ACTION.Downgrade:
                    {
                        // ✅ ดึงข้อมูล stat ปัจจุบันก่อน downgrade
                        int currentStat = GetCurrentStat(Club, Slot);
                        
                        WriteConsole.WriteLine($"[CLUB_DOWNGRADE_DEBUG] Slot:{Slot}, Current Stat:{currentStat}, Pang Log:{Club.ItemClubPangLog}", ConsoleColor.Cyan);
                        
                        // ✅ ตรวจสอบว่า Stat ปัจจุบัน > 0 (มีอะไรให้ลดหรือไม่)
                        if (currentStat <= 0)
                        {
                            WriteConsole.WriteLine($"[CLUB_DOWNGRADE] ✗ Cannot downgrade - Stat is already 0!", ConsoleColor.Red);
                            player.SendResponse(new byte[] { 0xA5, 0x00, 0x04 });
                            return;
                        }
                        
                        // ✅ คำนวณเงินที่จะคืน = ค่าใช้จ่ายใน upgrade ครั้งล่าสุด (currentLevel)
                        // ตัวอย่าง: Power level 5 → จ่ายครั้งล่าสุด = 2100 * 5 = 10,500
                        uint refundPang = CalculateUpgradeCost(Slot, currentStat);
                        
                        WriteConsole.WriteLine($"[CLUB_DOWNGRADE] Current Level:{currentStat} → Refunding {refundPang} Pang to player", ConsoleColor.Yellow);
                        
                        // ✅ ลดค่าสถิติก่อน (เหมือน Upgrade ที่ ClubAddStatus ก่อน)
                        if (Club.ClubRemoveStatus(Slot))
                        {
                            // ✅ ลด Pang Log ตามจำนวนเงินที่คืน
                            if (Club.ItemClubPangLog >= refundPang)
                            {
                                Club.ItemClubPangLog -= refundPang;
                            }
                            else
                            {
                                WriteConsole.WriteLine($"[CLUB_DOWNGRADE] ⚠ PangLog ({Club.ItemClubPangLog}) < Refund ({refundPang}), setting to 0", ConsoleColor.Yellow);
                                Club.ItemClubPangLog = 0;
                            }
                            
                            // ✅ คืนเงินให้ player
                            player.AddPang(refundPang);
                            
                            // ✅ ตั้งค่า flag เพื่อบอกให้ save ลง DB
                            Club.ItemNeedUpdate = true;
                            
                            player.Inventory.ItemWarehouse.Update(Club);
                            
                            // ✅ บันทึกลง Database
                            try
                            {
                                using (var db = DbContextFactory.Create())
                                {
                                    player.Inventory.Save(db);
                                    WriteConsole.WriteLine($"[CLUB_DOWNGRADE] ✅ Saved to DB - Club Index:{Club.ItemIndex}, Slot:{Slot}, Stats: P={Club.ItemC0},C={Club.ItemC1},I={Club.ItemC2},S={Club.ItemC3},Cv={Club.ItemC4}, Slots: P={Club.ItemC0Slot},C={Club.ItemC1Slot},I={Club.ItemC2Slot}, PangLog:{Club.ItemClubPangLog}", ConsoleColor.Green);
                                }
                            }
                            catch (Exception saveEx)
                            {
                                WriteConsole.WriteLine($"[CLUB_DOWNGRADE] ⚠ DB save failed: {saveEx.Message}", ConsoleColor.Yellow);
                            }

                            // ✅ ส่ง packet กลับไปให้ client พร้อมจำนวนเงินที่คืน
                            player.SendResponse(ShowClubStatus(TCLUB_ACTION.Decrement, TCLUB_ACTION.Downgrade, Slot, Club.ItemIndex, refundPang));
                            player.SendPang();
                            
                            WriteConsole.WriteLine($"[CLUB_DOWNGRADE] ✅ Success - Refunded {refundPang} Pang, New PangLog: {Club.ItemClubPangLog}", ConsoleColor.Green);
                        }
                        else
                        {
                            WriteConsole.WriteLine($"[CLUB_DOWNGRADE] ✗ ClubRemoveStatus failed!", ConsoleColor.Red);
                            player.SendResponse(new byte[] { 0xA5, 0x00, 0x04 });
                        }
                    }
                    break;
                default:
                    {
                        WriteConsole.WriteLine("PLAYER_CLUB_ACTION_UNKNOWN");
                        player.SendResponse(new byte[] { 0xA5, 0x00, 0x04 });
                    }
                    break;
            }
        }

        public void PlayerClubUpgrade(GPlayer player, Packet packet)
        {
            var ItemTypeID = packet.ReadUInt32();
            var ItemQty = packet.ReadUInt16();
            var ClubIndex = packet.ReadUInt32();

            bool Check()
            {
                return ((ItemTypeID == 0x1A00020F) || (ItemTypeID == 0x7C800026 && ItemQty > 0));
            }

            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x3D, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }

            if (!Check())
            {
                SendCode(READ_PACKET_ERROR);
                return;
            }

            var Club = player.Inventory.ItemWarehouse.GetClub(ClubIndex, TGET_CLUB.gcIndex);

            if (Club == null)
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }

            var RemoveItem = player.Inventory.Remove(ItemTypeID, ItemQty, true);

            if (!RemoveItem.Status)
            {
                SendCode(REMOVE_ITEM_FAIL);
                return;
            }

            var ClubInfo = Club.GetClubSlotStatus();
            ClubInfo = PlayerGetClubSlotLeft(Club.ItemTypeID, ClubInfo);
            var GetType = PlayerGetSlotUpgrade(ItemTypeID, ItemQty, ClubInfo);

            if (GetType <= -1)
            {
                SendCode(CLUBSET_SLOT_FULL);
            }

            if (!(Club.ClubAddStatus((TCLUB_STATUS)GetType)))
            {
                SendCode(CLUBSET_SLOT_FULL);
            }

            player.ClubTemporary.PClub = Club;
            player.ClubTemporary.UpgradeType = GetType;
            player.ClubTemporary.Count = 1;

            player.Response.Write(new byte[] { 0x3D, 0x02 });
            player.Response.Write(0);
            player.Response.Write((int)GetType);
            player.SendResponse();

        }

        public void PlayerUpgradeClubAccept(GPlayer player)
        {
            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x3E, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }
            if ((player.ClubTemporary.PClub == null))
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem(player.ClubTemporary.PClub);
            player.SendTransaction();

            player.Response.Write(new byte[] { 0x3E, 0x02 });
            player.Response.WriteUInt32(0);
            player.Response.WriteUInt32((uint)player.ClubTemporary.UpgradeType);
            player.Response.WriteUInt32(((PlayerItemData)player.ClubTemporary.PClub).ItemIndex);
            player.SendResponse();

            player.Inventory.ItemWarehouse.Update(((PlayerItemData)player.ClubTemporary.PClub));
            //Limpar
            player.ClubTemporary.Clear();
        }

        public void PlayerUpgradeClubCancel(GPlayer player)
        {
            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x3F, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }

            if ((player.ClubTemporary.PClub == null))
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }
            if (((PlayerItemData)player.ClubTemporary.PClub).ItemClubSlotCancelledCount >= 5)
            {
                SendCode(CLUBSET_CANNOT_CANCEL);
                return;
            }
            if (!(((PlayerItemData)player.ClubTemporary.PClub).ClubRemoveStatus((TCLUB_STATUS)player.ClubTemporary.UpgradeType)))
            {
                SendCode(CLUBSET_FAIL_CANCEL);
                return;
            }
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem((PlayerItemData)player.ClubTemporary.PClub);
            player.SendTransaction();

            player.Response.Write(new byte[] { 0x3F, 0x02 });
            player.Response.Write(0);
            player.Response.WriteUInt32(((PlayerItemData)player.ClubTemporary.PClub).ItemIndex);
            player.SendResponse();
        }

        public void PlayerUpgradeRank(GPlayer player, Packet packet)
        {
            var ItemTypeID = packet.ReadUInt32();
            var ItemQty = packet.ReadUInt16();
            var ClubIndex = packet.ReadUInt32();

            TClubUpgradeRank UpgradeInfo;
            PlayerItemData Club;
            sbyte GetType;
            AddData RemoveItem;

            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x40, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }
            bool Check()
            {
                bool result;
                result = (ItemTypeID == 0x7C800041);
                return result;
            }

            if (!Check())
            {
                SendCode(READ_PACKET_ERROR);
                return;
            }
            Club = player.Inventory.ItemWarehouse.GetItem(ClubIndex, TGET_CLUB.gcIndex);
            if ((Club == null) || (!(GetItemGroup(Club.ItemTypeID) == 0x4)))
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }

            UpgradeInfo = PlayerGetClubRankUPData(Club.ItemTypeID, Club.GetClubSlotStatus());
            if (UpgradeInfo.ClubPoint <= 0)
            {
                SendCode(CLUBSET_NOT_ENOUGHT_POINT_FOR_UPGRADE);
                // TODO: This must be showned as cannot rank up anymore
                return;
            }
            GetType = PlayerGetSlotUpgrade(ItemTypeID, ItemQty, UpgradeInfo.ClubSlotLeft);
            if (GetType <= -1)
            {
                SendCode(CLUBSET_CANNOT_ADD_SLOT);
                return;
            }
            // /* remove soren card */
            RemoveItem = player.Inventory.Remove(ItemTypeID, ItemQty, true);
            if (!RemoveItem.Status)
            {
                SendCode(REMOVE_ITEM_FAIL);
                return;
            }
            if (!Club.RemoveClubPoint(UpgradeInfo.ClubPoint))
            {
                SendCode(CLUBSET_NOT_ENOUGHT_POINT_FOR_UPGRADE);
                return;
            }
            // Add To Log
            Club.ItemClubPointLog += UpgradeInfo.ClubPoint;
            if (!Club.ClubAddStatus((TCLUB_STATUS)GetType))
            {
                SendCode(CLUBSET_CANNOT_ADD_SLOT);
                return;
            }
            // * this is used for add club slot when rank is up to Special *
            if (UpgradeInfo.ClubCurrentRank >= 4)
            {
                if (!Club.ClubAddStatus((TCLUB_STATUS)UpgradeInfo.ClubSPoint))
                {
                    SendCode(CLUBSET_CANNOT_ADD_SLOT);
                    return;
                }
            }
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem(Club);
            player.SendTransaction();

            player.Response.Write(new byte[] { 0x40, 0x02 });
            player.Response.Write(0);
            player.Response.WriteUInt32((uint)GetType);
            player.Response.WriteUInt32(Club.ItemIndex);
            player.SendResponse();
        }

        public void PlayerUseAbbot(GPlayer player, Packet packet)
        {
            var SupplyTypeID = packet.ReadUInt32();
            var ClubIndex = packet.ReadUInt32();

            PlayerItemData ClubInfo;
            AddData RemoveItem;

            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x46, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }
            bool Check()
            {
                bool result;
                result = SupplyTypeID == 0x1A000210;
                return result;
            }


            if (!Check())
            {
                SendCode(READ_PACKET_ERROR);
                return;
            }
            ClubInfo = player.Inventory.ItemWarehouse.GetItem(ClubIndex, TGET_CLUB.gcIndex);
            if ((ClubInfo == null) || (!(GetItemGroup(ClubInfo.ItemTypeID) == 0x4)))
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }
            if (ClubInfo.ItemClubSlotCancelledCount <= 0)
            {
                SendCode(CLUBSET_ABBOT_NOT_READY);
                return;
            }
            RemoveItem = player.Inventory.Remove(SupplyTypeID, 1, true);
            if (!RemoveItem.Status)
            {
                SendCode(REMOVE_ITEM_FAIL);
                return;
            }
            // ## reset
            ClubInfo.ItemClubSlotCancelledCount = 0;
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem(ClubInfo);
            player.SendTransaction();

            SendCode(Zero);
        }

        public void PlayerUseClubPowder(GPlayer player, Packet packet)
        {
            var SupplyTypeID = packet.ReadUInt32();
            var ClubIndex = packet.ReadUInt32();
            PlayerItemData ClubInfo;
            AddData RemoveItem;
            PlayerTransactionData Tran;

            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x47, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }
            // 47 02 00 1A,436208199=Titan Boo Powder L
            // 4B 02 00 1A,436208203=Titan Boo Powder H
            bool Check()
            {
                bool result;
                result = (SupplyTypeID == 0x1A00024B) || (SupplyTypeID == 0x1A000247);
                return result;
            }

            if (!Check())
            {
                SendCode(READ_PACKET_ERROR);
                return;
            }
            ClubInfo = player.Inventory.ItemWarehouse.GetClub(ClubIndex, TGET_CLUB.gcIndex);
            if ((ClubInfo == null))
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }
            if (!ClubInfo.ClubSetCanReset())
            {
                SendCode(CLUBSET_CANNOT_CANCEL);
                return;
            }
            RemoveItem = player.Inventory.Remove(SupplyTypeID, 1, true);
            if (!RemoveItem.Status)
            {
                SendCode(REMOVE_ITEM_FAIL);
                return;
            }
            if (SupplyTypeID == 0x1A00024B)
            {
                player.AddPang((uint)Round(Convert.ToDouble(ClubInfo.ItemClubPangLog / 2)));
                player.SendPang();

                ClubInfo.ItemClubPoint += (uint)Round(Convert.ToDouble(ClubInfo.ItemClubPointLog / 2));
            }
            // Reset club point
            ClubInfo.ClubSetReset();
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem(ClubInfo);
            Tran = new PlayerTransactionData
            {
                Types = 0xC9,
                TypeID = ClubInfo.ItemTypeID,
                Index = ClubInfo.ItemIndex,
                PreviousQuan = 0,
                NewQuan = 0,
                UCC = ""
            };
            // ## add transaction
            player.Inventory.ItemTransaction.Add(Tran);
            player.SendTransaction();

            player.Response.Write(new byte[] { 0x47, 0x02 });
            player.Response.Write(0);
            player.Response.WriteUInt32(ClubInfo.ItemTypeID);
            player.Response.WriteUInt32(ClubInfo.ItemIndex);
            player.SendResponse();
        }

        public void PlayerTransferClubPoint(GPlayer player, Packet packet)
        {
            var SupplyTypeID = packet.ReadUInt32();
            var ClubIndexFrom = packet.ReadUInt32();
            var ClubIndexTo = packet.ReadUInt32();
            var Quantity = packet.ReadUInt32();
            PlayerItemData ClubToMove;
            PlayerItemData ClubMoveTo;
            AddData RemoveItem;
            uint TotalPoint;
            const UInt16 ItemMovePoint = 300;

            void SendCode(byte[] Code)
            {
                player.Response.Write(new byte[] { 0x45, 0x02 });
                player.Response.Write(Code);
                player.SendResponse();
            }

            bool Check()
            {
                bool result;
                result = (SupplyTypeID == 0x1A000211) && (Quantity > 0);
                return result;
            }

            if (!Check())
            {
                SendCode(READ_PACKET_ERROR);
                return;
            }
            ClubToMove = player.Inventory.ItemWarehouse.GetClub(ClubIndexFrom, TGET_CLUB.gcIndex);
            ClubMoveTo = player.Inventory.ItemWarehouse.GetClub(ClubIndexTo, TGET_CLUB.gcIndex);

            if ((ClubToMove == null) || (ClubMoveTo == null) || (!(GetItemGroup(ClubToMove.ItemTypeID) == 0x4)) || (!(GetItemGroup(ClubMoveTo.ItemTypeID) == 0x4)))
            {
                SendCode(CLUBSET_NOT_FOUND_OR_NOT_EXIST);
                return;
            }
            TotalPoint = Quantity * ItemMovePoint;
            if (ClubToMove.GetClubPoint() < TotalPoint)
            {
                TotalPoint = ClubToMove.GetClubPoint();
            }
            if (!(Ceiling(a: TotalPoint / ItemMovePoint) == (int)Quantity))
            {
                return;
            }
            if ((ClubMoveTo.GetClubPoint() + TotalPoint) > 99999)
            {
                SendCode(CLUBSET_POINTFULL_OR_NOTENOUGHT);
                return;
            }
            // # REMOVE UCM CHIP #
            RemoveItem = player.Inventory.Remove(SupplyTypeID, Quantity, true);
            if (!RemoveItem.Status)
            {
                SendCode(REMOVE_ITEM_FAIL);
                return;
            }
            if (ClubToMove.RemoveClubPoint(TotalPoint))
            {
                if (!ClubMoveTo.AddClubPoint(TotalPoint))
                {
                    SendCode(CLUBSET_POINTFULL_OR_NOTENOUGHT);
                    return;
                }
            }
            else
            {
                SendCode(CLUBSET_POINTFULL_OR_NOTENOUGHT);
                return;
            }
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem(ClubToMove);
            // ## add transaction
            player.Inventory.ItemTransaction.AddClubSystem(ClubMoveTo);

            player.SendTransaction();
            SendCode(Zero);
        }
    }
}
