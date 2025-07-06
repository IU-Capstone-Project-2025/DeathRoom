using UnityEngine;
using DeathRoom.Common.network;
using DeathRoom.Common.dto;

public class NetworkDebugger : MonoBehaviour
{
    private Client client;
    
    void Start()
    {
        client = FindObjectOfType<Client>();
        
        // Тестируем сериализацию пакетов
        TestPacketSerialization();
    }
    
    void TestPacketSerialization()
    {
        Debug.Log("=== Testing Packet Serialization ===");
        
        try
        {
            // Тест LoginPacket
            var loginPacket = new LoginPacket
            {
                Username = "TestPlayer",
                Password = "secret"
            };
            
            var loginData = MessagePack.MessagePackSerializer.Serialize<IPacket>(loginPacket);
            var deserializedLogin = MessagePack.MessagePackSerializer.Deserialize<IPacket>(loginData);
            Debug.Log($"✅ LoginPacket test passed: {((LoginPacket)deserializedLogin).Username}");
            
            // Тест PlayerMovePacket
            var movePacket = new PlayerMovePacket
            {
                Position = new Vector3Serializable(1, 2, 3),
                Rotation = new Vector3Serializable(0, 90, 0),
                ClientTick = 123
            };
            
            var moveData = MessagePack.MessagePackSerializer.Serialize<IPacket>(movePacket);
            var deserializedMove = MessagePack.MessagePackSerializer.Deserialize<IPacket>(moveData);
            var move = (PlayerMovePacket)deserializedMove;
            Debug.Log($"✅ PlayerMovePacket test passed: pos({move.Position.X}, {move.Position.Y}, {move.Position.Z})");
            
            // Тест PlayerShootPacket
            var shootPacket = new PlayerShootPacket
            {
                Direction = new Vector3Serializable(0, 0, 1),
                ClientTick = 456
            };
            
            var shootData = MessagePack.MessagePackSerializer.Serialize<IPacket>(shootPacket);
            var deserializedShoot = MessagePack.MessagePackSerializer.Deserialize<IPacket>(shootData);
            var shoot = (PlayerShootPacket)deserializedShoot;
            Debug.Log($"✅ PlayerShootPacket test passed: dir({shoot.Direction.X}, {shoot.Direction.Y}, {shoot.Direction.Z})");
            
            Debug.Log("=== All serialization tests passed! ===");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Serialization test failed: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestConnection();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            TestPacketSerialization();
        }
    }
    
    void TestConnection()
    {
        if (client == null)
        {
            Debug.LogError("Client component not found!");
            return;
        }
        
        Debug.Log("=== Connection Test ===");
        // Debug.Log($"Server Address: {client.serverAddress}");
        Debug.Log($"Server Port: {client.serverPort}");
        Debug.Log($"Player Name: {client.playerName}");
        Debug.Log($"Is Connected: {client.isConnected}");
        Debug.Log($"Local Player: {(client.localPlayer != null ? "Created" : "Not created")}");
        Debug.Log($"Network Players: {client.networkPlayers.Count}");
        
        if (!client.isConnected)
        {
            Debug.Log("Attempting to connect...");
            client.ConnectToServer();
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Network Debugger");
        GUILayout.Label($"Connected: {(client?.isConnected ?? false)}");
        GUILayout.Label($"Players: {(client?.networkPlayers.Count ?? 0)}");
        
        if (GUILayout.Button("Test Connection (T)"))
        {
            TestConnection();
        }
        
        if (GUILayout.Button("Test Packets (P)"))
        {
            TestPacketSerialization();
        }
        
        GUILayout.EndArea();
    }
} 