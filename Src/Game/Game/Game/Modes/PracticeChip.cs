using System;
using System.Linq;
using PangyaAPI;
using PangyaAPI.BinaryModels;
using Game.Defines;
using Game.Client;
using Game.Client.Data;
using Game.Game.Data;
using static Game.GameTools.TGeneric;
using static Game.GameTools.PacketCreator;
using Game.Game.Helpers;
using System.IO;
using PangyaAPI.PangyaPacket;
using PangyaAPI.Tools;

namespace Game.Game.Modes
{
    public class PracticeChip : GameBase
    {
        public PracticeChip(GPlayer player, GameInformation GameInfo, GameEvent CreateEvent, GameEvent UpdateEvent, GameEvent DestroyEvent, PlayerEvent OnJoin, PlayerEvent OnLeave, ushort GameID) : base(player, GameInfo, CreateEvent, UpdateEvent, DestroyEvent, OnJoin, OnLeave, GameID)
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ? Room created - ID: {GameID}, Map: {GameInfo.Map}, Holes: {GameInfo.HoleTotal}", ConsoleColor.Green);
        }

        public override void AcquireData(GPlayer player)
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? AcquireData START - Player: {player.GetNickname}", ConsoleColor.Cyan);
            
            player.GameInfo.GameReady = true;
            WriteConsole.WriteLine($"[PLAYER_REQUEST_ACQUIRE_DATA]: {player.GetNickname}");
            player.GameInfo.GameData.Reverse();
            player.GameInfo.ConnectionID = player.ConnectionID;
            player.GameInfo.UID = player.GetUID;
            player.GameInfo.GameCompleted = false;

            player.GameInfo.Versus.LoadHole = false;
            player.GameInfo.Versus.LoadComplete = false;
            player.GameInfo.Versus.ShotSync = false;

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Sending packet 0x76 (Player Count)", ConsoleColor.Yellow);
            var packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x76, 0x00 });
            packet.Write((byte)4);
            packet.Write(1); //meu id? 
            packet.Write(GameTools.Tools.GetFixTime(DateTime.Now));
            player.SendResponse(packet.GetBytes());

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Sending packet 0x45 (Statistics)", ConsoleColor.Yellow);
            packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x45, 0x00, });
            packet.Write(player.Statistic());
            packet.Write(player.Inventory.GetTrophyInfo());
            packet.Write(uint.MaxValue);//XP?
            packet.Write(uint.MaxValue);//XP?
            packet.Write(uint.MaxValue);//xp?
            player.SendResponse(packet);

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Sending packet 0x52 (Game Info) - Map: {fGameData.Map}, Holes: {fGameData.HoleTotal}", ConsoleColor.Yellow);
            packet = new PangyaBinaryWriter();

            packet.Write(new byte[] { 0x52, 0x00 });
            packet.Write(fGameData.Map); //mapa
            packet.Write((byte)TGAME_MODE.GAME_MODE_REPEAT);//GameType
            packet.Write(fGameData.Mode);//mode game
            packet.Write(fGameData.HoleTotal); //hole total
            packet.Write(Trophy); //id do trofeu
            packet.Write(fGameData.VSTime);
            packet.Write(fGameData.GameTime);
            packet.Write(GetHoleBuild());
            player.SendResponse(packet.GetBytes());
            UpdatePlayer(player);
            
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ? AcquireData COMPLETE", ConsoleColor.Green);
        }

        public override void DestroyRoom()
        {
            throw new NotImplementedException();
        }

        public override byte[] GameInformation()
        {
            var response = new PangyaBinaryWriter();

            response.WriteStr(fGameData.Name, 64); //ok
            response.Write(fGameData.Password.Length > 0 ? false : true);
            response.Write(Started == true ? (byte)0 : (byte)1);
            response.Write(Await);//Orange
            response.Write(fGameData.MaxPlayer);
            response.Write((byte)Players.Count);
            response.Write(GameKey, 17);//ultimo byte ? zero
            response.Write(fGameData.Time30S);
            response.Write(fGameData.HoleTotal);
            response.Write((byte)TGAME_MODE.GAME_MODE_REPEAT);//GameType
            response.Write((ushort)ID);
            response.Write(fGameData.Mode);
            response.Write(fGameData.Map);
            response.Write(fGameData.VSTime);
            response.Write(fGameData.GameTime);
            response.Write(GetTrophy);
            response.Write(Idle);
            response.Write(fGameData.GMEvent); //GM Event 0(false), ON 1(true)
            response.WriteZero(0x4A);//GUILD DATA
            response.WriteUInt32(100);// rate pang 
            response.WriteUInt32(100);// rate chuva 
            response.Write(Owner);
            response.WriteByte((byte)GAME_TYPE.CHIP_IN_PRACTICE); //is chip-in practice
            response.Write(fGameData.Artifact);//artefato
            response.Write(fGameData.NaturalMode);//natural mode
            response.Write(fGameData.GPTypeID);//Grand Prix 1
            response.Write(fGameData.GPTypeIDA);//Grand Prix 2
            response.Write(fGameData.GPTime);//Grand Time
            response.Write(Iff<uint>(fGameData.GP, 1, 0));// grand prix active
            return response.GetBytes();
        }

        public override byte[] GetGameHeadData()
        {
            var response = new PangyaBinaryWriter();
            response.Write(new byte[] { 0x4A, 0x00, 0xFF, 0xFF });
            response.Write((byte)4);//GameType
            response.Write(fGameData.Map);
            response.Write(fGameData.HoleTotal);
            response.Write(fGameData.Mode);
            if (fGameData.HoleNumber > 0)
            {
                response.Write(fGameData.HoleNumber);
                response.Write(fGameData.LockHole == 7 ? 7 : 0);
            }
            response.Write(fGameData.NaturalMode);
            response.Write(fGameData.MaxPlayer);
            response.Write(fGameData.Time30S);
            response.Write(Idle);  //Room Idle
            response.Write(fGameData.VSTime);
            response.Write(fGameData.GameTime);
            response.Write(0); // trophy typeid
            response.Write(fGameData.Password.Length > 0 ? false : true);
            if (fGameData.Password.Length > 0)
            {
                response.WritePStr(fGameData.Password);
            }
            response.WritePStr(fGameData.Name);
            return response.GetBytes();
        }

        public override void OnPlayerLeave()
        {
            this.Started = false;
        }

        public override void PlayerGameDisconnect(GPlayer player)
        {
            if (Count == 0)
            {
                player.SetGameID(0xFFFF);
                player.SendResponse(ShowLeaveGame());
                PlayerLeave(this, player);
                this.Destroy(this);
                player.Game = null;
                FirstShot = false;
            }
            else
            {
                player.SetGameID(0xFFFF);
                PlayerLeave(this, player);
                OnPlayerLeave();
                Send(ShowGameLeave(player.ConnectionID, 2));
                //{ Find New Master }
                if ((uint)player.GetUID == Owner && Players.Count >= 1)
                    FindNewMaster();

                //{ Room Update }
                Update(this);

                player.SendResponse(ShowLeaveGame());

                player.Game = null;

                FirstShot = false;
            }
        }

        public override void PlayerLeavePractice()
        {
            WriteConsole.WriteLine($"[PRACTICE_MODE] 🎮 PlayerLeavePractice called - Players: {Players.Count}", ConsoleColor.Magenta);


            CopyScore();


            GenerateExperience();

            foreach (var P in Players)
                Write(ShowNameScore(P.GetNickname, P.GameInfo.GameData.Score, P.GameInfo.GameData.Pang));


            SendUnfinishedData();

            Write(new byte[] { 0x8C, 0x00 });

            foreach (var P in Players)
            {
                //{ CE 00 }
                SendMatchData(P);
                //{ 33 01 }

            }
            Started = false;
            WriteConsole.WriteLine($"[PRACTICE_MODE] ✅ PlayerLeavePractice completed", ConsoleColor.Green);
        }

        public new void SendUnfinishedData()
        {
            foreach (var P in PlayerData)
                if (!P.GameCompleted)
                    Write(ShowHoleData(P.ConnectionID, P.HolePos, (byte)P.GameData.TotalShot, (uint)P.GameData.Score, P.GameData.Pang, P.GameData.BonusPang, false));
        }

        public override void PlayerLoading(GPlayer player, Packet packet)
        {
            byte Process;

            Process = packet.ReadByte();

            Send(ShowGameLoading(player.ConnectionID, Process));
            player.GameInfo.Versus.LoadComplete = (Process * 10 >= 80);

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? PLAYER_LOADING: {player.GetNickname}:{Process * 10}%", ConsoleColor.Cyan);
        }

        public override void PlayerLoadSuccess(GPlayer client)
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ✓ PlayerLoadSuccess - {client.GetNickname} loaded successfully", ConsoleColor.Green);
            
            client.GameInfo.Versus.LoadComplete = true;
            client.GameInfo.GameData.HoleComplete = false;
            client.GameInfo.Versus.HoleDistance = 99999999;

            // ✅ คำนวณเวลาที่ผ่านไปตั้งแต่เริ่มเกม (Elapsed Time)
            // Client จะเอาไปคำนวณ: TimeRemaining = GameTime - ElapsedTime
            uint elapsedMs = 0;
            if (GameStart != DateTime.MinValue)
            {
                TimeSpan elapsed = DateTime.Now - GameStart;
                elapsedMs = (uint)Math.Max(0, elapsed.TotalMilliseconds);
                
                if (fGameData.GameTime > 0)
                {
                    uint remainingMs = fGameData.GameTime > elapsedMs ? fGameData.GameTime - elapsedMs : 0;
                    uint remainingMinutes = remainingMs / 60000;
                    uint remainingSeconds = (remainingMs % 60000) / 1000;
                    
                    WriteConsole.WriteLine($"[PRACTICE_CHIP] ⏱️ Elapsed: {elapsedMs / 1000}s, Remaining: {remainingMinutes}:{remainingSeconds:D2}", ConsoleColor.Cyan);
                }
                else
                {
                    WriteConsole.WriteLine($"[PRACTICE_CHIP] ♾️ No time limit mode (Elapsed: {elapsedMs / 1000}s)", ConsoleColor.Cyan);
                }
            }
            else
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ⚠ GameStart not set - using elapsed=0", ConsoleColor.Yellow);
            }

            WriteConsole.WriteLine($"[PRACTICE_CHIP] 📤 Sending match time and who plays", ConsoleColor.Yellow);
            client.Write(ShowMatchTimeUsed(elapsedMs));
            client.Write(ShowWhoPlay(client.ConnectionID));
        }

        public override void PlayerSendFinalResult(GPlayer player, Packet packet)
        {
        }

        public override void PlayerShotData(GPlayer player, Packet packet)
        {
            TShotData S;
            player.GameInfo.Versus.ShotSync = false;

            var decrypted = DecryptShot(packet.ReadBytes((int)(packet.GetSize - packet.GetPos)));

            packet.SetReader(new PangyaBinaryReader(new MemoryStream(decrypted)));

            S = (TShotData)packet.Read(new TShotData());

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ??? Shot Data - Player: {player.GetNickname}, Type: {S.ShotType}, Pang: {S.Pang}", ConsoleColor.Cyan);

            // ✅ CONSUME SPECIAL BALL - ตรวจสอบก่อนยิง
            bool ballConsumed = false;
            try
            {
                var currentBall = player.Inventory.ItemWarehouse.GetItem(player.Inventory.BallTypeID, 1);
                if (currentBall != null && currentBall.ItemC0 > 0)
                {
                    uint oldQty = currentBall.ItemC0;
                    
                    if (currentBall.RemoveQuantity(1))
                    {
                        ballConsumed = true;
                        WriteConsole.WriteLine($"[BALL_CONSUME]: {player.GetNickname} used special ball - TypeID:0x{currentBall.ItemTypeID:X}, Qty: {oldQty} → {currentBall.ItemC0}", ConsoleColor.Yellow);
                        
                        if (currentBall.ItemC0 == 0)
                        {
                            WriteConsole.WriteLine($"[BALL_CONSUME]: ⚠ Special ball depleted! Switching to default ball...", ConsoleColor.Red);
                            
                            var defaultBall = player.Inventory.ItemWarehouse.GetItem(335544320, 1);
                            if (defaultBall != null)
                            {
                                player.Inventory.SetBallTypeID(335544320);
                                WriteConsole.WriteLine($"[BALL_CONSUME]: ✓ Switched to default ball", ConsoleColor.Green);
                                player.SendResponse(new byte[] { 0x4B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 });
                            }
                        }
                        
                        currentBall.ItemNeedUpdate = true;
                        player.Inventory.ItemWarehouse.Update(currentBall);
                    }
                }
            }
            catch (Exception ballEx)
            {
                WriteConsole.WriteLine($"[BALL_CONSUME_ERROR]: {ballEx.Message}", ConsoleColor.Red);
            }

            switch (S.ShotType)
            {
                case TShotType.Unknown:
                case TShotType.Normal:
                    player.GameInfo.GameData.Pang += S.Pang;
                    player.GameInfo.GameData.BonusPang += S.BonusPang;
                    player.GameInfo.GameData.ShotCount += 1;
                    player.GameInfo.GameData.TotalShot += 1;
                    WriteConsole.WriteLine($"[PRACTICE_CHIP] ? Normal Shot - Total Shots: {player.GameInfo.GameData.TotalShot}", ConsoleColor.White);
                    break;
                case TShotType.OB:
                    {
                        player.GameInfo.GameData.Pang += S.Pang;
                        player.GameInfo.GameData.BonusPang += S.BonusPang;
                        player.GameInfo.GameData.ShotCount += 2;
                        player.GameInfo.GameData.TotalShot += 2;
                        WriteConsole.WriteLine($"[PRACTICE_CHIP] ? OB - Added 2 penalty shots", ConsoleColor.Red);
                    }
                    break;
                case TShotType.Success:
                    {
                        var pangDelta = Math.Abs((long)player.GameInfo.GameData.Pang - (long)S.Pang);
                        var bonusDelta = Math.Abs((long)player.GameInfo.GameData.BonusPang - (long)S.BonusPang);

                        if (pangDelta > 10000 || bonusDelta > 10000)
                        {
                            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? ANTI-CHEAT: Pang delta too high! Disconnecting player", ConsoleColor.Red);
                            player.Close();
                            return;
                        }
                        
                        // ? SET ค่า Pang และ BonusPang จาก TShotData (Client ส่งมา)
                        player.GameInfo.GameData.Pang = S.Pang;
                        player.GameInfo.GameData.BonusPang = S.BonusPang;
                        
                        player.GameInfo.GameData.HoleComplete = true;
                        player.GameInfo.GameData.HoleCompletedCount += 1;
                        player.GameInfo.UpdateScore(player.GameInfo.GameData.HoleComplete);
                        
                        if (player.GameInfo.GameData.HoleCompletedCount >= fGameData.HoleTotal)
                        {
                            player.GameInfo.GameCompleted = true;
                            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Player {player.GetNickname} completed ALL holes! Score: {player.GameInfo.GameData.Score}, Pang: {player.GameInfo.GameData.Pang}", ConsoleColor.Green);
                        }
                        else
                        {
                            WriteConsole.WriteLine($"[PRACTICE_CHIP] ? Hole Complete - {player.GameInfo.GameData.HoleCompletedCount}/{fGameData.HoleTotal}, Score: {player.GameInfo.GameData.Score}, Pang: {player.GameInfo.GameData.Pang}", ConsoleColor.Green);
                        }
                    }
                    break;
            }

            packet.Clear();
            packet.Write(new byte[] { 0x6E, 0x00 });
            packet.WriteUInt32(player.ConnectionID);
            packet.WriteByte((byte)player.GameInfo.HolePos);
            packet.WriteSingle(S.Pos.X);
            packet.WriteSingle(S.Pos.Z);
            packet.Write(S.MatchData);
            Send(packet.GetBytes());

            player.GameInfo.Versus.HoleDistance = S.Pos.HoleDistance(player.GameInfo.HolePos3D);
            Console.WriteLine("[PLAYER_HOLE_DISTANCE]: " + player.GameInfo.Versus.HoleDistance);
        }

        public override void PlayerShotInfo(GPlayer player, Packet packet)
        {
            var ShotType = packet.ReadUInt16();
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Shot Info - Player: {player.GetNickname}, ShotType: {ShotType}", ConsoleColor.White);
            
            byte[] UN;
            var resp = new PangyaBinaryWriter();
            resp.Write(new byte[] { 0x55, 0x00 });
            resp.Write(player.ConnectionID);
            switch (ShotType)
            {
                case 1:
                    {
                        packet.Skip(9);
                        UN = packet.ReadBytes(61);
                        resp.Write(UN);
                    }
                    break;
                default:
                    {
                        UN = packet.ReadBytes(61);
                        resp.Write(UN);
                    }
                    break;
            }
            Send(resp);
        }

        public override void PlayerStartGame()
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? PLAYER_START_GAME - Players: {Players.Count}", ConsoleColor.Magenta);
            
            if (Started)
            {
                WriteConsole.WriteLine("[PRACTICE_CHIP] ? Game already started", ConsoleColor.Red);
                return;
            }

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Clearing player data...", ConsoleColor.Yellow);
            // { Clear Player Score Data }
            ClearPlayerData();

            //{ Trophy }
            Gold = 0xFFFFFFFF;
            Silver1 = 0xFFFFFFFF;
            Silver2 = 0xFFFFFFFF;
            Bronze1 = 0xFFFFFFFF;
            Bronze2 = 0xFFFFFFFF;
            Bronze3 = 0xFFFFFFFF;

            //{ Medal }
            BestRecovery = 0xFFFFFFFF;
            BestChipIn = 0xFFFFFFFF;
            BestDrive = 0xFFFFFFFF;
            BestSpeeder = 0xFFFFFFFF;
            LongestPutt = 0xFFFFFFFF;
            LuckyAward = 0xFFFFFFFF;

            Started = true;
            Await = true;
            
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ??? Building holes...", ConsoleColor.Yellow);
            BuildHole();

            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Sending start packets (0x30, 0x31, PangRate)", ConsoleColor.Yellow);
            Send(new byte[] { 0x30, 0x02 });
            Send(new byte[] { 0x31, 0x02 });

            Send(ShowPangRate());

            Update(this);

            GameStart = DateTime.Now;
            
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ? Game started successfully at {GameStart}", ConsoleColor.Green);
        }
        public override void PlayerSyncShot(GPlayer client, Packet packet)
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? PlayerSyncShot - {client.GetNickname}, Completed: {client.GameInfo.GameCompleted}", ConsoleColor.Cyan);
            
            Send(ShowDropItem(client.ConnectionID));
            var Succeed = client.GameInfo.GameCompleted;

            if (Succeed)
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Player finished! Sending 0x99 packet", ConsoleColor.Green);
                client.SendResponse(new byte[] { 0x99, 0x01 });
            }
            // { Show Treasure Gauge }
            client.SendResponse(ShowTreasureGuage());
            //{ Show Name,Score,Pang when client finish their game }
            if (Succeed)
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Final Score - {client.GetNickname}: Score={client.GameInfo.GameData.Score}, Pang={client.GameInfo.GameData.Pang}", ConsoleColor.Yellow);
                Send(ShowNameScore(client.GetNickname, client.GameInfo.GameData.Score, client.GameInfo.GameData.Pang));

                Send(ShowHoleData(client.ConnectionID, client.GameInfo.HolePos, (byte)client.GameInfo.GameData.TotalShot, (uint)client.GameInfo.GameData.Score, (uint)client.GameInfo.GameData.Pang, (uint)client.GameInfo.GameData.BonusPang));
            }
            else if (client.GameInfo.GameData.HoleComplete)
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ? Hole completed - Sending hole data", ConsoleColor.Green);
                Send(ShowHoleData(client.ConnectionID, client.GameInfo.HolePos, (byte)client.GameInfo.GameData.TotalShot, (uint)client.GameInfo.GameData.Score, (uint)client.GameInfo.GameData.Pang, (uint)client.GameInfo.GameData.BonusPang));
            }

            if (Succeed)
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Player leaving match", ConsoleColor.Yellow);
                Send(ShowLeaveMatch(client.ConnectionID, 2));
            }

            if (_allFinished())
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? All players finished!", ConsoleColor.Magenta);
                _onAllPlayerFinished();
            }
        }

        public override void SendHoleData(GPlayer player)
        {
            var H = Holes.CurrentHole;

            if (H == null)
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? CurrentHole is NULL!", ConsoleColor.Red);
                return;
            }

            var Data = Holes.CurrentHole;
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ??? Sending Hole Data - Wind: {Data.WindPower}, Weather: {Data.Weather}", ConsoleColor.Cyan);
            player.SendResponse(ShowWind(Data.WindPower, Data.WindDirection));
            player.SendResponse(ShowWeather(Data.Weather));
        }

        public override void SendPlayerOnCreate(GPlayer player)
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? SendPlayerOnCreate - {player.GetNickname}", ConsoleColor.Green);
            
            var packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x48, 0x00 });
            packet.Write((byte)0);
            packet.Write(new byte[] { 0xFF, 0xFF });
            packet.Write((byte)1);
            packet.Write(player.GetGameInfomations(1));
            packet.Write((byte)0);
            player.SendResponse(packet.GetBytes());
        }

        public override void SendPlayerOnJoin(GPlayer player)
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ? SendPlayerOnJoin - {player.GetNickname} (Empty implementation for practice mode)", ConsoleColor.Gray);
            // Practice mode ไม่ต้องส่ง packet join เพราะเป็นโหมดเดี่ยว
        }

        public override bool Validate()
        {
            if (fGameData.MaxPlayer > 4) { return false; }

            return true;
        }

        private void _onAllPlayerFinished()
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? _onAllPlayerFinished - Processing final results...", ConsoleColor.Magenta);
            
            // { copy score }
            CopyScore();

            //AfterMatchDone;
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Generating experience...", ConsoleColor.Yellow);
            GenerateExperience();

            foreach (var P in Players)
            {
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? Sending match data to {P.GetNickname}", ConsoleColor.Cyan);
                SendMatchData(P);
            }
            Started = false;
            
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ? All players finished processing complete!", ConsoleColor.Green);
        }

        public override void GenerateExperience()
        {
            WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? GenerateExperience START", ConsoleColor.Yellow);
            
            foreach (var P in Players)
            {
                P.GameInfo.GameData.EXP = new GameExpTable().GetEXP(GAME_TYPE.CHIP_IN_PRACTICE, GameData.Map, 0, (byte)Players.Count, P.GameInfo.GameData.HoleCompletedCount);
                WriteConsole.WriteLine($"[PRACTICE_CHIP] ?? EXP - {P.GetNickname}: {P.GameInfo.GameData.EXP} (Holes: {P.GameInfo.GameData.HoleCompletedCount})", ConsoleColor.Green);
            }
        }
    }
}
