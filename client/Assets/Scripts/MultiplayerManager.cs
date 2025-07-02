using UnityEngine;
using TMPro;

public class MultiplayerManager : MonoBehaviour {
    [Header("UI")]
    public GameObject connectingPanel;
    public GameObject gameUI;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playersCountText;
    
    [Header("Settings")]
    public string defaultPlayerName = "Player";
    
    private Client client;
    private int connectedPlayers = 0;
    
    void Start() {
        client = FindObjectOfType<Client>();
        
        if (client == null) {
            Debug.LogError("Client component not found!");
            return;
        }
        
        if (string.IsNullOrEmpty(client.playerName)) {
            // Создаем уникальное имя для каждого клиента
            string timestamp = System.DateTime.Now.Ticks.ToString();
            client.playerName = defaultPlayerName + "_" + timestamp.Substring(timestamp.Length - 6);
        }
        
        ShowConnectingPanel();
    }
    
    void Update() {
        UpdateUI();
    }
    
    void ShowConnectingPanel() {
        if (connectingPanel != null) {
            connectingPanel.SetActive(true);
        }
        
        if (gameUI != null) {
            gameUI.SetActive(false);
        }
        
        UpdateStatus("Connecting to server...");
    }
    
    void ShowGameUI() {
        if (connectingPanel != null) {
            connectingPanel.SetActive(false);
        }
        
        if (gameUI != null) {
            gameUI.SetActive(true);
        }
        
        UpdateStatus("Connected");
    }
    
    void UpdateStatus(string status) {
        if (statusText != null) {
            statusText.text = status;
        }
    }
    
    void UpdateUI() {
        if (client == null) return;
        
        bool isConnected = client.isConnected;
        
        if (isConnected && connectingPanel.activeSelf) {
            ShowGameUI();
        } else if (!isConnected && gameUI.activeSelf) {
            ShowConnectingPanel();
        }
        
        connectedPlayers = client.networkPlayers.Count + (client.localPlayer != null ? 1 : 0);
        
        if (playersCountText != null) {
            playersCountText.text = $"Players: {connectedPlayers}";
        }
    }
    
    public void OnReconnectButton() {
        if (client != null) {
            client.ConnectToServer();
        }
    }
    
    public void OnQuitButton() {
        Application.Quit();
    }
}