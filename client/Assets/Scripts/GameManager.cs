using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public List<GameObject> playerPrefabs; 
    public List<GameObject> spawnPoints;   
    public float spawnDelay = 3f;
    public TextMeshProUGUI countDownText;
    public Camera camToTurnOff;

    private void Start()
    {
        // Проверяем, есть ли Client компонент (сетевая игра)
        var client = FindObjectOfType<Client>();
        if (client != null)
        {
            // В сетевой игре GameManager не должен спавнить игроков
            Debug.Log("Network mode detected - GameManager will not spawn players");
            return;
        }
        
        if (camToTurnOff != null)
        {
            camToTurnOff.enabled = false;
        }

        
        // StartCoroutine(SpawnPlayersAfterDelay());
    }

    // private IEnumerator SpawnPlayersAfterDelay()
    // {
    //     float remainingTime = spawnDelay;
    //     while (remainingTime > 0)
    //     {
    //         countDownText.text = Mathf.CeilToInt(remainingTime).ToString();
    //         yield return new WaitForSeconds(1f);
    //         remainingTime -= 1f;
    //     }
    //
    //     countDownText.text = "GO!";
    //     yield return new WaitForSeconds(0.5f);
    //     countDownText.text = "";
    //
    //     if (camToTurnOff != null)
    //     {
    //         camToTurnOff.enabled = false;
    //     }
    //
    //     int spawnCount = Mathf.Min(playerPrefabs.Count, spawnPoints.Count);
    //     if (spawnCount == 0)
    //     {
    //         Debug.LogWarning(".");
    //         yield break;
    //     }
    //
    //     List<GameObject> shuffledSpawnPoints = new List<GameObject>(spawnPoints);
    //     Shuffle(shuffledSpawnPoints);
    //
    //     for (int i = 0; i < spawnCount; i++)
    //     {
    //         GameObject prefab = playerPrefabs[i];
    //         GameObject spawnPoint = shuffledSpawnPoints[i];
    //         // Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    //     }
    // }

    private void Shuffle(List<GameObject> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            GameObject temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
