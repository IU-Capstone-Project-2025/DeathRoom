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
    public string serverAddress = "77.233.222.200";
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
    
    // Client tick synchronization
    private long clientTick = 0;
    private float tickRate = 60f; // Match server rate
    private float nextTickTime = 0f;

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

        // Client tick synchronization
        if (isConnected && Time.time >= nextTickTime)
        {
            clientTick++;
            nextTickTime = Time.time + (1f / tickRate);
        }
    }

    void OnDestroy()
    {
        netManager?.Stop();
    }

    void OnApplicationPause(bool pauseStatus)
    {
		return;
    }

    public void SendAnimationUpdate(Dictionary<string, object> parameters)
    {
        if (!isConnected || localPlayerId == -1) return;

        var packet = new PlayerAnimationPacket
        {
            PlayerId = this.localPlayerId,
            ClientTick = lastServerTick
        };

        foreach (var param in parameters)
        {
            switch (param.Value)
            {
                case bool bValue:
                    packet.BoolParams[param.Key] = bValue;
                    break;
                case float fValue:
                    packet.FloatParams[param.Key] = fValue;
                    break;
                case int iValue:
                    packet.IntParams[param.Key] = iValue;
                    break;
            }
        }

        if (packet.BoolParams.Count > 0 || packet.FloatParams.Count > 0 || packet.IntParams.Count > 0)
        {
            SendPacket(packet, DeliveryMethod.Unreliable);
        }
    }

    void ProcessPacket(byte[] data)
    {
        try
        {
            Debug.Log($"Attempting to deserialize packet of {data.Length} bytes");
            var packet = MessagePackSerializer.Deserialize<IPacket>(data, MessagePackSerializer.DefaultOptions);
            
            if (packet == null)
            {
                Debug.LogError($"Failed to deserialize packet - got null. Data length: {data.Length}");
                return;
            }
            
            Debug.Log($"Successfully deserialized packet of type: {packet.GetType().Name}");
            
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
                    lastServerTick = worldState.ServerTick; 
                    break;

                case PlayerShootPacket shootPacket:
                    Debug.Log($"Player {shootPacket} shot in direction {shootPacket.Direction}");
                    break;

                case PlayerShootBroadcastPacket broadcastPacket:
                    // Handle shoot broadcast from server
                    Debug.Log($"Received PlayerShootBroadcastPacket: ShooterId={broadcastPacket.ShooterId}, Direction=({broadcastPacket.Direction.X}, {broadcastPacket.Direction.Y}, {broadcastPacket.Direction.Z}), ClientTick={broadcastPacket.ClientTick}, ServerTick={broadcastPacket.ServerTick}");
                    OnReceiveShootBroadcast(broadcastPacket);
                    break;
                case PlayerAnimationPacket animPacket:
                    if (animPacket.PlayerId == localPlayerId) break;
                    if (networkPlayers.TryGetValue(animPacket.PlayerId, out var player))
                    {
                        player.ApplyAnimationUpdate(animPacket);
                    }
                    break;

                case null:
                    Debug.LogError("Unknown packet type - this should not happen after null check above");
                    break;
                default:
                    Debug.LogError($"Unhandled packet type: {packet.GetType().Name}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing packet: {e}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            Debug.LogError($"Packet data length: {data.Length}");
           
        }
    }

    void UpdateNetworkPlayer(PlayerState ps)
    {
        if (ps.Id == localPlayerId && localPlayerId != -1) 
        {
            Debug.Log($"Updating local player {ps.Username} (ID: {ps.Id}) - Health: {ps.HealthPoint}/{ps.MaxHealthPoint}, Armor: {ps.ArmorPoint}/{ps.MaxArmorPoint}");
            
            // Update local player's health and armor from server
            if (localPlayer != null)
            {
                var healthComponent = localPlayer.GetComponentInChildren<Playerhealth>();
                if (healthComponent != null)
                {
                    healthComponent.SetHealthAndArmorFromServer(
                        ps.HealthPoint, 
                        ps.MaxHealthPoint, 
                        ps.ArmorPoint, 
                        ps.MaxArmorPoint
                    );
                }
            }
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
        var nw = go.GetComponentInChildren<NetworkPlayer>() ?? go.AddComponent<NetworkPlayer>();
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
        localPlayer.transform.Find("Player").GetComponent<PlayerMovement>().client = this;
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

    public void PerformShoot(Vector3 origin, Vector3 direction)
    {
        if (!isConnected) return;

        long shootTick = clientTick;
        
        // 1. Send shoot packet first
        var shootPacket = new PlayerShootPacket { 
            Direction = new Vector3Serializable(direction),
            ClientTick = shootTick 
        };
        SendPacket(shootPacket);
        
        // 2. Perform local hit detection
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity))
        {
            // Check if we hit a player
            var hitPlayer = hit.collider.GetComponent<NetworkPlayer>();
            if (hitPlayer != null && hitPlayer.PlayerId != localPlayerId)
            {
                // 3. Send hit packet with SAME tick and direction
                var hitPacket = new PlayerHitPacket
                {
                    TargetId = hitPlayer.PlayerId,
                    ClientTick = shootTick,  // Same tick!
                    Direction = new Vector3Serializable(direction)  // Same direction!
                };
                SendPacket(hitPacket);
                
                Debug.Log($"Hit detected on player {hitPlayer.PlayerId} at tick {shootTick}");
            }
        }
        
        // 4. Show local effects immediately (muzzle flash, sound, etc.)
        ShowLocalShootEffects(origin, direction);
    }

    void ShowLocalShootEffects(Vector3 origin, Vector3 direction)
    {
        // Implement local visual/audio effects here
        // This gives immediate feedback while waiting for server validation
        Debug.Log($"Showing local shoot effects from {origin} in direction {direction}");
    }

    void SendPacket<T>(T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IPacket
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
            serverPeer.Send(data, deliveryMethod);
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

    void OnReceiveShootBroadcast(PlayerShootBroadcastPacket broadcastPacket)
    {
        // Don't show effects for own shots (already shown locally)
        if (broadcastPacket.ShooterId == localPlayerId) 
        {
            Debug.Log($"Ignoring own shoot broadcast from server");
            return;
        }
        
        // Show effects for other players' shots
        if (networkPlayers.TryGetValue(broadcastPacket.ShooterId, out NetworkPlayer shooter))
        {
            Vector3 shootDirection = new Vector3(
                broadcastPacket.Direction.X, 
                broadcastPacket.Direction.Y, 
                broadcastPacket.Direction.Z
            );
            
            ShowShootEffectsForPlayer(shooter, shootDirection, broadcastPacket.ClientTick, broadcastPacket.ServerTick);
            Debug.Log($"Showing shoot effects for player {broadcastPacket.ShooterId}");
        }
        else
        {
            Debug.LogWarning($"Received shoot broadcast for unknown player {broadcastPacket.ShooterId}");
        }
    }

    void ShowShootEffectsForPlayer(NetworkPlayer shooter, Vector3 direction, long clientTick, long serverTick)
    {
        // Implement visual and audio effects for other players' shots
        // This could include:
        // - Muzzle flash at shooter position
        // - Shoot sound effect
        // - Bullet tracer/projectile
        // - Screen shake if close
        
        Vector3 shooterPosition = shooter.transform.position;
        Debug.Log($"Player {shooter.PlayerId} shot from {shooterPosition} in direction {direction}");
        
        // Example: Create bullet tracer (you would implement this based on your game's visual system)
        CreateBulletTracer(shooterPosition, direction);
        PlayShootSound(shooterPosition);
    }

    void CreateBulletTracer(Vector3 origin, Vector3 direction)
    {
        // Placeholder for bullet tracer implementation
        Debug.Log($"Creating bullet tracer from {origin} in direction {direction}");
    }

    void PlayShootSound(Vector3 position)
    {
        // Placeholder for 3D positioned audio
        Debug.Log($"Playing shoot sound at position {position}");
    }

    public long GetCurrentClientTick()
    {
        return clientTick;
    }
}
