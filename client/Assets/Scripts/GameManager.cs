using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject playerSpawnPoint;
    public GameObject botSpawnPoint;
    public GameObject botPrefab;

    public void Start() 
    {
        Instantiate(playerPrefab, playerSpawnPoint.transform.position, playerSpawnPoint.transform.rotation);
        Instantiate(botPrefab, botSpawnPoint.transform.position, botSpawnPoint.transform.rotation);
    }
}
