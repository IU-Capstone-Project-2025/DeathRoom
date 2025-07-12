using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UILeaderBoard : MonoBehaviour
{
    [System.Serializable]
    public class PlayerScore
    {
        public string playerName;
        public int kills;
        public int assists;
        public int deaths;
        public int score;

        public PlayerScore(string name, int kills, int assists, int deaths)
        {
            this.playerName = name;
            this.kills = kills;
            this.assists = assists;
            this.deaths = deaths;
            this.score = CalculateScore();
        }

        public int CalculateScore()
        {
            return kills * 100 + assists * 50 - deaths * 25;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject playerScorePrefab;

    [Header("Test Data")]
    [SerializeField] private bool generateTestData = false;
    [SerializeField] private int testPlayersCount = 5;

    private List<PlayerScore> playerScores = new List<PlayerScore>();
    [SerializeField] private float updateInterval = 5f;
    private bool isPaused = false;

    private void Awake()
    {
        if (generateTestData)
        {
            GenerateTestData();
        }
    }

    private void Start()
    {
        leaderboardPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleLeaderboard();
        }
    }

    public void ToggleLeaderboard()
    {
        isPaused = !isPaused;
        leaderboardPanel.SetActive(isPaused);
        
        if (isPaused) 
        {
            UpdateLeaderboard();
        }
    }

    public void UpdateLeaderboard()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var sortedPlayers = playerScores.OrderByDescending(p => p.score).ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var playerEntry = Instantiate(playerScorePrefab, contentParent);
            var entryTexts = playerEntry.GetComponentsInChildren<TMP_Text>();

            if (entryTexts.Length >= 5)
            {
                entryTexts[0].text = (i + 1).ToString();
                entryTexts[1].text = sortedPlayers[i].playerName;
                entryTexts[2].text = sortedPlayers[i].kills.ToString();
                entryTexts[3].text = sortedPlayers[i].assists.ToString();
                entryTexts[4].text = sortedPlayers[i].deaths.ToString();
                entryTexts[5].text = sortedPlayers[i].score.ToString();
            }

            if (sortedPlayers[i].playerName == "You")
            {
                playerEntry.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.8f, 0.5f);
            }
        }
    }

    public void AddOrUpdatePlayer(string name, int kills, int assists, int deaths)
    {
        var existingPlayer = playerScores.Find(p => p.playerName == name);
        if (existingPlayer != null)
        {
            existingPlayer.kills = kills;
            existingPlayer.assists = assists;
            existingPlayer.deaths = deaths;
            existingPlayer.score = existingPlayer.CalculateScore();
        }
        else
        {
            playerScores.Add(new PlayerScore(name, kills, assists, deaths));
        }
    }

    private void GenerateTestData()
    {
        playerScores.Clear();
        
        playerScores.Add(new PlayerScore("You", 10, 5, 3));

        string[] randomNames = { "Player1", "Player2", "Player3", "PLayer4", "Player5" };
        for (int i = 0; i < testPlayersCount - 1; i++)
        {
            playerScores.Add(new PlayerScore(
                randomNames[Random.Range(0, randomNames.Length)],
                Random.Range(0, 15),
                Random.Range(0, 10),
                Random.Range(0, 8)
            ));
        }
    }
}
