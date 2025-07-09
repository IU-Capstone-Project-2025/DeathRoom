using LiteNetLib;
using MessagePack;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.network;
using DeathRoom.Application;

namespace DeathRoom.GameServer;

    public class GameServer : INetEventListener
    {
        private readonly NetManager _netManager;
    private readonly PlayerSessionService _playerSessionService;
    private readonly WorldStateService _worldStateService;
    private readonly GameLoopService _gameLoopService;
    private readonly PacketHandlerService _packetHandlerService;
    private CancellationTokenSource _cts = new();
    private Task? _gameLoopTask;

    public GameServer(
        PlayerSessionService playerSessionService,
        WorldStateService worldStateService,
        GameLoopService gameLoopService,
        PacketHandlerService packetHandlerService)
    {
        _playerSessionService = playerSessionService;
        _worldStateService = worldStateService;
        _gameLoopService = gameLoopService;
        _packetHandlerService = packetHandlerService;
            _netManager = new NetManager(this);
    }

    public NetManager NetManager => _netManager;

    public Task Start(CancellationToken cancellationToken)
    {
        Console.WriteLine("[GameServer] Start: запуск NetManager и игрового цикла");
        _netManager.Start(9050);
        _gameLoopTask = _gameLoopService.RunAsync(cancellationToken);
        return _gameLoopTask;
    }

        public void Stop()
        {
        Console.WriteLine("[GameServer] Stop: остановка NetManager");
        //_netManager.Stop(); // Временно закомментировано для диагностики зависания
        Console.WriteLine("[GameServer] Stop: NetManager остановлен (строка закомментирована)");
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
                var packet = MessagePackSerializer.Deserialize<IPacket>(data);
        _packetHandlerService.HandlePacket(peer, packet);
                        }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("DeathRoomSecret");
    }
} 
