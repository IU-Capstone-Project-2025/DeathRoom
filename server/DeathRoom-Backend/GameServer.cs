using LiteNetLib;
using MessagePack;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.Network;
using DeathRoom.Application;

namespace DeathRoom.GameServer;

    public class GameServer : INetEventListener
    {
        private readonly NetManager _netManager;
    private readonly PlayerSessionService _playerSessionService;
    private readonly WorldStateService _worldStateService;
    private readonly GameLoopService _gameLoopService;
    private readonly PacketHandlerService _packetHandlerService;
    private Task? _gameLoopTask;

    public GameServer(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        GameLoopService gameLoopService,
        PacketHandlerService packetHandlerService)
    {
        Console.WriteLine("[GameServer] Конструктор вызван");
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _gameLoopService = gameLoopService;
        _packetHandlerService = packetHandlerService;
        _netManager = new NetManager(this);

        // Внедряю реальный делегат для рассылки состояния мира
        var loopServiceType = _gameLoopService.GetType();
        var field = loopServiceType.GetField("_broadcastWorldState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(_gameLoopService, (Func<DeathRoom.Domain.WorldState, Task>)(ws =>
            {
                var packet = new DeathRoom.Common.Network.WorldStatePacket
                {
                    PlayerStates = ws.PlayerStates.Select(p => new DeathRoom.Common.Dto.PlayerState
                    {
                        Id = p.Id,
                        Username = p.Username,
                        Position = new DeathRoom.Common.Dto.Vector3Serializable { X = p.Position.X, Y = p.Position.Y, Z = p.Position.Z },
                        Rotation = new DeathRoom.Common.Dto.Vector3Serializable { X = p.Rotation.X, Y = p.Rotation.Y, Z = p.Rotation.Z },
                        HealthPoint = p.HealthPoint,
                        MaxHealthPoint = p.MaxHealthPoint,
                        ArmorPoint = p.ArmorPoint,
                        MaxArmorPoint = p.MaxArmorPoint,
                        ArmorExpirationTick = p.ArmorExpirationTick
                    }).ToList(),
                    ServerTick = ws.ServerTick
                };
                var data = MessagePack.MessagePackSerializer.Serialize<DeathRoom.Common.Network.IPacket>(packet);
                _netManager.SendToAll(data, LiteNetLib.DeliveryMethod.Unreliable);
                return Task.CompletedTask;
            }));
        }
    }

    public NetManager NetManager => _netManager;

    public Task Start(CancellationToken cancellationToken)
    {
        Console.WriteLine("[GameServer] Start: запуск NetManager и игрового цикла");
        bool started = _netManager.Start(9050);
        Console.WriteLine($"[GameServer] NetManager.Start(9050) вернул: {started}");
        if (started)
            Console.WriteLine("[GameServer] NetManager успешно стартовал и слушает порт 9050");
        else
            Console.WriteLine("[GameServer] ОШИБКА: NetManager не смог стартовать порт 9050! Возможно, порт занят или нет прав.");
        Console.WriteLine("[GameServer] Запуск игрового цикла...");
        _gameLoopTask = _gameLoopService.RunAsync(cancellationToken);
        Console.WriteLine("[GameServer] GameLoopService.RunAsync вызван");
        return _gameLoopTask;
    }

        public void Stop()
        {
        Console.WriteLine("[GameServer] Stop: остановка NetManager");
        _netManager.Stop();
        Console.WriteLine("[GameServer] Stop: NetManager остановлен");
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Peer connected: {peer.Port}. Waiting for login.");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
        _playerSessionService.TryRemoveSession(peer, out var playerState);
        if (playerState != null)
            _playerSessionService.RemoveInMemoryPlayer(playerState.Id);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"Network error: {socketError}");
		}

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
            {
                var data = reader.GetRemainingBytes();
                _packetHandlerService.HandlePacket(peer, data);
                        }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("DeathRoomSecret");
    }
} 
