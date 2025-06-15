using LiteNetLib;
using System.Net;
using System.Net.Sockets;

namespace DeathRoom.GameServer
{
    public class GameServer : INetEventListener
    {
        private readonly NetManager _netManager;

        public GameServer()
        {
            _netManager = new NetManager(this);
        }

        public void Start()
        {
            _netManager.Start(9050); 
            Console.WriteLine("Server started on port 9050");

            while (!Console.KeyAvailable)
            {
                _netManager.PollEvents();
                Thread.Sleep(15);
            }
        }

        public void Stop()
        {
            _netManager.Stop();
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine($"Peer connected: {peer.Port}");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"Peer disconnected: {peer.Port}. Reason: {disconnectInfo.Reason}");
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"Network error: {socketError}");
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            Console.WriteLine($"Received data from {peer.Port}");
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey("SomeConnectionKey");
        }
    }
} 