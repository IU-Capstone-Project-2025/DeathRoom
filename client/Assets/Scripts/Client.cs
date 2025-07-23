using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using DeathRoom.Common.dto;
using MessagePack;
using DeathRoom.Common.Network;
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

    [Header("Shooting")]
    public TrailRenderer shootTrail;

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
    private int respawnCount = 0;
    private const int MAX_RESPAWNS = 1;
    
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
        Disconnect();
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
                        Debug.Log($"Player in packet: {ps.Username} (ID: {ps.Id}) at position {ps.Position.X}, {ps.Position.Y}, {ps.Position.Z} - Health: {ps.HealthPoint}/{ps.MaxHealthPoint}, Armor: {ps.ArmorPoint}/{ps.MaxArmorPoint}");
                        Debug.LogWarning($"[ARMOR DEBUG] Player {ps.Username} armor values: ArmorPoint={ps.ArmorPoint}, MaxArmorPoint={ps.MaxArmorPoint}");
                        //point
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
                    
                case PlayerDeathPacket deathPacket:
                    Debug.Log($"Player {deathPacket.PlayerId} died. Killer ID: {deathPacket.KillerId}");
                    if (deathPacket.PlayerId == localPlayerId)
                    {
                        Debug.Log("Local player died. Respawning...");
                        RespawnPlayer();
                    }
                    break;
                    
                case PlayerHealthUpdatePacket healthUpdate:
                    if (localPlayer != null)
                    {
                        var healthComponent = localPlayer.GetComponentInChildren<Playerhealth>();
                        if (healthComponent != null)
                        {
                            // Update health and armor values
                            healthComponent.SetHealthAndArmorFromServer(
                                healthUpdate.Health,
                                (int)healthComponent.maxHealth,
                                healthUpdate.Armor,
                                (int)healthComponent.maxArmor
                            );
                            
                            Debug.Log($"[HEALTH UPDATE] Received health update: {healthUpdate.Health} HP, {healthUpdate.Armor} Armor");
                        }
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
                    Debug.Log($"[HEALTH UPDATE] Received health update from server: {ps.HealthPoint}/{ps.MaxHealthPoint}");
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
        Vector3 playerPos = ps.Position.ToUnityVector3();
        bool hasValidPosition = playerPos.magnitude > 0.1f;
        
        Vector3 spawnPos = hasValidPosition ? playerPos : GetRandomSpawnPoint();
        GameObject go = Instantiate(networkPlayerPrefab, spawnPos, Quaternion.identity);
        var nw = go.GetComponentInChildren<NetworkPlayer>() ?? go.AddComponent<NetworkPlayer>();
        nw.Initialize(ps);
        networkPlayers[ps.Id] = nw;
        Debug.Log($"Created network player {ps.Username} (ID {ps.Id}) at position {spawnPos}");
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
        Debug.Log($"[SHOOT] Sent PlayerShootPacket at tick {shootTick}");
        
        // Debug raycast parameters
        Debug.Log($"[RAYCAST DEBUG] Origin: {origin}, Direction: {direction}, Distance: Infinity");
        Debug.DrawRay(origin, direction * 100f, Color.red, 5f); // Visualize ray for 5 seconds
        
        // 2. Perform local hit detection with detailed debugging
        RaycastHit hit;
        bool raycastHit = Physics.Raycast(origin, direction, out hit, Mathf.Infinity);
        Debug.Log($"[RAYCAST DEBUG] Raycast result: {raycastHit}");
        
        if (raycastHit)
        {
            Debug.Log($"[RAYCAST] Hit object: '{hit.collider.name}' at distance {hit.distance:F2}, tag: '{hit.collider.tag}', layer: {hit.collider.gameObject.layer}");
            Debug.Log($"[RAYCAST] Collider isTrigger: {hit.collider.isTrigger}, enabled: {hit.collider.enabled}");
            Debug.Log($"[RAYCAST] Hit point: {hit.point}, normal: {hit.normal}");
            
            // Check if we hit a player - try both current object and parents
            var hitPlayer = hit.collider.GetComponent<NetworkPlayer>();
            if (hitPlayer == null)
            {
                hitPlayer = hit.collider.GetComponentInParent<NetworkPlayer>();
                if (hitPlayer != null) Debug.Log($"[RAYCAST] Found NetworkPlayer in parent object");
            }
            if (hitPlayer == null)
            {
                hitPlayer = hit.collider.GetComponentInChildren<NetworkPlayer>();
                if (hitPlayer != null) Debug.Log($"[RAYCAST] Found NetworkPlayer in child object");
            }
            
            if (hitPlayer != null)
            {
                Debug.Log($"[RAYCAST] Found NetworkPlayer: ID={hitPlayer.PlayerId}, Name='{hitPlayer.Username}', IsLocalPlayer={hitPlayer.PlayerId == localPlayerId}");
                
                if (hitPlayer.PlayerId != localPlayerId)
                {
                    var hitPacket = new PlayerHitPacket
                    {
                        TargetId = hitPlayer.PlayerId,
                        ClientTick = shootTick,
                        Direction = new Vector3Serializable(direction)
                    };
                    SendPacket(hitPacket);
                    
                    Debug.Log($"[HIT SENT] Hit detected on player {hitPlayer.PlayerId} (name: {hitPlayer.Username}) at tick {shootTick} - waiting for server health update");
                }
                else
                {
                    Debug.Log($"[SHOOT] Hit own player, ignoring");
                }
            }
            else
            {
                Debug.Log($"[SHOOT] Hit object '{hit.collider.name}' but no NetworkPlayer component found");
            }
        }
        else
        {
            Debug.Log("[SHOOT] No hit detected - raycast missed");
        }
        ShowLocalShootEffects(origin, direction);
    }

    public long GetCurrentClientTick()
    {
        return clientTick;
    }

    void ShowLocalShootEffects(Vector3 origin, Vector3 direction)
    {
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

    public Vector3 GetRandomSpawnPoint()
    {
        return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].position;
    }

    public void RespawnPlayer()
    {
        if (respawnCount >= MAX_RESPAWNS)
        {
            Debug.Log("Превышено максимальное количество респавнов");
            return;
        }
        
        Vector3 spawnPoint = GetRandomSpawnPoint();
        Transform playerTransform = localPlayer.transform.Find("Player");
        
        playerTransform.position = spawnPoint;
        playerTransform.rotation = Quaternion.identity;
            
        var movePacket = new PlayerMovePacket
        {
            Position = new Vector3Serializable(spawnPoint),
            Rotation = new Vector3Serializable(playerTransform.eulerAngles),
            ClientTick = GetCurrentClientTick()
        };
        SendPacket(movePacket);
        
        var healthUpdatePacket = new PlayerHealthUpdatePacket
        {
            Health = 100,
            Armor = 100,
            ClientTick = GetCurrentClientTick()
        };
        SendPacket(healthUpdatePacket);
        Debug.Log($"[HEALTH UPDATE] Sent respawn health update: {100} HP, {100} Armor");
            
        respawnCount++;
        Debug.Log($"Игрок респавнется. Количество оставшихся респавнов: {MAX_RESPAWNS - respawnCount}");
    }

    void OnReceiveShootBroadcast(PlayerShootBroadcastPacket broadcastPacket)
    {
        if (broadcastPacket.ShooterId == localPlayerId) 
        {
            Debug.Log($"Ignoring own shoot broadcast from server");
            return;
        }
        
        if (networkPlayers.TryGetValue(broadcastPacket.ShooterId, out NetworkPlayer shooter))
        {
            Vector3 shootDirection = new Vector3(
                broadcastPacket.Direction.X, 
                broadcastPacket.Direction.Y, 
                broadcastPacket.Direction.Z
            );
            
            bool isShotgun = shooter.GetCurrentWeaponType() == WeaponType.Shotgun;
            
            ShowShootEffectsForPlayer(
                shooter, 
                shootDirection, 
                broadcastPacket.ClientTick, 
                broadcastPacket.ServerTick,
                isShotgun
            );
            
            Debug.Log($"Showing shoot effects for player {broadcastPacket.ShooterId} (Weapon: {(isShotgun ? "Shotgun" : "Rifle")})");
        }
        else
        {
            Debug.LogWarning($"Received shoot broadcast for unknown player {broadcastPacket.ShooterId}");
        }
    }

    // Helper method to find a child transform by name recursively
    private Transform FindChildRecursive(Transform parent, string name)
    {
        // Check if the current transform has the name we're looking for
        if (parent.name == name)
            return parent;

        // Search through all children
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            // Check if this child has the name
            if (child.name == name)
                return child;
                
            // Recursively search the child's children
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }

        // If we get here, we didn't find it
        return null;
    }

    void ShowShootEffectsForPlayer(NetworkPlayer shooter, Vector3 direction, long clientTick, long serverTick, bool isShotgun = false)
    {
        if (shooter == null) return;
        
        // Find the appropriate gun barrel transform based on weapon type
        Transform shootPoint = null;
        string weaponType = isShotgun ? "Shotgun" : "Rifle";
        string shootOutTag = isShotgun ? "ShootOutShotgun" : "ShootOutRifle";
        
        // Try to find the specific weapon's shoot point first
        var weaponRoot = FindChildRecursive(shooter.transform, shootOutTag);
        if (weaponRoot != null)
        {
            // Find the actual shoot point (usually a child named "ShootOut")
            shootPoint = weaponRoot.Find("ShootOut") ?? weaponRoot;
        }
        
        // Fallback to any shoot point if specific one not found
        if (shootPoint == null)
        {
            shootPoint = FindChildRecursive(shooter.transform, "ShootOut");
        }
        
        // Default position if no shoot point found
        Vector3 shootPosition = shootPoint != null ? shootPoint.position : 
            shooter.transform.position + shooter.transform.forward * 0.5f + Vector3.up * 1.7f;
        
        // Create muzzle flash effect with weapon-specific settings
        CreateMuzzleFlash(shootPosition, direction, isShotgun);
        
        // Create bullet tracers with weapon-specific patterns
        if (isShotgun)
        {
            // Shotgun spread pattern (5 pellets in a spread)
            for (int i = 0; i < 5; i++)
            {
                // Calculate spread direction (slight random spread for shotgun)
                Vector3 spreadDirection = direction + UnityEngine.Random.insideUnitSphere * 0.1f;
                spreadDirection.Normalize();
                CreateBulletTracer(shootPosition, spreadDirection);
            }
        }
        else
        {
            // Single bullet for rifle
            CreateBulletTracer(shootPosition, direction);
        }
        
        // Optional: Add screen shake if the shot is close to the local player
        if (localPlayer != null)
        {
            float distance = Vector3.Distance(shootPosition, localPlayer.transform.position);
            float shakeDistance = isShotgun ? 15f : 10f; // Shotgun has more range for screen shake
            if (distance < shakeDistance)
            {
                float intensity = isShotgun ? 0.15f : 0.1f; // More intense for shotgun
                // CameraShake.Instance?.Shake(intensity, intensity * (1f - distance/shakeDistance));
            }
        }
    }
    
    void CreateMuzzleFlash(Vector3 position, Vector3 direction, bool isShotgun)
    {
        // Create muzzle flash effect
        GameObject muzzleFlash = new GameObject("MuzzleFlash");
        muzzleFlash.transform.position = position;
        muzzleFlash.transform.rotation = Quaternion.LookRotation(direction);
        
        // Add light component for muzzle flash with weapon-specific settings
        Light flashLight = muzzleFlash.AddComponent<Light>();
        
        if (isShotgun)
        {
            // Shotgun muzzle flash (wider, more intense)
            flashLight.color = new Color(1f, 0.8f, 0.4f); // Brighter, more white
            flashLight.range = 8f;
            flashLight.intensity = 5f;
            flashLight.shadowStrength = 0.8f;
            
            // Play shotgun sound (uncomment when audio is set up)
            // AudioSource.PlayClipAtPoint(shotgunSound, position, 0.7f);
        }
        else
        {
            // Rifle muzzle flash
            flashLight.color = new Color(1f, 0.7f, 0.3f); // Orange-yellow
            flashLight.range = 5f;
            flashLight.intensity = 3f;
            
            // Play rifle sound (uncomment when audio is set up)
            // AudioSource.PlayClipAtPoint(rifleSound, position, 0.5f);
        }
        
        // Destroy the muzzle flash after a short delay
        Destroy(muzzleFlash, 0.05f);
    }
    

    void CreateBulletTracer(Vector3 origin, Vector3 direction)
    {
        if (shootTrail == null)
        {
            Debug.LogWarning("Shoot trail prefab is not assigned!");
            return;
        }

        // Create the trail renderer
        TrailRenderer trail = Instantiate(shootTrail, origin, Quaternion.identity);
        
        // Set up raycast to find where the bullet would hit
        RaycastHit hit;
        float maxDistance = 1000f;
        bool isHit = Physics.Raycast(origin, direction, out hit, maxDistance);
        
        // If we didn't hit anything, use a point in the distance
        Vector3 endPoint = isHit ? hit.point : origin + direction * maxDistance;
        
        // Position the trail between origin and endpoint
        trail.transform.position = origin;
        trail.Clear(); // Clear any existing points
        
        // Animate the trail using a coroutine
        StartCoroutine(AnimateBulletTracer(trail, origin, endPoint));
    }
    
    IEnumerator AnimateBulletTracer(TrailRenderer trail, Vector3 start, Vector3 end)
    {
        float duration = 0.1f; // Duration of the bullet travel
        float startTime = Time.time;
        
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            // Move the trail renderer along the path
            trail.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        
        // Ensure the final position is set
        trail.transform.position = end;
        
        // Wait for the trail to fade out before destroying it
        if (trail != null)
        {
            yield return new WaitForSeconds(trail.time);
            if (trail != null)
            {
                Destroy(trail.gameObject);
            }
        }
    }
}

