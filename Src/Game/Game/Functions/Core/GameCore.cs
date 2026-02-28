using System;
using System.Linq;
using Game.Client;
using PangyaAPI;
using PangyaAPI.BinaryModels;
using Game.Client.Inventory;
using Connector.DataBase;
using static Game.GameTools.PacketCreator;
using static PangyaFileCore.IffBaseManager;
using Game.Lobby;
using Game.Game;
using Game.Client.Inventory.Data;
using Game.Client.Inventory.Data.Slot;
using Game.Client.Inventory.Data.ItemDecoration;
using Game.Data;
using Game.Defines;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Functions.Core
{
    public class MatchHistoryResult
    {
        public int SEX { get; set; }
        public string NICKNAME { get; set; }
        public string USERID { get; set; }
        public int UID { get; set; }
    }

    public class GameCore
    {
        #region Public Methods
        public void PlayerGetMatchHistory(GPlayer player)
        {
            try
            {
                WriteConsole.WriteLine($"[MATCH_HISTORY] Loading history for Player {player.GetUID} ({player.GetNickname})", ConsoleColor.Cyan);
                
                // For now, always send empty match history to prevent client hang
                // TODO: Implement proper match history system when table is ready
                WriteConsole.WriteLine($"[MATCH_HISTORY] Match history system disabled - sending empty history", ConsoleColor.Yellow);
                
                player.Response.Write(new byte[] { 0xAF, 0x00 });
                player.Response.WriteZero(260); // 5 slots x 52 bytes = 260 bytes
                player.SendResponse();

                // Send additional packets if player is in lobby (not in game)
                if (player.GameID == ushort.MaxValue)
                {
                    player.SendResponse(new byte[] { 0x2E, 0x02, 0x00, 0x00, 0x00, 0x00 });
                    player.SendResponse(new byte[] { 0x20, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                }
                
                WriteConsole.WriteLine($"[MATCH_HISTORY] Empty history sent to player {player.GetUID}", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[MATCH_HISTORY_ERROR] Exception: {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[MATCH_HISTORY_ERROR] Stack: {ex.StackTrace}", ConsoleColor.Yellow);
                
                // Send empty history to prevent client hang
                try
                {
                    player.Response.Clear();
                    player.Response.Write(new byte[] { 0xAF, 0x00 });
                    player.Response.WriteZero(260);
                    player.SendResponse();
                }
                catch
                {
                    WriteConsole.WriteLine($"[MATCH_HISTORY_ERROR] Failed to send fallback response", ConsoleColor.Red);
                }
            }
        }

        public void PlayerSaveMacro(GPlayer player, Packet Reader)
        {
            var Macro = new string[8];

            for (int i = 0; i < 8; i++)
            {
                Reader.ReadPStr(out Macro[i], 64);
            }

            // ✅ ใช้ DB_pangya_game_macro แทน
            using (var dbMacro = new Connector.Table.DB_pangya_game_macro())
            {
                var existingMacro = dbMacro.SelectByUID((int)player.GetUID);
                
                if (existingMacro != null)
                {
                    // Update existing macros
                    existingMacro.Macro1 = Macro[0];
                    existingMacro.Macro2 = Macro[1];
                    existingMacro.Macro3 = Macro[2];
                    existingMacro.Macro4 = Macro[3];
                    existingMacro.Macro5 = Macro[4];
                    existingMacro.Macro6 = Macro[5];
                    existingMacro.Macro7 = Macro[6];
                    existingMacro.Macro8 = Macro[7];
                    dbMacro.Update(existingMacro);
                }
                else
                {
                    // Insert new macros
                    var macroData = new Connector.Table.GameMacroData
                    {
                        UID = (int)player.GetUID,
                        Macro1 = Macro[0],
                        Macro2 = Macro[1],
                        Macro3 = Macro[2],
                        Macro4 = Macro[3],
                        Macro5 = Macro[4],
                        Macro6 = Macro[5],
                        Macro7 = Macro[6],
                        Macro8 = Macro[7]
                    };
                    dbMacro.Insert(macroData);
                }
            }
        }

        public void PlayerChangeServer(GPlayer player)
        {
            // Generate random auth key
            var key = Guid.NewGuid().ToString("N").Substring(0, 16);
            
            try
            {
                player.Response.Write(new byte[] { 0xD4, 0x01 });
                player.Response.WriteUInt32(0);
                player.Response.WritePStr(key);
                player.SendResponse();
            }
            catch
            {
                player.Response.Write(new byte[] { 0xD4, 0x1 });
                player.Response.WriteUInt32(1);
                player.SendResponse();
            }
        }

        public void PlayerControlAssist(GPlayer PL)
        {
            uint AssistItem = 467664918;

            try
            {
                WriteConsole.WriteLine($"[ASSIST_CONTROL]: Player {PL.GetUID} ({PL.GetNickname}) requesting ASSIST control", ConsoleColor.Cyan);
                
                // ✅ Check current item quantity in warehouse
                uint currentQuantity = PL.Inventory.GetQuantity(AssistItem);
                WriteConsole.WriteLine($"[ASSIST_CONTROL]: Current item quantity: {currentQuantity}", ConsoleColor.Cyan);

                byte newAssistState;
                
                switch (currentQuantity)
                {
                    case 1: // TO CLOSE (add item to make it 2)
                        {
                            WriteConsole.WriteLine($"[ASSIST_CONTROL]: Closing ASSIST - Adding item", ConsoleColor.Yellow);
                            
                            var Item = new AddItem()
                            {
                                ItemIffId = AssistItem,
                                Quantity = 1,
                                Transaction = true,
                                Day = 0
                            };
                            PL.AddItem(Item);
                            PL.Assist = 0;
                            newAssistState = 0;
                            
                            WriteConsole.WriteLine($"[ASSIST_CONTROL]: ASSIST CLOSED", ConsoleColor.Green);
                        }
                        break;
                        
                    case 2: // TO OPEN (remove item to make it 1)
                        {
                            WriteConsole.WriteLine($"[ASSIST_CONTROL]: Opening ASSIST - Removing item", ConsoleColor.Yellow);
                            
                            PL.Inventory.Remove(AssistItem, 1, true);
                            PL.Assist = 1;
                            newAssistState = 1;
                            
                            WriteConsole.WriteLine($"[ASSIST_CONTROL]: ASSIST OPENED", ConsoleColor.Green);
                        }
                        break;
                        
                    default:
                        {
                            WriteConsole.WriteLine($"[ASSIST_CONTROL]: Invalid item quantity {currentQuantity}, aborting", ConsoleColor.Red);
                            return;
                        }
                }

                // ✅ Save to database (sync AssistMode with Quantity state)
                try
                {
                    // ✅ ใช้ DB_pangya_personal แทน
                    using (var dbPersonal = new Connector.Table.DB_pangya_personal())
                    {
                        dbPersonal.UpdateAssistMode((int)PL.GetUID, newAssistState);
                        WriteConsole.WriteLine($"[ASSIST_CONTROL]: Database updated - AssistMode = {newAssistState}", ConsoleColor.Green);
                    }
                }
                catch (Exception dbEx)
                {
                    WriteConsole.WriteLine($"[ASSIST_CONTROL_ERROR]: Database save failed - {dbEx.Message}", ConsoleColor.Red);
                }

                // ✅ Send transaction to update client UI
                PL.SendTransaction();
                
                // ✅ Send success response
                PL.SendResponse(new byte[] { 0x6A, 0x02, 0x00, 0x00, 0x00, 0x00 });
                
                WriteConsole.WriteLine($"[ASSIST_CONTROL]: Transaction and response sent to {PL.GetNickname}", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[ASSIST_CONTROL_EXCEPTION]: Player {PL.GetUID} - {ex.Message}", ConsoleColor.Red);
                WriteConsole.WriteLine($"[ASSIST_CONTROL_EXCEPTION]: StackTrace: {ex.StackTrace}", ConsoleColor.Red);
                
                try
                {
                    PL.SendResponse(new byte[] { 0x6A, 0x02, 0x01, 0x00, 0x00, 0x00 });
                }
                catch
                {
                    WriteConsole.WriteLine($"[ASSIST_CONTROL_EXCEPTION]: Failed to send error response", ConsoleColor.Red);
                }
            }
        }

        public void PlayerSaveBar(GPlayer player, Packet packet)
        {
            Channel Lobby;
            GameBase GameHandle;
            PlayerInventory Inventory;


            Lobby = player.Lobby;

            GameHandle = Lobby.GetGameHandle(player);
            Inventory = player.Inventory;

            packet.ReadByte(out byte action);
            packet.ReadUInt32(out uint id);
            try
            {
                var Response = new PangyaBinaryWriter();

                Response.Write(new byte[] { 0x4B, 0x00 });
                Response.WriteUInt32(0);
                Response.WriteByte(action);
                Response.WriteUInt32(player.ConnectionID);
                switch (action)
                {
                    case 1: // ## caddie
                        {
                            if (!Inventory.SetCaddieIndex(id))
                            {
                                player.Close();
                                return;
                            }
                            Response.Write(Inventory.GetCaddieData());
                        }
                        break;
                    case 2: // ## ball
                        {
                            if (!Inventory.SetBallTypeID(id))
                            {
                                player.Close();
                                return;
                            }
                            Response.Write(id);
                        }
                        break;
                    case 3: // ## club
                        {
                            if (!Inventory.SetClubSetIndex(id))
                            {
                                player.Close();
                                return;
                            }
                            
                            // ✅ บันทึก ClubIndex ที่เลือก
                            player.SelectedClubInLocker = id;
                            WriteConsole.WriteLine($"[SAVE_BAR] 📌 Saved SelectedClubInLocker = {id}", ConsoleColor.Cyan);
                            
                            Response.Write(Inventory.GetClubData());//clubdata temp
                        }
                        break;
                    case 4: // ## char
                        {
                            if (!Inventory.SetCharIndex(id))
                            {
                                player.Close();
                                return;
                            }
                            Response.Write(Inventory.GetCharData());
                        }
                        break;
                    case 5: // ## mascot
                        {
                            if (!Inventory.SetMascotIndex(id))
                            {
                                player.Close();
                                return;
                            }
                            Response.Write(Inventory.GetMascotData());
                        }
                        break;
                    case 7: // ## start game
                        {
                            if (GameHandle == null) return;

                            GameHandle.AcquireData(player);
                        }
                        break;
                }
                if (action == 4 && GameHandle != null)
                {
                    GameHandle.Send(Response);
                    //Atualizar
                    GameHandle.Send(ShowActionGamePlayInfo(player));
                    if (GameHandle.GameType == GAME_TYPE.CHAT_ROOM)
                    {
                        //GameHandle.Send(ShowRoomEntrance(player.ConnectionID, 15));
                    }
                }
                else
                {
                    player.SendResponse(Response.GetBytes());
                }
            }
            catch
            {
                player.Close();
            }
        }

        public void PlayerChangeEquipment(GPlayer player, Packet Reader)
        {
            PangyaBinaryWriter Reply;
            bool Status;

            if (!Reader.ReadByte(out byte action)) { return; }

            Status = false;
            Reply = new PangyaBinaryWriter();
            try
            {
                Reply.Write(new byte[] { 0x6B, 0x00, 0x04 });
                Reply.WriteByte(action);
                switch (action)
                {
                    case 0:  // ## save char equip
                        {
                            var invchar = (CharacterData)Reader.Read(new CharacterData());

                            var character = player.Inventory.GetCharacter(invchar.TypeID);
                            if (character == null)
                            {
                                WriteConsole.WriteLine("[PLAYER_CHANGE_EQUIPCHAR]: Error Ao Tentar Setar EquipChar", ConsoleColor.Red);
                                player.Close();
                                return;
                            }
                            character.EquipTypeID = invchar.EquipTypeID;
                            character.EquipIndex = invchar.EquipIndex;
                            character.AuxPart = invchar.AuxPart;
                            character.AuxPart2 = invchar.AuxPart2;
                            character.AuxPart3 = invchar.AuxPart3;
                            character.AuxPart4 = invchar.AuxPart4;
                            character.AuxPart5 = invchar.AuxPart5;
                            character.FCutinIndex = invchar.FCutinIndex;
                            character.Power = invchar.Power;
                            character.Control =invchar.Control;
                            character.Impact = invchar.Impact;
                            character.Spin = invchar.Spin;
                            character.Curve = invchar.Curve;
                            player.Inventory.ItemCharacter.UpdateCharacter(character);
                            Status = true;
                            Reply.Write(player.Inventory.GetCharData(invchar.Index));
                        }
                        break;
                    case 1:  // ## change caddie
                        {
                            Reader.ReadUInt32(out uint CaddiIndex);
                            if (!player.Inventory.SetCaddieIndex(CaddiIndex))
                            {
                                WriteConsole.WriteLine("[PLAYER_CHANGE_CADDIE]: Error Ao Tentar Setar CaddieIndex", ConsoleColor.Red);
                                player.Close();
                            }
                            Status = true;
                            Reply.WriteUInt32(CaddiIndex);
                        }
                        break;
                    case 2: // ## item for play
                        {
                            ItemSlotData ItemSlots;

                            ItemSlots = (ItemSlotData)Reader.Read(new ItemSlotData());
                            player.Inventory.ItemSlot.SetItemSlot(ItemSlots);
                            Status = true;
                            Reply.Write(player.Inventory.ItemSlot.GetItemSlot());
                        }
                        break;
                    case 3: // ## Change Ball And Club
                        {
                            Reader.ReadUInt32(out uint BallTypeID);
                            Reader.ReadUInt32(out uint ClubIndex);
                            
                            // ✅ บันทึก ClubIndex ที่เลือกใน Locker (ไม่สนใจ Ball)
                            player.SelectedClubInLocker = ClubIndex;
                            WriteConsole.WriteLine($"[CHANGE_EQUIP] 📌 Ball={BallTypeID}, Club={ClubIndex} → Saved SelectedClubInLocker={ClubIndex}", ConsoleColor.Cyan);

                            // บันทึกทั้ง Ball และ Club ลง Equipment
                            if (!player.Inventory.SetGolfEQP(BallTypeID, ClubIndex))
                            {
                                WriteConsole.WriteLine("[PLAYER_CHANGE_EQUIP]: Error Ao Tentar Setar GolfEquip", ConsoleColor.Red);
                                player.Close();
                            }
                            Status = true;
                            Reply.Write(player.Inventory.GetGolfEQP());
                        }
                        break;
                    case 4: // ## Change Decoration
                        {
                            try
                            {
                                var Decoration = (ItemDecorationData)Reader.Read(new ItemDecorationData());
                                
                                WriteConsole.WriteLine($"[DEBUG_DEC]: Player {player.GetUID} changing decoration - BG:{Decoration.BackGroundTypeID}, Frame:{Decoration.FrameTypeID}, Sticker:{Decoration.StickerTypeID}", ConsoleColor.Cyan);

                                if (!player.Inventory.SetDecoration(Decoration.BackGroundTypeID, Decoration.FrameTypeID, Decoration.StickerTypeID, Decoration.SlotTypeID, Decoration.UnknownTypeID, Decoration.TitleTypeID))
                                {
                                    WriteConsole.WriteLine($"[PLAYER_CHANGE_DEC]: Error setting decoration for player {player.GetUID}", ConsoleColor.Red);
                                    
                                    // Send error response instead of disconnecting
                                    var errorPacket = new PangyaBinaryWriter();
                                    errorPacket.Write(new byte[] { 0x6B, 0x00 });
                                    errorPacket.WriteByte(4); // Error code
                                    errorPacket.WriteByte(4); // Action = decoration
                                    player.SendResponse(errorPacket.GetBytes());
                                    errorPacket.Dispose();
                                    return;
                                }
                                
                                Status = true;
                                Reply.Write(player.Inventory.GetDecorationData());
                                WriteConsole.WriteLine($"[PLAYER_CHANGE_DEC]: Successfully changed decoration for player {player.GetUID}", ConsoleColor.Green);
                            }
                            catch (Exception ex)
                            {
                                WriteConsole.WriteLine($"[PLAYER_CHANGE_DEC_ERROR]: {ex.Message}", ConsoleColor.Red);
                                WriteConsole.WriteLine($"[PLAYER_CHANGE_DEC_ERROR]: {ex.StackTrace}", ConsoleColor.Red);
                            }
                        }
                        break;
                    case 5:  // ## change char
                        {
                            Reader.ReadUInt32(out uint CharacterIndex);

                            if (!player.Inventory.SetCharIndex(CharacterIndex))
                            {
                                WriteConsole.WriteLine("[PLAYER_CHANGE_CHAR]: Error Ao Tentar Setar CharIndex", ConsoleColor.Red);
                                player.Close();
                            }
                            Status = true;
                            Reply.WriteUInt32(CharacterIndex);
                        }
                        break;
                    case 8: // ## change mascot
                        {
                            Reader.ReadUInt32(out uint MascotIndex);

                            if (!player.Inventory.SetMascotIndex(MascotIndex))
                            {
                                WriteConsole.WriteLine("[PLAYER_CHANGE_MASCOT]: Error Ao Tentar Setar MascotIndex");
                                player.Close();
                                return;
                            }
                            Status = true;

                            Reply.Write(player.Inventory.GetMascotData());
                        }
                        break;
                    case 9:// #Cutin 
                        {
                            var CharacterIndex = Reader.ReadUInt32();
                            var CutinIndex = Reader.ReadUInt32();
                            if (!player.Inventory.SetCutInIndex(CharacterIndex, CutinIndex))
                            {
                                WriteConsole.WriteLine("[PLAYER_CHANGE_CHARCUTIN]: Error Ao Tentar Setar Cutin", ConsoleColor.Red);
                                player.Close();
                            }
                            Status = true;
                            Reply.Write(CharacterIndex);
                            Reply.Write(CutinIndex);
                            Reply.WriteZero(12);//12 byte vazios
                        }
                        break;
                    default:
                        WriteConsole.WriteLine("Action_Unkown: {0}, Array: {1}", new object[] { action, BitConverter.ToString(Reader.GetRemainingData) });
                        break;
                }
                if (Status)
                {
                    player.SendResponse(Reply.GetBytes());
                    
                    // ✅ SAVE TO DATABASE after equipment change
                    try
                    {
                        WriteConsole.WriteLine($"[CHANGE_EQUIP] 💾 Player {player.GetNickname} (UID:{player.GetUID}) changed equipment (action:{action}) - Saving to DB...", ConsoleColor.Cyan);
                        
                        if (player.Inventory != null)
                        {
                            // ✅ ใช้ DB_pangya_user_equip แทน
                            using (var dbEquip = new Connector.Table.DB_pangya_user_equip())
                            {
                                var equipData = dbEquip.SelectByUID((int)player.GetUID);
                                if (equipData != null)
                                {
                                    equipData.CHARACTER_ID = (int)player.Inventory.CharacterIndex;
                                    equipData.CADDIE = (int)player.Inventory.CaddieIndex;
                                    equipData.CLUB_ID = (int)player.Inventory.ClubSetIndex;
                                    equipData.BALL_ID = (int)player.Inventory.BallTypeID;
                                    equipData.MASCOT_ID = (int)player.Inventory.MascotIndex;
                                    dbEquip.Update(equipData);
                                    WriteConsole.WriteLine($"[CHANGE_EQUIP] ✓ Equipment saved to pangya_user_equip", ConsoleColor.Green);
                                }
                            }
                        }
                    }
                    catch (Exception saveEx)
                    {
                        WriteConsole.WriteLine($"[CHANGE_EQUIP] ⚠ Save failed: {saveEx.Message}", ConsoleColor.Yellow);
                    }
                    
                    // ✅ BROADCAST TO ROOM - ถ้าอยู่ใน Game Room ให้ส่งข้อมูล equipment ใหม่ให้คนอื่นในห้องเห็น
                    try
                    {
                        var lobby = player.Lobby;
                        if (lobby != null && player.GameID != ushort.MaxValue)
                        {
                            var gameHandle = lobby.GetGameHandle(player);
                            if (gameHandle != null)
                            {
                                // ส่ง packet บอกให้คนอื่นในห้องอัปเดตข้อมูล player
                                WriteConsole.WriteLine($"[CHANGE_EQUIP] 📡 Broadcasting equipment change to room (action:{action})", ConsoleColor.Cyan);
                                
                                // สำหรับ action 3 (Ball + Club) ส่งข้อมูล golf equipment ใหม่
                                if (action == 3)
                                {
                                    var broadcastPacket = new PangyaBinaryWriter();
                                    broadcastPacket.Write(new byte[] { 0x6B, 0x00, 0x04 });
                                    broadcastPacket.WriteByte(3); // action = golf equipment
                                    broadcastPacket.WriteUInt32(player.ConnectionID); // ให้รู้ว่าใครเปลี่ยน
                                    broadcastPacket.Write(player.Inventory.GetGolfEQP());
                                    gameHandle.Send(broadcastPacket.GetBytes());
                                    broadcastPacket.Dispose();
                                    
                                    WriteConsole.WriteLine($"[CHANGE_EQUIP] ✓ Golf equipment broadcasted to room", ConsoleColor.Green);
                                }
                                // สำหรับ action 4 (Character) มีการ broadcast อยู่แล้ว
                                else if (action == 4)
                                {
                                    WriteConsole.WriteLine($"[CHANGE_EQUIP] ℹ Character change already broadcasted", ConsoleColor.Gray);
                                }
                            }
                        }
                    }
                    catch (Exception broadcastEx)
                    {
                        WriteConsole.WriteLine($"[CHANGE_EQUIP] ⚠ Broadcast failed: {broadcastEx.Message}", ConsoleColor.Yellow);
                    }
                }
            }
            catch
            {
            }
        }

        public void PlayerGPTime(GPlayer player)
        {
            using (var Response = new PangyaBinaryWriter())
            {
                Response.Write(new byte[] { 0xbA, 0x00 });
                Response.Write(GameTools.Tools.GetFixTime(DateTime.Now));
                player.SendResponse(Response);
            }
        }

        public void PlayerChangeMascotMessage(GPlayer player, Packet packet)
        {
            packet.ReadUInt32(out uint MASCOT_IDX);
            packet.ReadPStr(out string MASCOT_MSG);

            if (!player.Inventory.SetMascotText(MASCOT_IDX, MASCOT_MSG))
            {
                player.SendResponse(new byte[] { 0xE2, 0x00, 0x01 });
                return;
            }
            player.Response.Write(new byte[] { 0xE2, 0x00, 0x04 });
            player.Response.WriteUInt32(MASCOT_IDX);
            player.Response.WritePStr(MASCOT_MSG);
            player.Response.WriteUInt64(player.GetPang);
            player.SendResponse();
        }

        public void PlayerGetCutinInfo(GPlayer player, Packet packet)
        {
            var cutin = (CutinInfoData)packet.Read(new CutinInfoData());

            switch (cutin.Type)
            {
                case 0:
                    {
                        player.SendResponse(IffEntry.CutinInfo.GetCutinString(cutin.TypeID));
                    }
                    break;
                case 1:
                    {
                        var Char = player.Inventory.GetCharacter(cutin.TypeID);
                        var Item = player.Inventory.ItemWarehouse.GetItem(Char.FCutinIndex);
                        if (Item == null)
                        {
                            return;
                        }
                        player.SendResponse(IffEntry.CutinInfo.GetCutinString(Item.ItemTypeID));
                    }
                    break;
            }
        }
        #endregion
    }
}
