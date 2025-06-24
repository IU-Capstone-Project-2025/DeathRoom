using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.dto;
using System.Collections.Concurrent;
using DeathRoom.Common.network;
using DeathRoom.Data;

namespace DeathRoom.GameServer
{
    public class GameServer : INetEventListener
    {
		private const float CYLINDER_RELATIVE_HEIGHT = 2;
		private Vector3 CYLINDER_SHIFT = new Vector3(0, 0, CYLINDER_RELATIVE_HEIGHT);

        private readonly NetManager _netManager;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConcurrentDictionary<NetPeer, PlayerState> _players = new();
        private readonly GameDbContext _dbContext;

        // Интервалы (мс), задаются переменными окружения DEATHROOM_BROADCAST_INTERVAL_MS и DEATHROOM_IDLE_INTERVAL_MS
        private readonly int _broadcastIntervalMs = 15;
        private readonly int _idleIntervalMs = 100; // период опроса, когда на сервере нет игроков

		private float angleCos(Vector3 first, Vector3 second) { return (first*second)/(!first*!second); }

        public GameServer(GameDbContext dbContext)
        {
            _netManager = new NetManager(this);
            _dbContext = dbContext;

            // читаем интервалы из переменных окружения (если заданы)
            if (int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_BROADCAST_INTERVAL_MS"), out var bInt) && bInt > 0)
            {
                _broadcastIntervalMs = bInt;
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("DEATHROOM_IDLE_INTERVAL_MS"), out var iInt) && iInt > 0)
            {
                _idleIntervalMs = iInt;
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
                _netManager.PollEvents();
                
                if (!_players.IsEmpty)
                {
                    // бродкаст пакетов только при наличии игроков
                    var worldStatePacket = new WorldStatePacket
                    {
                        PlayerStates = _players.Values.ToList()
                    };
                    var data = PacketProcessor.Pack(worldStatePacket);
                    _netManager.SendToAll(data, DeliveryMethod.Unreliable);

                    await Task.Delay(_broadcastIntervalMs);
                }
                else
                {
                    // когда игроков нет — реже опрашиваем
                    await Task.Delay(_idleIntervalMs);
                }
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();

            // мягко отключаем всех игроков
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
                // сохраняем последние данные об игроке
                var player = _dbContext.Players.Find(playerState.Id);
                if (player != null)
                {
                    player.LastSeen = DateTime.UtcNow;
                    _dbContext.SaveChanges();
                }
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

		public void OnHitRegistred(PlayerState shooter, KeyValuePair<NetPeer,PlayerState> playerHit, int damage) {
			Console.WriteLine($"Player {shooter.Username} dealed {damage} damage to {playerHit.Value.Username}.");
			playerHit.Value.HealthPoint = playerHit.Value.HealthPoint - damage;
		}

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var data = reader.GetRemainingBytes();
            var (type, packet) = PacketProcessor.Unpack(data);

            if (packet is LoginPacket loginPacket)
            {
                if (_players.ContainsKey(peer)) return;

                var player = _dbContext.Players.FirstOrDefault(p => p.Username == loginPacket.Username);
                if (player == null)
                {
                    // сюда бы еще айди но пока я думаю как лучше сделать
                    player = new Player
                    {
                        Username = loginPacket.Username,
                        Kills = 0,
                        Deaths = 0,
                        LastSeen = DateTime.UtcNow
                    };
                    _dbContext.Players.Add(player);
                    _dbContext.SaveChanges();
                }

                var playerState = new PlayerState
                {
                    Id = player.Id,
                    Username = player.Username,
                    Position = new Vector3(), 
                    Rotation = new Vector3()
                };
                
                _players.TryAdd(peer, playerState);
                Console.WriteLine($"Player {player.Username} logged in from {peer.Port}");
            }
            else if (packet is PlayerMovePacket movePacket)
            {
                if (_players.TryGetValue(peer, out var playerState))
                {
                    playerState.Position = movePacket.Position;
                    playerState.Rotation = movePacket.Rotation;
                }
            }
            else if (packet is PlayerShootPacket shootPacket)
            {
                if (_players.TryGetValue(peer, out var playerState))
                {
                    Console.WriteLine($"Player {playerState.Username} shot in direction {shootPacket.Direction.X}, {shootPacket.Direction.Y}, {shootPacket.Direction.Z}");
					Vector3 shooterPos = playerState.Position;
					Vector3 shootDir = playerState.Rotation;
					Vector3 shootProj = shootDir.projection(ProjectionCode.xz);
					foreach(var other in _players) {
						if(other.Key == peer) { continue;
						} else {
							Vector3 radius = other.Value.Position - shooterPos;
							// Maybe add some optimizations later
							// Checking xz projection
							Vector3 radProj = radius.projection(ProjectionCode.xz);
							bool projectionHits = !radProj/Math.Sqrt(!radProj*!radProj-1)<=angleCos(radProj, shootProj);
							if(!projectionHits) { // shot missed
								continue;
							}
							// Checking bottom sphere
							Vector3 botRadius = radius - CYLINDER_SHIFT;
							if(!botRadius/Math.Sqrt(!botRadius*!botRadius-1)<=angleCos(botRadius, shootDir)) { // Succesful hit
								OnHitRegistred(playerState, other, 10);
								break;
							}
							// Checking top sphere
							Vector3 topRadius = radius + CYLINDER_SHIFT;
							if(!topRadius/Math.Sqrt(!topRadius*!topRadius-1)<=angleCos(topRadius, shootDir)) { // Succesful hit
								OnHitRegistred(playerState, other, 20);
								break;
							}
							// Checking cylinder
							float yIntersection = shootDir.Y * !radProj/(radius.X*shootDir.X + radius.Z*shootDir.Z);
							if(radius.Y - CYLINDER_RELATIVE_HEIGHT <= yIntersection || yIntersection <= radius.Y + CYLINDER_RELATIVE_HEIGHT) {
								OnHitRegistred(playerState, other, 10);
								break;
							} // Succesful hit
							// If nothing is triggered, then it is misshot
						}
					}
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
    }
} 
