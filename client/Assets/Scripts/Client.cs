using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using System.Net;
using DeathRoom.Common.network;
using DeathRoom.Network;
using MessagePack;
using UnityEditor.PackageManager;

public class Client : MonoBehaviour {
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

    void Start() {
        InitializeNetwork();
        ConnectToServer();
    }

    void InitializeNetwork() {
        netListener = new EventBasedNetListener();
        
        netListener.PeerConnectedEvent += OnConnected;
        netListener.PeerDisconnectedEvent += OnDisconnected;
        netListener.NetworkReceiveEvent += OnNetworkReceive;
        netListener.NetworkErrorEvent += OnNetworkError;

        netManager = new NetManager(netListener);
        netManager.Start();
    }

    public void ConnectToServer() {
        Debug.Log($"Connecting to server {serverAddress}:{serverPort}...");
        serverPeer = netManager.Connect(serverAddress, serverPort, "DeathRoomSecret");
    }

    void OnConnected(NetPeer peer) {
        Debug.Log($"Connected to server: {peer}");
        isConnected = true;
        
        // Send login packet
        SendLoginPacket();
    }

    void OnDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
        Debug.Log($"Disconnected from server. Reason: {disconnectInfo.Reason}");
        isConnected = false;
        
        // Clean up local player and network players
        if (localPlayer != null) {
            Destroy(localPlayer);
            localPlayer = null;
        }
        
        foreach (var player in networkPlayers.Values) {
            if (player != null && player.gameObject != null) {
                Destroy(player.gameObject);
            }
        }
        networkPlayers.Clear();
    }

    void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        var data = reader.GetRemainingBytes();
        ProcessPacket(data);
    }

    void OnNetworkError(IPEndPoint endPoint, Error socketErrorCode) {
        Debug.LogError($"Network error: {socketErrorCode}");
    }

    void ProcessPacket(byte[] data) {
        if (data.Length == 0) return;
        
        var packetType = (PacketType)data[0];
        var packetData = new byte[data.Length - 1];
        System.Buffer.BlockCopy(data, 1, packetData, 0, packetData.Length);
        
        switch (packetType) {
            case PacketType.WorldState:
                ProcessWorldStatePacket(packetData);
                break;
            default:
                Debug.Log($"Received unknown packet type: {packetType}");
                break;
        }
    }

    void ProcessWorldStatePacket(byte[] data) {
        try {
            var worldState = MessagePackSerializer.Deserialize<WorldStatePacket>(data);
            lastServerTick = worldState.ServerTick;
            
            foreach (var playerState in worldState.Players.Values) {
                UpdateNetworkPlayer(playerState);
            }
            
            // Remove disconnected players
            var playersToRemove = new List<int>();
            foreach (var kvp in networkPlayers) {
                bool found = false;
                foreach (var playerState in worldState.Players.Values) {
                    if (playerState.PlayerId == kvp.Key) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    playersToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var playerId in playersToRemove) {
                RemoveNetworkPlayer(playerId);
            }
            
        } catch (System.Exception e) {
            Debug.LogError($"Error processing WorldState packet: {e.Message}");
        }
    }

    void UpdateNetworkPlayer(PlayerState playerState) {
        // Skip if this is our local player (we control it locally)
        if (localPlayer != null && localPlayerMovement != null && 
            playerState.Username == playerName) {
            return;
        }
        
        if (!networkPlayers.ContainsKey(playerState.PlayerId)) {
            CreateNetworkPlayer(playerState);
        } else {
            var networkPlayer = networkPlayers[playerState.PlayerId];
            if (networkPlayer != null) {
                networkPlayer.UpdateState(playerState);
            }
        }
    }

    void CreateNetworkPlayer(PlayerState playerState) {
        if (networkPlayerPrefab == null) {
            Debug.LogError("Network player prefab is not set!");
            return;
        }
        
        Vector3 spawnPos = playerState.Position != Vector3.zero ? 
            UnityVector3(playerState.Position) : 
            GetRandomSpawnPoint();
            
        GameObject playerObj = Instantiate(networkPlayerPrefab, spawnPos, Quaternion.identity);
        NetworkPlayer networkPlayer = playerObj.GetComponent<NetworkPlayer>();
        
        if (networkPlayer == null) {
            networkPlayer = playerObj.AddComponent<NetworkPlayer>();
        }
        
        networkPlayer.Initialize(playerState);
        networkPlayers[playerState.PlayerId] = networkPlayer;
        
        Debug.Log($"Created network player: {playerState.Username} (ID: {playerState.PlayerId})");
    }

    void RemoveNetworkPlayer(int playerId) {
        if (networkPlayers.ContainsKey(playerId)) {
            var networkPlayer = networkPlayers[playerId];
            if (networkPlayer != null && networkPlayer.gameObject != null) {
                Debug.Log($"Removing network player: {networkPlayer.Username} (ID: {playerId})");
                Destroy(networkPlayer.gameObject);
            }
            networkPlayers.Remove(playerId);
        }
    }

    void SendLoginPacket() {
        var loginPacket = new LoginPacket {
            Username = playerName
        };
        
        SendPacket(PacketType.Login, loginPacket);
        
        // Spawn local player after login
        SpawnLocalPlayer();
    }

    void SpawnLocalPlayer() {
        if (localPlayer != null) return;
        
        if (localPlayerPrefab == null) {
            Debug.LogError("Local player prefab is not set!");
            return;
        }
        
        Vector3 spawnPos = GetRandomSpawnPoint();
        localPlayer = Instantiate(localPlayerPrefab, spawnPos, Quaternion.identity);
        localPlayerMovement = localPlayer.GetComponent<PlayerMovement>();
        
        if (localPlayerMovement == null) {
            Debug.LogError("Local player prefab must have PlayerMovement component!");
        }
        
        Debug.Log($"Spawned local player: {playerName}");
    }

    Vector3 GetRandomSpawnPoint() {
        if (spawnPoints != null && spawnPoints.Length > 0) {
            var randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return randomSpawn.position;
        }
        return Vector3.zero;
    }

    void Update() {
        if (netManager != null) {
            netManager.PollEvents();
        }
        
        // Send player movement
        if (isConnected && localPlayer != null && Time.time >= nextSendTime) {
            SendPlayerMovement();
            nextSendTime = Time.time + (1f / sendRate);
        }
    }

    void SendPlayerMovement() {
        if (localPlayerMovement == null) return;
        
        var movePacket = new PlayerMovePacket {
            Position = new Vector3 {
                x = localPlayer.transform.position.x,
                y = localPlayer.transform.position.y,
                z = localPlayer.transform.position.z
            },
            Rotation = new Vector3 {
                x = localPlayer.transform.eulerAngles.x,
                y = localPlayer.transform.eulerAngles.y,
                z = localPlayer.transform.eulerAngles.z
            }
        };
        
        SendPacket(PacketType.PlayerMove, movePacket);
    }

    public void SendShoot(Vector3 direction, int targetId = -1) {
        if (!isConnected) return;
        
        var shootPacket = new PlayerShootPacket {
            Direction = new Vector3 {
                x = direction.x,
                y = direction.y,
                z = direction.z
            }
        };
        
        SendPacket(PacketType.PlayerShoot, shootPacket);
        
        // If we hit a specific target, send hit packet
        if (targetId > 0) {
            var hitPacket = new PlayerHitPacket {
                TargetId = targetId,
                ClientTick = lastServerTick,
                Direction = new Vector3 {
                    x = direction.x,
                    y = direction.y,
                    z = direction.z
                }
            };
            
            SendPacket(PacketType.PlayerHit, hitPacket);
        }
    }

    void SendPacket<T>(PacketType type, T packet) where T : class {
        if (!isConnected || serverPeer == null) return;
        
        try {
            var data = PacketProcessor.Pack(packet as IPacket);
            serverPeer.Send(data, DeliveryMethod.ReliableOrdered);
        } catch (System.Exception e) {
            Debug.LogError($"Error sending packet {type}: {e.Message}");
        }
    }

    Vector3 UnityVector3(Vector3 v) {
        return new Vector3(v.x , v.y, v.z);
    }

    void FixedUpdate() {
        if (netManager != null) {
            netManager.PollEvents();
        }
    }

    void OnDestroy() {
        if (netManager != null) {
            netManager.Stop();
        }
    }

    void OnApplicationPause(bool pauseStatus) {
        if (pauseStatus && netManager != null) {
            netManager.Stop();
        }
    }
}