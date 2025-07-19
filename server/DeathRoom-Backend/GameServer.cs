using LiteNetLib;
using MessagePack;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.Network;
using DeathRoom.Application;
using Microsoft.Extensions.Logging;

namespace DeathRoom.GameServer;

public class GameServer : INetEventListener
{
    private readonly NetManager _netManager;
    private readonly PlayerSessionService _playerSessionService;
    private readonly WorldStateService _worldStateService;
    private readonly GameLoopService _gameLoopService;
    private readonly PacketHandlerService _packetHandlerService;
    private Task? _gameLoopTask;
    private Task? _netPollTask;
    private CancellationTokenSource? _netPollCts;
    private readonly ILogger<GameServer> _logger;

    public GameServer(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        GameLoopService gameLoopService,
        PacketHandlerService packetHandlerService,
        ILogger<GameServer> logger)
    {
        _logger = logger;
        _logger.LogInformation("Конструктор вызван");
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _gameLoopService = gameLoopService;
        _packetHandlerService = packetHandlerService;
        _netManager = new NetManager(this);

        // Внедряю реальный делегат для рассылки состояния мира
        var loopServiceType = _gameLoopService.GetType();
        var field = loopServiceType.GetField("_broadcastWorldState",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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

        // Внедряю реальный делегат для рассылки пакетов через PacketHandlerService
        var packetHandlerType = _packetHandlerService.GetType();
        var broadcastField = packetHandlerType.GetField("_broadcastPacket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (broadcastField != null)
        {
            broadcastField.SetValue(_packetHandlerService, (Func<DeathRoom.Common.Network.IPacket, Task>)(packet =>
            {
                var data = MessagePack.MessagePackSerializer.Serialize<DeathRoom.Common.Network.IPacket>(packet);
                _netManager.SendToAll(data, LiteNetLib.DeliveryMethod.ReliableUnordered);
                return Task.CompletedTask;
            }));
        }

        // Внедряю реальный делегат для рассылки пакетов через PacketHandlerService
        var packetHandlerType2 = _packetHandlerService.GetType();
        var broadcastField2 = packetHandlerType2.GetField("_broadcastPacket",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (broadcastField2 != null)
        {
            broadcastField2.SetValue(_packetHandlerService, (Func<DeathRoom.Common.Network.IPacket, Task>)(packet =>
            {
                var data = MessagePack.MessagePackSerializer.Serialize<DeathRoom.Common.Network.IPacket>(packet);
                _netManager.SendToAll(data, LiteNetLib.DeliveryMethod.ReliableUnordered);
                return Task.CompletedTask;
            }));
        }
    }

    public NetManager NetManager => _netManager;

    public Task Start(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Пробую запустить NetManager (LiteNetLib) на порту 9050 (UDP) для всех интерфейсов (0.0.0.0 и [::])");
        bool started = _netManager.Start(9050);
        _logger.LogInformation($"NetManager.Start(9050) вернул: {started}");
        if (started)
        {
            _logger.LogInformation("NetManager успешно стартовал и слушает порт 9050 (UDP) на всех интерфейсах (0.0.0.0 и [::])");
            try
            {
                foreach (var addr in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
                {
                    _logger.LogInformation($"Сервер слушает: {addr}:9050/udp");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Не удалось получить список локальных IP-адресов: {ex.Message}");
            }
        }
        else
        {
            _logger.LogError("ОШИБКА: NetManager не смог стартовать порт 9050! Возможно, порт занят или нет прав.");
        }
        // Запуск PollEvents loop
        _netPollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _netPollTask = Task.Run(() =>
        {
            _logger.LogInformation("NetManager PollEvents loop запущен");
            try
            {
                while (!_netPollCts.Token.IsCancellationRequested)
                {
                    _netManager.PollEvents();
                    Thread.Sleep(1); // 1-10 мс, чтобы не грузить CPU
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"NetManager PollEvents loop: Exception: {ex}");
            }
            _logger.LogInformation("NetManager PollEvents loop завершён");
        }, _netPollCts.Token);
        _logger.LogInformation("Запуск игрового цикла...");
        _gameLoopTask = _gameLoopService.RunAsync(cancellationToken);
        _logger.LogInformation("GameLoopService.RunAsync вызван");
        return _gameLoopTask;
    }

    public void Stop()
    {
        _logger.LogInformation("Stop: остановка NetManager PollEvents loop");
        _netPollCts?.Cancel();
        _netPollTask?.Wait();
        _logger.LogInformation("Stop: NetManager PollEvents loop остановлен");
        _logger.LogInformation("Stop: остановка NetManager");
        _netManager.Stop();
        _logger.LogInformation("Stop: NetManager остановлен");
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _logger.LogInformation($"Peer connected: {peer.Port}. Waiting for login.");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _playerSessionService.TryRemoveSession(peer, out var playerState);
        if (playerState != null)
            _playerSessionService.RemoveInMemoryPlayer(playerState.Id);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        _logger.LogError($"Network error: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        var data = reader.GetRemainingBytes();
        _packetHandlerService.HandlePacket(peer, data);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
        _logger.LogInformation(
            $"[UNCONNECTED] Получен пакет от {remoteEndPoint}, тип: {messageType}, размер: {reader.AvailableBytes}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        _logger.LogInformation($"[CONNECT] Попытка подключения от {request.RemoteEndPoint}");
        request.AcceptIfKey("DeathRoomSecret");
    }
}