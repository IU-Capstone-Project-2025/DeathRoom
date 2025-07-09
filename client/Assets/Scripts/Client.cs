using System;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.dto;
using MessagePack;
using DeathRoom.Common.network;

public class Client : MonoBehaviour
{
    private string serverAddress = "77.233.222.200";
    [Header("Network Settings")]
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
    
    public Dictionary<int, NetworkPlayer> networkPlayers = new Dictionary<int, NetworkPlayer>();
    public GameObject localPlayer;


    private float sendRate = 20f;
    private float nextSendTime = 0f;

    public bool isConnected = false;
    private long lastServerTick = 0;
    private int localPlayerId = -1;
    

    void Start()
    {
        var resolver = MessagePack.Resolvers.CompositeResolver.Create(
            MessagePack.Resolvers.StandardResolver.Instance
        );
        var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
        MessagePackSerializer.DefaultOptions = options;
        
        InitializeNetwork();
    }

    void InitializeNetwork()
    {
        netListener = new EventBasedNetListener();

        netListener.PeerConnectedEvent += OnConnected;
        netListener.PeerDisconnectedEvent += OnDisconnected;
        netListener.NetworkReceiveEvent += OnNetworkReceive;
        netListener.NetworkErrorEvent += OnNetworkError; 

        netManager = new NetManager(netListener);
        netManager.Start();
    }

    public void ConnectToServer()
    {
        Debug.Log($"Connecting to server {serverAddress}:{serverPort}...");
        serverPeer = netManager.Connect(serverAddress, serverPort, "DeathRoomSecret");
    }

    public void Disconnect()
    {
        if (serverPeer != null && isConnected)
        {
            Debug.Log("Manually disconnecting from server...");
            serverPeer.Disconnect();
        }
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
        localPlayerId = -1;

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
        try
        {
            var packet = MessagePackSerializer.Deserialize<IPacket>(data, MessagePackSerializer.DefaultOptions);
            
            switch (packet)
            {
                case WorldStatePacket worldState:
                    Debug.Log($"Processing WorldStatePacket with {worldState.PlayerStates?.Count} players");
                    Debug.Log($"My player name: {playerName}, My ID: {localPlayerId}");
                    
                    List<int> presentPlayers = new List<int>();
                    foreach (var ps in worldState.PlayerStates)
                    {
                        Debug.Log($"Player in packet: {ps.Username} (ID: {ps.Id}) at position {ps.Position.X}, {ps.Position.Y}, {ps.Position.Z}");
                        
                        if (ps.Username == playerName && localPlayerId == -1)
                        {
                            localPlayerId = ps.Id;
                            Debug.Log($"Set local player ID to: {localPlayerId}");
                        }
                        
                        UpdateNetworkPlayer(ps);
                        presentPlayers.Add(ps.Id);
                    }
                    
                    var toRemove = new List<int>();
                    foreach (var kvp in networkPlayers)
                    {
                        if (!presentPlayers.Contains(kvp.Key)) toRemove.Add(kvp.Key);
                    }
                    toRemove.ForEach(RemoveNetworkPlayer);
                    break;

                case null:
                    Debug.LogError("Unknown packet type");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing packet: {e}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    void UpdateNetworkPlayer(PlayerState ps)
    {
        if (ps.Id == localPlayerId && localPlayerId != -1) 
        {
            Debug.Log($"Skipping local player {ps.Username} (ID: {ps.Id})");
            return;
        }

        if (!networkPlayers.ContainsKey(ps.Id))
        {
            CreateNetworkPlayer(ps);
        }
        else
        {
            networkPlayers[ps.Id]?.UpdateState(ps);
        }
    }

    void CreateNetworkPlayer(PlayerState ps)
    {
        Vector3 spawnPos = ps.Position != Vector3.zero ? UnityVector3(ps.Position) : GetRandomSpawnPoint();
        GameObject go = Instantiate(networkPlayerPrefab, spawnPos, Quaternion.identity);
        var nw = go.GetComponent<NetworkPlayer>() ?? go.AddComponent<NetworkPlayer>();
        nw.Initialize(ps);
        networkPlayers[ps.Id] = nw;
        Debug.Log($"Created network player {ps.Username} (ID {ps.Id})");
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
        var lp = new LoginPacket { Username = playerName, Password = "secret" };
        SendPacket<LoginPacket>(lp);
        SpawnLocalPlayer();
    }

    void SpawnLocalPlayer()
    {
        if (localPlayer != null) return;
        Vector3 spawn = GetRandomSpawnPoint();
        localPlayer = Instantiate(localPlayerPrefab, spawn, Quaternion.identity);
        Debug.Log($"Spawned local player {playerName}");
    }

    void SendPlayerMovement()
    {
        if (localPlayer == null) return;

        var pkt = new PlayerMovePacket
        {
            Position = new Vector3Serializable(localPlayer.transform.Find("Player").position),
            Rotation = new Vector3Serializable(localPlayer.transform.Find("Player").eulerAngles),
            ClientTick = lastServerTick
        };
        
        Debug.Log($"packet: send player movement coordinates: {pkt.Position.X}, {pkt.Position.Y}, {pkt.Position.Z}");
        SendPacket(pkt);
    }

    public void SendShoot(Vector3 direction, int targetId = -1)
    {
        if (!isConnected) return;

        var sp = new PlayerShootPacket { 
            Direction = new Vector3Serializable(direction),
            ClientTick = lastServerTick 
        };
        SendPacket(sp);

        if (targetId > 0)
        {
            var hp = new PlayerHitPacket
            {
                TargetId = targetId,
                ClientTick = lastServerTick,
                Direction = new Vector3Serializable(direction)
            };
            SendPacket(hp);
        }
    }

    void SendPacket<T>(T packet) where T : IPacket
    {
        if (!isConnected || serverPeer == null)
        {
            Debug.LogError($"Server connection lost.");
            return;
        }

        try
        {
            var data = MessagePackSerializer.Serialize<IPacket>(packet, MessagePackSerializer.DefaultOptions);
            Debug.Log($"Sending packet type: {packet.GetType().Name}, size: {data.Length}");
            serverPeer.Send(data, DeliveryMethod.ReliableOrdered);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending {typeof(T)}: {e}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
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
