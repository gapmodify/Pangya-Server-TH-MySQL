using System;
using System.Linq;
using System.Collections.Generic;
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
    public class ModeVersus : GameBase
    {
        // ✅ PlayerOrderTurnCtx (จาก C++ struct)
        private class PlayerOrderTurnCtx
        {
            public GPlayer Player { get; set; }
            public double DistanceToHole { get; set; }
        }

        // ✅ Versus State Enum (จาก C++)
        private enum VersusState : byte
        {
            WAIT_HIT_SHOT,
            SHOOTING,
            END_SHOT,
            LOAD_HOLE,
            WAIT_END_GAME
        }

        private VersusState _currentState = VersusState.WAIT_HIT_SHOT;
        private readonly object _stateLock = new object();
        
        // ✅ Player Turn System (จาก C++)
        private GPlayer _playerTurn = null;
        private List<GPlayer> _playerOrder = new List<GPlayer>();  // เก็บลำดับตอนเริ่มหลุม
        
        // ✅ Sync Flags (จาก C++)
        private Dictionary<uint, bool> _loadHoleFlags = new Dictionary<uint, bool>();
        private Dictionary<uint, bool> _finishCharIntroFlags = new Dictionary<uint, bool>();
        private Dictionary<uint, bool> _syncShotFlags = new Dictionary<uint, bool>();
        private Dictionary<uint, bool> _finishShotFlags = new Dictionary<uint, bool>();
        private Dictionary<uint, bool> _initShotFlags = new Dictionary<uint, bool>();
        
        // ✅ Distance tracking (แยกตามผู้เล่น)
        private Dictionary<uint, double> _playerDistances = new Dictionary<uint, double>();
        
        // ✅ Game Status Flags
        private bool _versusActive = false;
        private uint _nextStepFlag = 0;
        private bool _gameInitialized = false; // ✅ FIX: ป้องกัน init ซ้ำ
        
        public ModeVersus(GPlayer player, GameInformation GameInfo, GameEvent CreateEvent, GameEvent UpdateEvent, GameEvent DestroyEvent, PlayerEvent OnJoin, PlayerEvent OnLeave, ushort GameID) 
            : base(player, GameInfo, CreateEvent, UpdateEvent, DestroyEvent, OnJoin, OnLeave, GameID)
        {
            InitializeSyncFlags();
        }

        private void InitializeSyncFlags()
        {
            foreach (var p in Players)
            {
                _loadHoleFlags[p.ConnectionID] = false;
                _finishCharIntroFlags[p.ConnectionID] = false;
                _syncShotFlags[p.ConnectionID] = false;
                _finishShotFlags[p.ConnectionID] = false;
                _initShotFlags[p.ConnectionID] = false;
                _playerDistances[p.ConnectionID] = 99999999;
            }
        }

        private void ClearAllSyncFlags()
        {
            lock (_stateLock)
            {
                foreach (var key in _loadHoleFlags.Keys.ToList()) _loadHoleFlags[key] = false;
                foreach (var key in _finishCharIntroFlags.Keys.ToList()) _finishCharIntroFlags[key] = false;
                foreach (var key in _syncShotFlags.Keys.ToList()) _syncShotFlags[key] = false;
                foreach (var key in _finishShotFlags.Keys.ToList()) _finishShotFlags[key] = false;
                foreach (var key in _initShotFlags.Keys.ToList()) _initShotFlags[key] = false;
            }
        }

        private bool AllPlayersReady(Dictionary<uint, bool> flags)
        {
            return flags.Values.All(v => v);
        }

        public override void AcquireData(GPlayer player)
        {
            WriteConsole.WriteLine($"[ACQUIRE_DATA]: {player.GetNickname}", ConsoleColor.Cyan);
            
            player.GameInfo.GameData.Reverse();
            player.GameInfo.ConnectionID = player.ConnectionID;
            player.GameInfo.UID = player.GetUID;
            player.GameInfo.GameCompleted = false;

            player.GameInfo.Versus.LoadHole = false;
            player.GameInfo.Versus.LoadComplete = false;
            player.GameInfo.Versus.ShotSync = false;
            
            // ✅ Initialize Distance for this player
            _playerDistances[player.ConnectionID] = 99999999;
            player.GameInfo.Versus.HoleDistance = 99999999;
            
            // ✅ Build hole BEFORE sending 0x76 (เหมือน C++)
            if (Holes == null || Holes.CurrentHole == null)
            {
                BuildHole();
                WriteConsole.WriteLine($"[ACQUIRE_DATA]: Hole built - Wind/Weather ready", ConsoleColor.Green);
            }

            // ✅ 0x76 - Game Info with all players
            var packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x76, 0x00 });
            packet.Write((byte)GameType);
            packet.Write((byte)Players.Count);
            
            foreach (var P in Players)
            {
                packet.Write(P.GetGameInfoVS());
            }
            player.SendResponse(packet.GetBytes());

            // ✅ 0x45 - Player Statistics
            packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x45, 0x00 });
            packet.Write(player.Statistic());
            packet.Write(player.Inventory.GetTrophyInfo());
            packet.Write(uint.MaxValue);
            packet.Write(uint.MaxValue);
            packet.Write(uint.MaxValue);
            player.SendResponse(packet);

            // ✅ 0x52 - Course/Hole Info
            packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x52, 0x00 });
            packet.Write(fGameData.Map);
            packet.Write((byte)fGameData.GameType);
            packet.Write(fGameData.Mode);
            packet.Write(fGameData.HoleTotal);
            packet.Write(0); // Trophy
            packet.Write(fGameData.VSTime);
            packet.Write(fGameData.GameTime);
            packet.Write(GetHoleBuild());
            player.SendResponse(packet);
            
            WriteConsole.WriteLine($"[ACQUIRE_DATA]: ✅ Complete for {player.GetNickname}", ConsoleColor.Green);
        }

        public override void PlayerLoading(GPlayer player, Packet packet)
        {
            byte Process = packet.ReadByte();
            Send(ShowGameLoading(player.ConnectionID, Process));
            
            WriteConsole.WriteLine($"[LOADING]: {player.GetNickname} - {Process * 10}%", ConsoleColor.Cyan);

            // Auto-complete at 70%
            if (Process >= 7 && !player.GameInfo.Versus.LoadComplete)
            {
                Send(ShowGameLoading(player.ConnectionID, 10));
                PlayerLoadSuccess(player);
            }
        }

        public override void PlayerLoadSuccess(GPlayer client)
        {
            WriteConsole.WriteLine($"[LOAD_SUCCESS]: {client.GetNickname}", ConsoleColor.Green);

            client.GameInfo.Versus.LoadComplete = true;
            client.GameInfo.GameData.HoleComplete = false;
            client.GameInfo.GameData.ShotCount = 0;
            client.GameInfo.GameData.TotalShot = 0;

            // ✅ Reset distance
            _playerDistances[client.ConnectionID] = 99999999;
            client.GameInfo.Versus.HoleDistance = 99999999;

            // ✅ Ensure client starts from tee for its own ball (per-player)
            // บาง client จะจำตำแหน่งลูกเก่าจากเกมก่อน/หลุมก่อน ถ้าไม่ส่ง reset
            try
            {
                var teeReset = new PangyaBinaryWriter();
                teeReset.Write(new byte[] { 0x6E, 0x00 });
                teeReset.Write(client.ConnectionID);
                teeReset.Write((byte)client.GameInfo.HolePos);
                teeReset.WriteSingle(0f);
                teeReset.WriteSingle(0f);
                teeReset.Write(new byte[6]);
                client.SendResponse(teeReset.GetBytes());
                WriteConsole.WriteLine($"[LOAD_SUCCESS]: ↺ Sent tee reset 0x6E to {client.GetNickname} (ConnID:{client.ConnectionID})", ConsoleColor.DarkGray);
            }
            catch
            {
            }

            // ✅ Set Load Hole Flag
            lock (_stateLock)
            {
                _loadHoleFlags[client.ConnectionID] = true;
            }

            // ✅ Check if all loaded
            if (AllPlayersReady(_loadHoleFlags))
            {
                // ✅ FIX: ป้องกัน init ซ้ำ
                lock (_stateLock)
                {
                    if (_gameInitialized)
                    {
                        WriteConsole.WriteLine($"[LOAD_SUCCESS]: ⚠️ Already initialized - IGNORED", ConsoleColor.Yellow);
                        return;
                    }
                    _gameInitialized = true;
                }
                
                WriteConsole.WriteLine($"[LOAD_SUCCESS]: ✅ ALL PLAYERS READY!", ConsoleColor.Green);
                
                lock (_stateLock)
                {
                    _currentState = VersusState.WAIT_HIT_SHOT;
                    foreach (var key in _loadHoleFlags.Keys.ToList()) _loadHoleFlags[key] = false;
                }

                // ✅ Initialize Turn Order (จาก C++ - init_turn_hole_start)
                InitializeTurnOrder();
                
                // ✅ Calculate first player turn
                _playerTurn = CalculatePlayerTurn();
                
                // ✅ Send initial data
                SendWeather();
                SendWind();
                Send(new byte[] { 0x15, 0x01, 0x0D, 0x00, 0x57, 0x5F, 0x42, 0x49, 0x47, 0x42, 0x4F, 0x4E, 0x47, 0x44, 0x41, 0x52, 0x49, 0x00, 0x02, 0x01, 0x03, 0x00, 0x03, 0x01, 0x01, 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02, 0x02, 0x01, 0x02, 0x03, 0x02, 0x03, 0x01, 0x00, 0x03, 0x01, 0x00, 0x03, 0x01, 0x02, 0x02, 0x01, 0x02, 0x01, 0x00, 0x03, 0x02, 0x02, 0x02, 0x01, 0x02, 0x02, 0x01, 0x00, 0x00, 0x03, 0x00, 0x02, 0x00, 0x03, 0x02, 0x03, 0x01, 0x00, 0x00, 0x02, 0x02, 0x00, 0x00, 0x01, 0x03, 0x02, 0x01, 0x01, 0x03, 0x01, 0x03, 0x01, 0x03, 0x03, 0x01, 0x00, 0x01, 0x00, 0x01, 0x01, 0x01, 0x00, 0x00, 0x03, 0x00, 0x02, 0x03, 0x01, 0x03, 0x03, 0x01, 0x03, 0x02, 0x03, 0x03, 0x02, 0x01, 0x02, 0x00, 0x01, 0x01, 0x01, 0x00, 0x00 });
                Send(new byte[] { 0x15, 0x01, 0x0D, 0x00, 0x52, 0x5F, 0x42, 0x49, 0x47, 0x42, 0x4F, 0x4E, 0x47, 0x44, 0x41, 0x52, 0x49, 0x00, 0x02, 0x02, 0x01, 0x03, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x03, 0x03, 0x01, 0x00, 0x00, 0x00, 0x00, 0x03, 0x01, 0x01, 0x00, 0x03, 0x02, 0x02, 0x02, 0x01, 0x02, 0x03, 0x01, 0x01, 0x00, 0x03, 0x01, 0x01, 0x02, 0x02, 0x02, 0x00, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x03, 0x01, 0x01, 0x01, 0x01, 0x00, 0x03, 0x03, 0x02, 0x01, 0x02, 0x01, 0x02, 0x03, 0x00, 0x03, 0x02, 0x02, 0x00, 0x01, 0x01, 0x02, 0x03, 0x01, 0x03, 0x03, 0x00, 0x03, 0x02, 0x03, 0x03, 0x00, 0x01, 0x00, 0x02, 0x01, 0x01, 0x03, 0x03, 0x02, 0x02, 0x03, 0x00, 0x03, 0x02, 0x02, 0x00, 0x01, 0x00, 0x00, 0x01 });
                Send(new byte[] { 0x15, 0x01, 0x0F, 0x00, 0x43, 0x4C, 0x55, 0x42, 0x53, 0x45, 0x54, 0x5F, 0x4D, 0x49, 0x52, 0x41, 0x43, 0x4C, 0x45, 0x00, 0x03, 0x02, 0x02, 0x03, 0x00, 0x02, 0x03, 0x01, 0x02, 0x03, 0x03, 0x03, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x02, 0x02, 0x02, 0x01, 0x02, 0x03, 0x03, 0x01, 0x01, 0x03, 0x03, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x03, 0x01, 0x01, 0x03, 0x01, 0x02, 0x01, 0x00, 0x01, 0x02, 0x02, 0x03, 0x03, 0x02, 0x01, 0x01, 0x03, 0x02, 0x03, 0x01, 0x01, 0x01, 0x02, 0x00, 0x00, 0x01, 0x03, 0x03, 0x00, 0x01, 0x01, 0x02, 0x00, 0x02, 0x00, 0x03, 0x03, 0x00, 0x02, 0x03, 0x03, 0x01, 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x02, 0x02, 0x01, 0x00, 0x01, 0x00, 0x03, 0x01, 0x00, 0x00, 0x03, 0x03, 0x00, 0x01, 0x00, 0x00 });

                // ✅ 0x53 - Who plays first
                Send(ShowWhoPlay(_playerTurn.ConnectionID));
                WriteConsole.WriteLine($"[LOAD_SUCCESS]: ▶️ {_playerTurn.GetNickname} plays first (ConnID:{_playerTurn.ConnectionID})", ConsoleColor.Green);
            }
        }

        // ✅ C++ - init_turn_hole_start() - Sort by previous hole score
        private void InitializeTurnOrder()
        {
            _playerOrder.Clear();
            
            var currentHoleNum = Holes != null && Holes.CurrentHole != null ? Holes.IndexOf(Holes.CurrentHole) : 0;
            
            if (currentHoleNum > 0)
            {
                var sorted = Players.OrderBy(p => p.GameInfo.GameData.Score)
                                   .ThenByDescending(p => p.GameInfo.GameData.Pang)
                                   .ToList();
                
                _playerOrder.AddRange(sorted);
                
                var orderStr = string.Join(", ", _playerOrder.Select(p => p.GetNickname + "(" + p.GameInfo.GameData.Score + ")"));
                WriteConsole.WriteLine($"[TURN_ORDER]: Init (Hole {currentHoleNum + 1}) - Sort by SCORE - Order: {orderStr}", ConsoleColor.Magenta);
            }
            else
            {
                var sorted = Players.OrderByDescending(p => p.GameInfo.GameData.Pang).ToList();
                _playerOrder.AddRange(sorted);
                
                var orderStr = string.Join(", ", _playerOrder.Select(p => p.GetNickname + "(" + p.GameInfo.GameData.Pang + "p)"));
                WriteConsole.WriteLine($"[TURN_ORDER]: Init (Hole 1) - Sort by PANG - Order: {orderStr}", ConsoleColor.Magenta);
            }
        }

        // ✅ C++ - getNextPlayerTurnHole() - ดึงจาก m_player_order
        private GPlayer GetNextPlayerTurnHole()
        {
            if (_playerOrder.Count == 0)
                return null;

            var next = _playerOrder[0];
            _playerOrder.RemoveAt(0);

            // Skip ถ้า player จบหลุมแล้ว (recursive)
            if (next.GameInfo.GameData.HoleComplete)
                return GetNextPlayerTurnHole();

            WriteConsole.WriteLine($"[GET_NEXT_TURN]: From order list - {next.GetNickname}", ConsoleColor.Yellow);
            return next;
        }

        // ✅ C++ - requestCalculePlayerTurn() - Main turn calculation
        private GPlayer CalculatePlayerTurn()
        {
            // 1. ลองดึงจาก player order ก่อน (ตอนเริ่มหลุม)
            var nextFromOrder = GetNextPlayerTurnHole();
            if (nextFromOrder != null)
                return nextFromOrder;

            // 2. ถ้า order list หมด = คำนวณใหม่ตามระยะห่าง
            var activePlayers = Players.Where(p => !p.GameInfo.GameData.HoleComplete).ToList();

            if (activePlayers.Count == 0)
            {
                WriteConsole.WriteLine($"[CALC_TURN]: ⚠️ No active players!", ConsoleColor.Red);
                return null;
            }

            var playerOrderTurnList = new List<PlayerOrderTurnCtx>();

            foreach (var p in activePlayers)
            {
                double dist = _playerDistances.ContainsKey(p.ConnectionID)
                    ? _playerDistances[p.ConnectionID]
                    : 99999999;

                playerOrderTurnList.Add(new PlayerOrderTurnCtx
                {
                    Player = p,
                    DistanceToHole = dist
                });

                WriteConsole.WriteLine($"[CALC_TURN_DEBUG]: {p.GetNickname} - Dist: {dist:F2}m, Shots: {p.GameInfo.GameData.ShotCount}, HoleComplete: {p.GameInfo.GameData.HoleComplete}", ConsoleColor.DarkGray);
            }

            // ✅ Versus rule: คนไกลหลุมได้ตีก่อน
            // Tie-breakers เพื่อกันสลับไปมา: shot น้อยก่อน, pang มากก่อน, connId น้อยก่อน
            var sorted = playerOrderTurnList
                .OrderByDescending(ctx => ctx.DistanceToHole)
                .ThenBy(ctx => ctx.Player.GameInfo.GameData.ShotCount)
                .ThenByDescending(ctx => ctx.Player.GameInfo.GameData.Pang)
                .ThenBy(ctx => ctx.Player.ConnectionID)
                .ToList();

            var playerTurn = sorted.First().Player;

            WriteConsole.WriteLine($"[CALC_TURN]: By distance(farther first) - {playerTurn.GetNickname} (Dist: {_playerDistances[playerTurn.ConnectionID]:F2}m, Shots: {playerTurn.GameInfo.GameData.ShotCount})", ConsoleColor.Yellow);

            return playerTurn;
        }

        // ✅ FIX: Handle Init Shot (Packet 0x12 - เมื่อผู้เล่นเริ่มตี)
        public void HandleInitShot(GPlayer player, Packet packet)
        {
            WriteConsole.WriteLine($"[INIT_SHOT]: {player.GetNickname} started shot", ConsoleColor.Cyan);

            lock (_stateLock)
            {
                if (_initShotFlags.ContainsKey(player.ConnectionID) && _initShotFlags[player.ConnectionID])
                {
                    WriteConsole.WriteLine($"[INIT_SHOT]: ⚠️ Already received InitShot - IGNORED", ConsoleColor.Yellow);
                    return;
                }
                
                _initShotFlags[player.ConnectionID] = true;
                _currentState = VersusState.SHOOTING;
            }

            // ✅ Stop player's bar space state
            player.GameInfo.Versus.ShotSync = false;

            // ✅ Read Shot Data (same structure as PlayerShotInfo)
            var ShotType = packet.ReadUInt16();
            byte[] shotData;

            var resp = new PangyaBinaryWriter();
            resp.Write(new byte[] { 0x55, 0x00 });
            resp.Write(player.ConnectionID);

            switch (ShotType)
            {
                case 1: // Power Shot
                    packet.Skip(9);
                    shotData = packet.ReadBytes(61);
                    resp.Write(shotData);
                    break;
                default:
                    shotData = packet.ReadBytes(61);
                    resp.Write(shotData);
                    break;
            }

            // ✅ FIX: Broadcast to ALL players (including sender)
            Send(resp.GetBytes());
            WriteConsole.WriteLine($"[INIT_SHOT]: ✅ Broadcasted shot info to all players", ConsoleColor.Green);
        }

        public override void PlayerShotData(GPlayer player, Packet packet)
        {
            TShotData S;
            player.GameInfo.Versus.ShotSync = false;

            var decrypted = DecryptShot(packet.GetRemainingData);
            packet.SetReader(new PangyaBinaryReader(new MemoryStream(decrypted)));
            S = (TShotData)packet.Read(new TShotData());
            
            // ✅ Set ConnectionId immediately
            S.ConnectionId = player.ConnectionID;
            WriteConsole.WriteLine($"[SHOT_DATA]: ✅ Shot from {player.GetNickname} (ConnID={player.ConnectionID})", ConsoleColor.Green);
            
            // ✅ ตรวจสอบว่าเป็น shot ที่ถูกต้อง
            if (_playerTurn != null && player.ConnectionID != _playerTurn.ConnectionID)
            {
                WriteConsole.WriteLine($"[SHOT_DATA]: ⚠️ Not player's turn! {player.GetNickname} tried to shoot (Turn: {_playerTurn.GetNickname}) - IGNORED", ConsoleColor.Red);
                return;
            }

            lock (_stateLock)
            {
                _currentState = VersusState.SHOOTING;
                _initShotFlags[player.ConnectionID] = true;
            }

            double distBefore = _playerDistances.ContainsKey(player.ConnectionID) ? _playerDistances[player.ConnectionID] : 99999999;

            switch (S.ShotType)
            {
                case TShotType.Success:
                    if (Math.Abs((long)player.GameInfo.GameData.Pang - (long)S.Pang) > 4000 ||
                        Math.Abs((long)player.GameInfo.GameData.BonusPang - (long)S.BonusPang) > 4000)
                    {
                        player.Close();
                        return;
                    }
                    player.GameInfo.GameData.Pang = S.Pang;
                    player.GameInfo.GameData.BonusPang = S.BonusPang;
                    player.GameInfo.GameData.HoleComplete = true;
                    player.GameInfo.GameData.HoleCompletedCount += 1;
                    player.GameInfo.UpdateScore(player.GameInfo.GameData.HoleComplete);
                    
                    if (player.GameInfo.GameData.HoleCompletedCount >= fGameData.HoleTotal)
                        player.GameInfo.GameCompleted = true;
                    
                    WriteConsole.WriteLine($"[SHOT_DATA]: {player.GetNickname} FINISHED HOLE! Score: {player.GameInfo.GameData.Score}", ConsoleColor.Green);
                    break;

                case TShotType.OB:
                    player.GameInfo.GameData.ShotCount += 2;
                    player.GameInfo.GameData.TotalShot += 2;
                    player.GameInfo.GameData.Pang = S.Pang;
                    player.GameInfo.GameData.BonusPang = S.BonusPang;
                    WriteConsole.WriteLine($"[SHOT_DATA]: {player.GetNickname} OB! (+2 penalty)", ConsoleColor.Red);
                    break;

                default:
                    player.GameInfo.GameData.ShotCount += 1;
                    player.GameInfo.GameData.TotalShot += 1;
                    player.GameInfo.GameData.Pang = S.Pang;
                    player.GameInfo.GameData.BonusPang = S.BonusPang;
                    break;
            }

            // ✅ Ball consumption
            try
            {
                var currentBall = player.Inventory.ItemWarehouse.GetItem(player.Inventory.BallTypeID, 1);
                if (currentBall != null && currentBall.ItemC0 > 0)
                {
                    uint oldQty = currentBall.ItemC0;
                    if (currentBall.RemoveQuantity(1))
                    {
                        WriteConsole.WriteLine($"[BALL_CONSUME]: {player.GetNickname} used ball - Qty: {oldQty} → {currentBall.ItemC0}", ConsoleColor.Yellow);
                        
                        if (currentBall.ItemC0 == 0)
                        {
                            var defaultBall = player.Inventory.ItemWarehouse.GetItem(335544320, 1);
                            if (defaultBall != null)
                            {
                                player.Inventory.SetBallTypeID(335544320);
                                player.SendResponse(new byte[] { 0x4B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 });
                            }
                        }
                        
                        currentBall.ItemNeedUpdate = true;
                        player.Inventory.ItemWarehouse.Update(currentBall);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole.WriteLine($"[BALL_CONSUME_ERROR]: {ex.Message}", ConsoleColor.Red);
            }

            double distAfter = S.Pos.HoleDistance(player.GameInfo.HolePos3D);
            _playerDistances[player.ConnectionID] = distAfter;
            player.GameInfo.Versus.HoleDistance = distAfter;
            
            WriteConsole.WriteLine($"[SHOT_DATA]: Distance: {distBefore:F2}m → {distAfter:F2}m ({(distAfter - distBefore):+0.00;-0.00}m)", ConsoleColor.Cyan);
            
            // ✅ Debug log สำหรับ distance calculation
            WriteConsole.WriteLine($"[SHOT_DATA_DEBUG]: Ball Pos: ({S.Pos.X:F2}, {S.Pos.Z:F2}), Hole Pos: ({player.GameInfo.HolePos3D.X:F2}, {player.GameInfo.HolePos3D.Z:F2})", ConsoleColor.DarkGray);

            // ✅ Send packet 0x64 (Shot Data) to shooter only
            var shotPacketResult = new PangyaBinaryWriter();
            shotPacketResult.Write(new byte[] { 0x64, 0x00 });
            shotPacketResult.WriteStruct(S);
            player.SendResponse(shotPacketResult.GetBytes());
            WriteConsole.WriteLine($"[SHOT_DATA]: → Sent 0x64 to shooter {player.GetNickname}", ConsoleColor.Gray);
            
            // ✅ Send packet 0x6E (Ball Position Update)
            // IMPORTANT: ใน Versus แต่ละคนต้องมีลูกของตัวเอง
            // ถ้าส่ง 0x6E broadcast ไปทุกคน จะทำให้ client อัปเดต "ตำแหน่งลูกตัวเอง" เป็นของคนอื่น
            // แล้วคนถัดไปจะเริ่มตีจากจุดตกของคนก่อน
            var posPacket = new PangyaBinaryWriter();
            posPacket.Write(new byte[] { 0x6E, 0x00 });
            posPacket.Write(player.ConnectionID);
            posPacket.Write((byte)player.GameInfo.HolePos);
            posPacket.WriteSingle((float)S.Pos.X);
            posPacket.WriteSingle((float)S.Pos.Z);
            posPacket.Write(S.MatchData);
            player.SendResponse(posPacket.GetBytes());
            WriteConsole.WriteLine($"[SHOT_DATA]: 📡 Sent 0x6E (ball position) to shooter only [ConnID:{player.ConnectionID}]", ConsoleColor.Gray);
        }

        public override void PlayerShotInfo(GPlayer player, Packet packet)
        {
            var ShotType = packet.ReadUInt16();
            byte[] UN;
            var resp = new PangyaBinaryWriter();
            resp.Write(new byte[] { 0x55, 0x00 });
            resp.Write(player.ConnectionID);
            
            switch (ShotType)
            {
                case 1:
                    packet.Skip(9);
                    UN = packet.ReadBytes(61);
                    resp.Write(UN);
                    break;
                default:
                    UN = packet.ReadBytes(61);
                    resp.Write(UN);
                    break;
            }
            Send(resp);
        }

        public override void PlayerSyncShot(GPlayer client, Packet packet)
        {
            client.GameInfo.Versus.ShotSync = true;
            
            lock (_stateLock)
            {
                _syncShotFlags[client.ConnectionID] = true;
            }
            
            // ✅ Log current sync status
            var syncedCount = _syncShotFlags.Values.Count(v => v);
            var totalCount = Players.Count;
            WriteConsole.WriteLine($"[SYNC_SHOT]: {client.GetNickname} synced ({syncedCount}/{totalCount})", ConsoleColor.Cyan);

            if (AllPlayersReady(_syncShotFlags))
            {
                WriteConsole.WriteLine($"[SYNC_SHOT]: ✅ All players synced!", ConsoleColor.Green);
                
                if (_playerTurn != null)
                    Send(ShowDropItem(_playerTurn.ConnectionID));

                lock (_stateLock)
                {
                    foreach (var key in _syncShotFlags.Keys.ToList()) _syncShotFlags[key] = false;
                    _currentState = VersusState.END_SHOT;
                }

                // ✅ Change turn immediately (ไม่ใช้ Task.Delay)
                ChangeTurn();
            }
            else
            {
                // ✅ Auto-sync spectators after 2 seconds
                var playersNotSynced = Players.Where(p => !_syncShotFlags[p.ConnectionID]).ToList();
                WriteConsole.WriteLine($"[SYNC_SHOT]: ⏳ Waiting for: {string.Join(", ", playersNotSynced.Select(p => p.GetNickname))}", ConsoleColor.Yellow);
                
                // ✅ Start timeout timer (5 seconds)
                System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
                {
                    lock (_stateLock)
                    {
                        if (_currentState == VersusState.SHOOTING)
                        {
                            WriteConsole.WriteLine($"[SYNC_SHOT]: ⚠️ TIMEOUT! Force-syncing remaining players...", ConsoleColor.Red);
                            
                            // Force sync all players
                            foreach (var key in _syncShotFlags.Keys.ToList())
                            {
                                if (!_syncShotFlags[key])
                                {
                                    var p = Players.FirstOrDefault(pl => pl.ConnectionID == key);
                                    WriteConsole.WriteLine($"[SYNC_SHOT]: 🔧 Force-synced {p?.GetNickname ?? "Unknown"}", ConsoleColor.DarkYellow);
                                    _syncShotFlags[key] = true;
                                }
                            }
                            
                            // Check again
                            if (AllPlayersReady(_syncShotFlags))
                            {
                                if (_playerTurn != null)
                                    Send(ShowDropItem(_playerTurn.ConnectionID));

                                foreach (var key in _syncShotFlags.Keys.ToList()) _syncShotFlags[key] = false;
                                _currentState = VersusState.END_SHOT;
                                
                                ChangeTurn();
                            }
                        }
                    }
                });
            }
        }

        public void HandleFinishShot(GPlayer player, Packet packet)
        {
            WriteConsole.WriteLine($"[FINISH_SHOT]: {player.GetNickname} (VS mode - optional)", ConsoleColor.DarkGray);
        }

        // ✅ C++ - changeTurn()
        private void ChangeTurn()
        {
            WriteConsole.WriteLine($"[CHANGE_TURN]: Calculating next turn...", ConsoleColor.Magenta);

            bool allFinished = Players.All(p => p.GameInfo.GameData.HoleComplete);
            
            if (allFinished)
            {
                WriteConsole.WriteLine($"[CHANGE_TURN]: ✅ All finished hole - Going to next hole", ConsoleColor.Green);
                ClearAllSyncFlags();
                PrepareNextHole();
                return;
            }

            _playerTurn = CalculatePlayerTurn();
            
            if (_playerTurn == null)
            {
                WriteConsole.WriteLine($"[CHANGE_TURN]: ⚠️ No player turn - Ending hole", ConsoleColor.Yellow);
                PrepareNextHole();
                return;
            }

            ClearAllSyncFlags();
            
            lock (_stateLock)
            {
                _currentState = VersusState.WAIT_HIT_SHOT;
            }

            SendWind();
            Send(ShowWhoPlay(_playerTurn.ConnectionID));
            
            WriteConsole.WriteLine($"[CHANGE_TURN]: ▶️ {_playerTurn.GetNickname} plays next (ConnID:{_playerTurn.ConnectionID})", ConsoleColor.Green);
        }

        private void HandleNextStep()
        {
            switch (_nextStepFlag)
            {
                case 1:
                    Send(new byte[] { 0x92, 0x00 });
                    WriteConsole.WriteLine($"[NEXT_STEP]: Asked if continue", ConsoleColor.Yellow);
                    break;

                case 2:
                    WriteConsole.WriteLine($"[NEXT_STEP]: Ending game", ConsoleColor.Red);
                    SendGameResult();
                    break;

                case 3:
                    WriteConsole.WriteLine($"[NEXT_STEP]: Player quit", ConsoleColor.Yellow);
                    ChangeTurn();
                    break;
            }

            _nextStepFlag = 0;
        }

        private void PrepareNextHole()
        {
            WriteConsole.WriteLine($"[NEXT_HOLE]: Preparing...", ConsoleColor.Cyan);

            foreach (var player in Players)
            {
                player.GameInfo.Versus.LoadComplete = false;
                player.GameInfo.GameData.HoleComplete = false;
                player.GameInfo.GameData.ShotCount = 0;
                player.GameInfo.Versus.ShotSync = false;
                
                _playerDistances[player.ConnectionID] = 99999999;
                player.GameInfo.Versus.HoleDistance = 99999999;
            }

            lock (_stateLock)
            {
                _currentState = VersusState.LOAD_HOLE;
                foreach (var key in _loadHoleFlags.Keys.ToList()) _loadHoleFlags[key] = false;
                _gameInitialized = false; // ✅ Reset flag สำหรับหลุมใหม่
            }

            if (Holes.GoToNext())
            {
                WriteConsole.WriteLine($"[NEXT_HOLE]: → Hole {Holes.CurrentHole.Hole}", ConsoleColor.Green);
                Send(new byte[] { 0x65, 0x00 });
                HoleComplete = false;
            }
            else
            {
                WriteConsole.WriteLine($"[NEXT_HOLE]: Game finished!", ConsoleColor.Magenta);
                SendGameResult();
            }
        }

        private void SendGameResult()
        {
            WriteConsole.WriteLine($"[GAME_RESULT]: Sending results...", ConsoleColor.Green);

            lock (_stateLock)
            {
                _versusActive = false;
                _currentState = VersusState.WAIT_END_GAME;
            }

            CopyScore();
            GenerateExperience();

            foreach (var player in Players)
            {
                Send(ShowNameScore(player.GetNickname, player.GameInfo.GameData.Score, player.GameInfo.GameData.Pang));
            }

            SendUnfinishedData();
            Send(new byte[] { 0x8C, 0x00 });

            foreach (var player in Players)
            {
                SendMatchData(player);
            }

            Started = false;
            WriteConsole.WriteLine($"[GAME_RESULT]: ✅ Done!", ConsoleColor.Green);
        }

        public override void PlayerStartGame()
        {
            if (Started)
            {
                WriteConsole.WriteLine("[START_GAME]: Already started!");
                return;
            }

            WriteConsole.WriteLine($"[START_GAME]: Starting with {Players.Count} players", ConsoleColor.Green);

            ClearPlayerData();

            Gold = Silver1 = Silver2 = Bronze1 = Bronze2 = Bronze3 = 0xFFFFFFFF;
            BestRecovery = BestChipIn = BestDrive = BestSpeeder = LongestPutt = LuckyAward = 0xFFFFFFFF;

            Started = true;
            Await = true;
            
            // ✅ Build hole
            BuildHole();
            
            // ✅ Initialize flags
            InitializeSyncFlags();
            
            lock (_stateLock)
            {
                _versusActive = true;
                _currentState = VersusState.WAIT_HIT_SHOT;
            }

            Send(new byte[] { 0x30, 0x02 });
            Send(new byte[] { 0x31, 0x02 });
            Send(ShowPangRate());

            Update(this);
            GameStart = DateTime.Now;
            
            WriteConsole.WriteLine($"[START_GAME]: ✅ Started!", ConsoleColor.Green);
        }

        // ✅ Override methods
        public override void DestroyRoom()
        {
            throw new NotImplementedException();
        }

        public override byte[] GameInformation()
        {
            var response = new PangyaBinaryWriter();
            response.WriteStr(fGameData.Name, 64);
            response.Write(fGameData.Password.Length > 0 ? false : true);
            response.Write(Started == true ? (byte)0 : (byte)1);
            response.Write(Await);
            response.Write(fGameData.MaxPlayer);
            response.Write((byte)Players.Count);
            response.Write(GameKey, 17);
            response.Write(fGameData.Time30S);
            response.Write(fGameData.HoleTotal);
            response.Write((byte)GameType);
            response.Write((ushort)ID);
            response.Write(fGameData.Mode);
            response.Write(fGameData.Map);
            response.Write(fGameData.VSTime);
            response.Write(fGameData.GameTime);
            response.Write(0);
            response.Write(Idle);
            response.Write(fGameData.GMEvent);
            response.WriteZero(76);
            response.Write(100);
            response.Write(100);
            response.Write(Owner);
            response.Write((byte)0x00);
            response.Write(fGameData.Artifact);
            response.Write(fGameData.NaturalMode);
            response.Write(fGameData.GPTypeID);
            response.Write(fGameData.GPTypeIDA);
            response.Write(fGameData.GPTime);
            response.Write(Iff<uint>(fGameData.GP, 1, 0));
            return response.GetBytes();
        }

        public override byte[] GetGameHeadData()
        {
            var response = new PangyaBinaryWriter();
            response.Write(new byte[] { 0x4A, 0x00, 0xFF, 0xFF });
            response.Write((byte)GameType);
            response.Write(fGameData.Map);
            response.Write(fGameData.HoleTotal);
            response.Write(fGameData.Mode);
            response.Write(fGameData.NaturalMode);
            response.Write(fGameData.MaxPlayer);
            response.Write(fGameData.Time30S);
            response.Write(Idle);
            response.Write(fGameData.VSTime);
            response.Write(fGameData.GameTime);
            response.Write(0);
            response.Write(fGameData.Password.Length > 0 ? false : true);
            if (fGameData.Password.Length > 0)
                response.WritePStr(fGameData.Password);
            response.WritePStr(fGameData.Name);
            return response.GetBytes();
        }

        public override void GenerateExperience()
        {
            foreach (var P in Players)
            {
                P.GameInfo.GameData.EXP = new GameExpTable().GetEXP(GAME_TYPE.VERSUS_STROKE, Map, 0, (byte)Players.Count, P.GameInfo.GameData.HoleCompletedCount);
            }
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
                
                if ((uint)player.GetUID == Owner && Players.Count >= 1)
                    FindNewMaster();

                Update(this);
                player.SendResponse(ShowLeaveGame());
                player.Game = null;
                FirstShot = false;
            }
        }

        public override void PlayerLeavePractice()
        {
        }

        public override void PlayerSendFinalResult(GPlayer player, Packet packet)
        {
        }

        public override void SendHoleData(GPlayer player)
        {
            var H = Holes.CurrentHole;
            if (H == null) return;

            var Data = Holes.CurrentHole;
            player.SendResponse(ShowWind(Data.WindPower, Data.WindDirection));
            player.SendResponse(ShowWeather(Data.Weather));
        }

        public override void SendPlayerOnCreate(GPlayer player)
        {
            if (player.GetCapability == 4 || player.GetCapability == 15)
                player.Visible = 4;

            var packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x48, 0x00 });
            packet.WriteByte(0);
            packet.Write(new byte[] { 0xFF, 0xFF });
            packet.WriteByte(1);
            packet.Write(player.GetGameInfomations(2));
            packet.Write((byte)0);
            player.SendResponse(packet.GetBytes());
        }

        public override void SendPlayerOnJoin(GPlayer player)
        {
            if (player.GetCapability == 4 || player.GetCapability == 15)
                player.Visible = 4;

            var packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x48, 0x00 });
            packet.Write((byte)0);
            packet.Write(new byte[] { 0xFF, 0xFF });
            packet.WriteByte(Players.Count);
            
            foreach (var P in Players)
                packet.Write(P.GetGameInfomations(2));
            
            packet.Write((byte)0);
            Send(packet.GetBytes());

            packet = new PangyaBinaryWriter();
            packet.Write(new byte[] { 0x48, 0x00 });
            packet.Write((byte)1);
            packet.Write(new byte[] { 0xFF, 0xFF });
            packet.Write(player.GetGameInfomations(2));
            Send(packet.GetBytes());
        }

        // ✅ Override PlayerHoleData เพื่อ set hole position ให้ถูกต้อง
        public override void PlayerHoleData(GPlayer player, Packet packet)
        {
            var H = (HoleData)packet.Read(new HoleData());
            
            // ✅ Update hole position for THIS player
            player.GameInfo.HolePos3D.X = H.X;
            player.GameInfo.HolePos3D.Z = H.Z;
            player.GameInfo.HolePos = H.HolePosition;
            player.GameInfo.GameData.ParCount = (sbyte)H.Par;
            
            WriteConsole.WriteLine($"[HOLE_DATA]: {player.GetNickname} - Hole Pos: ({H.X:F2}, {H.Z:F2}), Par: {H.Par}", ConsoleColor.Cyan);
            
            // ✅ Log สำหรับ Debug Distance Calculation
            WriteConsole.WriteLine($"[HOLE_DATA_DEBUG]: Player HolePos3D set - X={player.GameInfo.HolePos3D.X:F2}, Z={player.GameInfo.HolePos3D.Z:F2}", ConsoleColor.DarkGray);
            
            // ✅ FIX: Reset Ball Position สำหรับ Versus Mode - แต่ละคนต้องเริ่มจากจุดเดียวกัน
            // ใน Versus Mode ลูกของทุกคนเริ่มจากตำแหน่งเดียวกัน (Tee Position)
            // ไม่ใช่ตีต่อจากตำแหน่งคนอื่น!
            
            // ✅ ถ้าเป็นหลุม Natural Mode ให้ตั้ง shot count
            if ((fGameData.NaturalMode & 2) != 0)
            {
                switch (H.Par)
                {
                    case 4:
                        player.GameInfo.GameData.ShotCount = 2;
                        break;
                    case 5:
                        player.GameInfo.GameData.ShotCount = 3;
                        break;
                    default:
                        player.GameInfo.GameData.ShotCount = 1;
                        break;
                }
            }
            else
            {
                player.GameInfo.GameData.ShotCount = 1;
            }
            
            // ✅ Send hole data (wind/weather) back to player
            SendHoleData(player);
        }

        public override bool Validate()
        {
            if (fGameData.MaxPlayer > 4) return false;
            return true;
        }
    }
}