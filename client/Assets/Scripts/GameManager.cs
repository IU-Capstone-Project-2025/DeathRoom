using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public GameObject spawnPoint;

    public void Start() 
    {
        Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }
}
