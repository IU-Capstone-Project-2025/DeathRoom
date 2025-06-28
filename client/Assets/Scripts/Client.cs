using System;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using MessagePack;
using DeathRoom.Common.network;
using DeathRoom.Network;

public class Client : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverAddress = "localhost";
    public int serverPort = 9050;
    public string playerName = "Player";

    [Header("Player")]
    public GameObject localPlayerPrefab;
    public GameObject networkPlayerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private NetManager netManager;
    private EventBasedNetListener netListener;
    private NetPeer serverPeer;

    public GameObject localPlayer;
    private PlayerMovement localPlayerMovement;
    public Dictionary<int, NetworkPlayer> networkPlayers = new Dictionary<int, NetworkPlayer>();

    private float sendRate = 20f; // 20 updates per second
    private float nextSendTime = 0f;

    public bool isConnected = false;
    private long lastServerTick = 0;

    void Start()
    {
        InitializeNetwork();
        ConnectToServer();
    }

    void InitializeNetwork()
    {
        netListener = new EventBasedNetListener();

        netListener.PeerConnectedEvent += OnConnected;
        netListener.PeerDisconnectedEvent += OnDisconnected;
        netListener.NetworkReceiveEvent += OnNetworkReceive;
        netListener.NetworkErrorEvent += OnNetworkError; // теперь с правильной сигнатурой

        netManager = new NetManager(netListener);
        netManager.Start();
    }

    public void ConnectToServer()
    {
        Debug.Log($"Connecting to server {serverAddress}:{serverPort}...");
        serverPeer = netManager.Connect(serverAddress, serverPort, "DeathRoomSecret");
    }

    void OnConnected(NetPeer peer)
    {
        Debug.Log($"Connected to server: {peer}");
        isConnected = true;
        SendLoginPacket();
    }

    void OnDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"Disconnected. Reason: {disconnectInfo.Reason}");
        isConnected = false;

        if (localPlayer != null)
        {
            Destroy(localPlayer);
            localPlayer = null;
        }

        foreach (var p in networkPlayers.Values)
        {
            if (p != null && p.gameObject != null) Destroy(p.gameObject);
        }
        networkPlayers.Clear();
    }

    void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var data = reader.GetRemainingBytes();
        ProcessPacket(data);
    }

    void OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Debug.LogError($"Network error: {socketErrorCode}");
    }

    void Update()
    {
        netManager?.PollEvents();

        if (isConnected && localPlayer != null && Time.time >= nextSendTime)
        {
            SendPlayerMovement();
            nextSendTime = Time.time + (1f / sendRate);
        }
    }

    void FixedUpdate()
    {
        netManager?.PollEvents();
    }

    void OnDestroy()
    {
        netManager?.Stop();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) netManager?.Stop();
    }

    void ProcessPacket(byte[] data)
    {
        if (data.Length == 0) return;

        var packetType = (PacketType)data[0];
        var packetData = new byte[data.Length - 1];
        Buffer.BlockCopy(data, 1, packetData, 0, packetData.Length);

        switch (packetType)
        {
            case PacketType.WorldState:
                ProcessWorldStatePacket(packetData);
                break;
            default:
                Debug.Log($"Unknown packet type: {packetType}");
                break;
        }
    }

    void ProcessWorldStatePacket(byte[] data)
    {
        try
        {
            var worldState = MessagePackSerializer.Deserialize<WorldStatePacket>(data);
            lastServerTick = worldState.ServerTick;

            foreach (var ps in worldState.Players.Values)
            {
                UpdateNetworkPlayer(ps);
            }

            var toRemove = new List<int>();
            foreach (var kvp in networkPlayers)
            {
                if (!worldState.Players.ContainsKey(kvp.Key)) toRemove.Add(kvp.Key);
            }

            toRemove.ForEach(RemoveNetworkPlayer);

        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing WorldState: {e}");
        }
    }

    void UpdateNetworkPlayer(PlayerState ps)
    {
        if (localPlayerMovement != null && ps.Username == playerName) return;

        if (!networkPlayers.ContainsKey(ps.PlayerId))
        {
            CreateNetworkPlayer(ps);
        }
        else
        {
            networkPlayers[ps.PlayerId]?.UpdateState(ps);
        }
    }

    void CreateNetworkPlayer(PlayerState ps)
    {
        if (networkPlayerPrefab == null)
        {
            Debug.LogError("No networkPlayerPrefab assigned!");
            return;
        }

        Vector3 spawnPos = ps.Position != Vector3.zero ? UnityVector3(ps.Position) : GetRandomSpawnPoint();
        GameObject go = Instantiate(networkPlayerPrefab, spawnPos, Quaternion.identity);
        var nw = go.GetComponent<NetworkPlayer>() ?? go.AddComponent<NetworkPlayer>();
        nw.Initialize(ps);
        networkPlayers[ps.PlayerId] = nw;
        Debug.Log($"Created network player {ps.Username} (ID {ps.PlayerId})");
    }

    void RemoveNetworkPlayer(int id)
    {
        if (networkPlayers.TryGetValue(id, out var np))
        {
            if (np != null) Destroy(np.gameObject);
            networkPlayers.Remove(id);
            Debug.Log($"Removed network player ID {id}");
        }
    }

    void SendLoginPacket()
    {
        var lp = new LoginPacket { Username = playerName };
        SendPacket(PacketType.Login, lp);
        SpawnLocalPlayer();
    }

    void SpawnLocalPlayer()
    {
        if (localPlayer != null) return;
        if (localPlayerPrefab == null)
        {
            Debug.LogError("No localPlayerPrefab assigned!");
            return;
        }
        Vector3 spawn = GetRandomSpawnPoint();
        localPlayer = Instantiate(localPlayerPrefab, spawn, Quaternion.identity);
        localPlayerMovement = localPlayer.GetComponent<PlayerMovement>();
        if (localPlayerMovement == null)
            Debug.LogError("Local prefab lacks PlayerMovement!");
        Debug.Log($"Spawned local player {playerName}");
    }

    void SendPlayerMovement()
    {
        if (localPlayerMovement == null) return;

        var pkt = new PlayerMovePacket
        {
            Position = localPlayer.transform.position,
            Rotation = localPlayer.transform.eulerAngles
        };
        SendPacket(PacketType.PlayerMove, pkt);
    }

    public void SendShoot(Vector3 direction, int targetId = -1)
    {
        if (!isConnected) return;

        var sp = new PlayerShootPacket { Direction = direction };
        SendPacket(PacketType.PlayerShoot, sp);

        if (targetId > 0)
        {
            var hp = new PlayerHitPacket
            {
                TargetId = targetId,
                ClientTick = lastServerTick,
                Direction = direction
            };
            SendPacket(PacketType.PlayerHit, hp);
        }
    }

    void SendPacket<T>(PacketType type, T packet) where T : class
    {
        if (!isConnected || serverPeer == null) return;

        try
        {
            var body = MessagePackSerializer.Serialize(packet);
            var data = new byte[1 + body.Length];
            data[0] = (byte)type;
            Buffer.BlockCopy(body, 0, data, 1, body.Length);
            serverPeer.Send(data, DeliveryMethod.ReliableOrdered);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending {type}: {e}");
        }
    }

    Vector3 UnityVector3(Vector3 v) => new Vector3(v.x, v.y, v.z);
    Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;
        return Vector3.zero;
    }
}
