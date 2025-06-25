using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.dto;
using System.Collections.Concurrent;
using DeathRoom.Common.network;
using DeathRoom.Data;
using DeathRoom.Data.Entities;

namespace DeathRoom.GameServer
{
    public class GameServer : INetEventListener
    {
		private const float CYLINDER_RELATIVE_HEIGHT = 2;
		private Vector3 CYLINDER_SHIFT = new Vector3(0, 0, CYLINDER_RELATIVE_HEIGHT);
        private const int MAX_SNAPSHOTS = 64;
        private long _serverTick = 0;

        private readonly NetManager _netManager;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConcurrentDictionary<NetPeer, PlayerState> _players = new();
        private readonly GameDbContext _dbContext;

        private readonly int _broadcastIntervalMs = 15;
        private readonly int _idleIntervalMs = 100;

        private readonly int _worldStateHistoryLength = 20;
        private readonly int _worldStateSaveInterval = 10;
        private readonly Queue<(long Tick, WorldStatePacket State)> _worldStateHistory = new();

        // --- In-memory сессии и игроки ---
        private readonly Dictionary<int, PlayerState> _inMemoryPlayers = new();
        private int _nextPlayerId = 1;

		private float angleCos(Vector3 first, Vector3 second) { return (first*second)/(!first*!second); }

        public GameServer(GameDbContext dbContext)
        {
            _netManager = new NetManager(this);
            _dbContext = dbContext;

            if (int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_BROADCAST_INTERVAL_MS"), out var bInt) && bInt > 0)
            {
                _broadcastIntervalMs = bInt;
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_IDLE_INTERVAL_MS"), out var iInt) && iInt > 0)
            {
                _idleIntervalMs = iInt;
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_WORLDSTATE_HISTORY_LENGTH"), out var hLen) && hLen > 0)
            {
                _worldStateHistoryLength = hLen;
            }
            if (int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_WORLDSTATE_SAVE_INTERVAL"), out var sInt) && sInt > 0)
            {
                _worldStateSaveInterval = sInt;
            }
        }

        public void Start()
        {
            var portEnv = Environment.GetEnvironmentVariable("DEATHROOM_SERVER_PORT");
            var port = int.TryParse(portEnv, out var parsedPort) ? parsedPort : 9050;

            if (_netManager.Start(port))
            {
                Console.WriteLine($"Server started on port {port}");
            }
            else
            {
                Console.WriteLine($"Failed to start server on port {port}");
                return;
            }
            
            var gameLoop = new Task(GameLoop, _cancellationTokenSource.Token);
            gameLoop.Start();
        }

        private async void GameLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                _serverTick++;
                _netManager.PollEvents();
                
                if (!_players.IsEmpty)
                {
                    var worldStatePacket = new WorldStatePacket
                    {
                        PlayerStates = _players.Values.Select(p => p.Clone()).ToList(),
                        ServerTick = _serverTick
                    };
                    if (_serverTick % _worldStateSaveInterval == 0)
                    {
                        _worldStateHistory.Enqueue((_serverTick, worldStatePacket));
                        if (_worldStateHistory.Count > _worldStateHistoryLength)
                            _worldStateHistory.Dequeue();
                    }
                    var data = PacketProcessor.Pack(worldStatePacket);
                    _netManager.SendToAll(data, DeliveryMethod.Unreliable);

                    await Task.Delay(_broadcastIntervalMs);
                }
                else
                {
                    await Task.Delay(_idleIntervalMs);
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();

            foreach (var peer in _players.Keys)
            {
                try
                {
                    peer.Disconnect();
                }
                catch { /* ignore */ }
            }

            _netManager.Stop();
            Console.WriteLine("Server stopped.");
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Peer connected: {peer.Port}. Waiting for login.");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (_players.TryRemove(peer, out var playerState))
            {
                Console.WriteLine($"Player {playerState.Username} disconnected. Reason: {disconnectInfo.Reason}");
                _inMemoryPlayers.Remove(playerState.Id);
                // --- Старый код с БД закомментирован ---
                /*
                var player = _dbContext.Players.Find(playerState.Id);
                if (player != null)
                {
                    player.LastSeen = DateTime.UtcNow;
                    _dbContext.SaveChanges();
                }
                */
            }
            else
            {
                Console.WriteLine($"Unauthenticated peer disconnected: {peer.Port}. Reason: {disconnectInfo.Reason}");
            }
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"Network error: {socketError}");
        }

		public void OnHitRegistred(PlayerState shooter, KeyValuePair<NetPeer, PlayerState> playerHit, int damage) {
			Console.WriteLine($"Player {shooter.Username} dealed {damage} damage to {playerHit.Value.Username}.");
			playerHit.Value.HealthPoint -= damage;
			if (playerHit.Value.HealthPoint <= 0)
			{
				Console.WriteLine($"Player {playerHit.Value.Username} died!");
				// Disconnect and remove player
				if (_players.TryRemove(playerHit.Key, out var _))
				{
					_inMemoryPlayers.Remove(playerHit.Value.Id);
					try { playerHit.Key.Disconnect(); } catch { /* ignore */ }
				}
			}
		}

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var data = reader.GetRemainingBytes();
            var (type, packet) = PacketProcessor.Unpack(data);

            if (packet is LoginPacket loginPacket)
            {
                if (_players.ContainsKey(peer)) return;

                // --- In-memory регистрация игрока ---
                var playerState = new PlayerState
                {
                    Id = _nextPlayerId++,
                    Username = loginPacket.Username,
                    Position = new Vector3(),
                    Rotation = new Vector3(),
                    HealthPoint = 100,
                    MaxHealthPoint = 100
                };
                _inMemoryPlayers[playerState.Id] = playerState;
                _players.TryAdd(peer, playerState);
                Console.WriteLine($"Player {playerState.Username} logged in from {peer.Port}");

                // --- Старый код с БД закомментирован ---
                /*
                var player = _dbContext.Players.FirstOrDefault(p => p.Login == loginPacket.Username);
                if (player == null)
                {
                    player = new Player
                    {
                        Login = loginPacket.Username,
                        Rating = 0,
                        HashedPassword = "",
                        Nickname = "",
                        LastSeen = DateTime.UtcNow
                    };
                    _dbContext.Players.Add(player);
                    _dbContext.SaveChanges();
                }
                */
            }
            else if (packet is PlayerMovePacket movePacket)
            {
                if (_players.TryGetValue(peer, out var playerState))
                {
                    playerState.Position = movePacket.Position;
                    playerState.Rotation = movePacket.Rotation;

                    var snapshot = new PlayerSnapshot
                    {
                        ServerTick = _serverTick,
                        Position = movePacket.Position,
                        Rotation = movePacket.Rotation
                    };
                    
                    playerState.Snapshots.Enqueue(snapshot);
                    
                    if (playerState.Snapshots.Count > MAX_SNAPSHOTS)
                    {
                        playerState.Snapshots.Dequeue();
                    }
                }
            }
            else if (packet is PlayerHitPacket hitPacket)
            {
                if (_players.TryGetValue(peer, out var shooterState))
                {
                    Console.WriteLine($"[HIT] Player {shooterState.Username} claims hit on {hitPacket.TargetId} at tick {hitPacket.ClientTick}");
                    var worldStateAtShot = GetWorldStateAtTick(hitPacket.ClientTick);
                    if (worldStateAtShot == null)
                    {
                        Console.WriteLine($"[LagComp] Нет состояния мира на тик {hitPacket.ClientTick}");
                        return;
                    }
                    var shooter = worldStateAtShot.PlayerStates.FirstOrDefault(p => p.Id == shooterState.Id);
                    var target = worldStateAtShot.PlayerStates.FirstOrDefault(p => p.Id == hitPacket.TargetId);
                    if (shooter == null || target == null)
                    {
                        Console.WriteLine($"[LagComp] Не найден стрелявший или цель в состоянии мира на тик {hitPacket.ClientTick}");
                        return;
                    }
                    // Проверка попадания (raycast/углы)
                    Vector3 shooterPos = shooter.Position;
                    Vector3 shootDir = hitPacket.Direction;
                    Vector3 shootProj = shootDir.projection(ProjectionCode.xz);
                    Vector3 radius = target.Position - shooterPos;
                    Vector3 radProj = radius.projection(ProjectionCode.xz);
                    bool projectionHits = !radProj/Math.Sqrt(!radProj*!radProj-1)<=angleCos(radProj, shootProj);
                    if(!projectionHits) return;
                    // Checking bottom sphere
                    Vector3 botRadius = radius - CYLINDER_SHIFT;
                    if(!botRadius/Math.Sqrt(!botRadius*!botRadius-1)<=angleCos(botRadius, shootDir)) {
                        var hitPeer = _players.FirstOrDefault(p => p.Value.Id == target.Id);
                        if (!hitPeer.Equals(default(KeyValuePair<NetPeer, PlayerState>)))
                            OnHitRegistred(shooterState, hitPeer, 10);
                        return;
                    }
                    // Checking top sphere
                    Vector3 topRadius = radius + CYLINDER_SHIFT;
                    if(!topRadius/Math.Sqrt(!topRadius*!topRadius-1)<=angleCos(topRadius, shootDir)) {
                        var hitPeer = _players.FirstOrDefault(p => p.Value.Id == target.Id);
                        if (!hitPeer.Equals(default(KeyValuePair<NetPeer, PlayerState>)))
                            OnHitRegistred(shooterState, hitPeer, 20);
                        return;
                    }
                    // Checking cylinder
                    float yIntersection = shootDir.Y * !radProj/(radius.X*shootDir.X + radius.Z*shootDir.Z);
                    if(radius.Y - CYLINDER_RELATIVE_HEIGHT <= yIntersection || yIntersection <= radius.Y + CYLINDER_RELATIVE_HEIGHT) {
                        var hitPeer = _players.FirstOrDefault(p => p.Value.Id == target.Id);
                        if (!hitPeer.Equals(default(KeyValuePair<NetPeer, PlayerState>)))
                            OnHitRegistred(shooterState, hitPeer, 10);
                        return;
                    }
                    // Если ничего не сработало — промах
                }
            }
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.Accept();
        }

        private WorldStatePacket InterpolateWorldState(long tick, (long Tick, WorldStatePacket State) before, (long Tick, WorldStatePacket State) after)
        {
            var result = new WorldStatePacket();
            float t = (float)(tick - before.Tick) / (after.Tick - before.Tick);
            foreach (var beforePlayer in before.State.PlayerStates)
            {
                var afterPlayer = after.State.PlayerStates.FirstOrDefault(p => p.Id == beforePlayer.Id);
                if (afterPlayer != null)
                {
                    var interpPlayer = beforePlayer.Clone();
                    interpPlayer.Position = InterpolateVector3(beforePlayer.Position, afterPlayer.Position, t);
                    interpPlayer.Rotation = InterpolateVector3(beforePlayer.Rotation, afterPlayer.Rotation, t);
                    interpPlayer.HealthPoint = (int)(beforePlayer.HealthPoint * (1 - t) + afterPlayer.HealthPoint * t);
                    result.PlayerStates.Add(interpPlayer);
                }
            }
            return result;
        }

        private Vector3 InterpolateVector3(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        public WorldStatePacket GetWorldStateAtTick(long tick)
        {
            if (_worldStateHistory.Count == 0) return null;
            var arr = _worldStateHistory.ToArray();
            for (int i = 0; i < arr.Length - 1; i++)
            {
                if (arr[i].Tick <= tick && tick <= arr[i + 1].Tick)
                {
                    if (arr[i].Tick == tick) return arr[i].State;
                    if (arr[i + 1].Tick == tick) return arr[i + 1].State;
                    return InterpolateWorldState(tick, arr[i], arr[i + 1]);
                }
            }
            // если тик меньше самого раннего — возвращаем первый, если больше — последний
            if (tick < arr[0].Tick) return arr[0].State;
            return arr[^1].State;
        }
    }
} 
