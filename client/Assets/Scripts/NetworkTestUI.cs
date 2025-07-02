using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkTestUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField playerNameInput;
    public TMP_InputField serverAddressInput;
    public TMP_InputField serverPortInput;
    public Button connectButton;
    public Button disconnectButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playersListText;
    
    [Header("References")]
    public Client client;
    
    void Start()
    {
        if (client == null)
            client = FindObjectOfType<Client>();
            
        if (client == null)
        {
            Debug.LogError("Client component not found!");
            return;
        }
        
        // Инициализируем UI значениями по умолчанию
        if (playerNameInput != null)
            playerNameInput.text = "Player_" + Random.Range(1000, 9999);
            
        if (serverAddressInput != null)
            serverAddressInput.text = client.serverAddress;
            
        if (serverPortInput != null)
            serverPortInput.text = client.serverPort.ToString();
            
        // Настраиваем кнопки
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
            
        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(OnDisconnectClicked);
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (client == null) return;
        
        bool connected = client.isConnected;
        
        if (connectButton != null)
            connectButton.interactable = !connected;
            
        if (disconnectButton != null)
            disconnectButton.interactable = connected;
            
        if (statusText != null)
        {
            string status = connected ? "Connected" : "Disconnected";
            statusText.text = $"Status: {status}";
        }
        
        if (playersListText != null)
        {
            int networkPlayerCount = client.networkPlayers.Count;
            int localPlayerCount = client.localPlayer != null ? 1 : 0;
            int totalPlayers = networkPlayerCount + localPlayerCount;
            
            string playersList = $"Total Players: {totalPlayers}\n";
            playersList += $"Local Player: {(client.localPlayer != null ? client.playerName : "None")}\n";
            playersList += "Network Players:\n";
            
            foreach (var kvp in client.networkPlayers)
            {
                var np = kvp.Value;
                if (np != null)
                {
                    playersList += $"- {np.Username} (ID: {np.PlayerId})\n";
                }
            }
            
            playersListText.text = playersList;
        }
    }
    
    void OnConnectClicked()
    {
        if (client == null) return;
        
        // Применяем настройки из UI
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            client.playerName = playerNameInput.text;
            
        if (serverAddressInput != null && !string.IsNullOrEmpty(serverAddressInput.text))
            client.serverAddress = serverAddressInput.text;
            
        if (serverPortInput != null && int.TryParse(serverPortInput.text, out int port))
            client.serverPort = port;
            
        Debug.Log($"Connecting as {client.playerName} to {client.serverAddress}:{client.serverPort}");
        client.ConnectToServer();
    }
    
    void OnDisconnectClicked()
    {
        if (client == null) return;
        
        // Вручную отключаемся
        if (client.isConnected)
        {
            Debug.Log("Manual disconnect requested");
            client.Disconnect();
        }
    }
} 