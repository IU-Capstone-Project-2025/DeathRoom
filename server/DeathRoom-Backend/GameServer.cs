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
        private readonly NetManager _netManager;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConcurrentDictionary<NetPeer, PlayerState> _players = new();
        private readonly GameDbContext _dbContext;

        public GameServer(GameDbContext dbContext)
        {
            _netManager = new NetManager(this);
            _dbContext = dbContext;
        }

        public void Start()
        {
            _netManager.Start(9050);
            Console.WriteLine("Server started on port 9050");
            
            var gameLoop = new Task(GameLoop, _cancellationTokenSource.Token);
            gameLoop.Start();
        }

        private async void GameLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                _netManager.PollEvents();
                
                // бродкаст пакетов
                var worldStatePacket = new WorldStatePacket
                {
                    PlayerStates = _players.Values.ToList()
                };
                var data = PacketProcessor.Pack(worldStatePacket);
                _netManager.SendToAll(data, DeliveryMethod.Unreliable);

                await Task.Delay(15);
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
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
                    // TODO: написать регистрацию хитов
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