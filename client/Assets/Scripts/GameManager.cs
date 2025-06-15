using UnityEngine;

public class GameManager : MonoBehaviour
{

    public GameObject _playerPrefab;
    public GameObject _spawnPoint;

    void Start()
    {
        Instantiate(_playerPrefab, _spawnPoint.transform.position, _spawnPoint.transform.rotation);
    }
}
