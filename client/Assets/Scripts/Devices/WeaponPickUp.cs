using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickUp : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject[] weaponPrefabs;
    [SerializeField] private float respawnTime = 10f;
    [SerializeField] private GameObject spawnPosition;
    private GameObject currentWeapon = null;

    void Start()
    {
        SpawnRandomWeapon();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerWeaponManager weaponManager = other.GetComponent<PlayerWeaponManager>();

            if (weaponManager != null)
            {
                weaponManager.AddWeapon(currentWeapon);
                Destroy(currentWeapon);
                StartCoroutine(RespawnWeapon());
            }
        }
    }
    private void SpawnRandomWeapon()
    {
        if (weaponPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, weaponPrefabs.Length);
        currentWeapon = Instantiate(weaponPrefabs[randomIndex], spawnPosition.transform.position, Quaternion.identity, transform);

        currentWeapon.AddComponent<WeaponDevice>();
    }
    
    private IEnumerator RespawnWeapon()
    {
        currentWeapon = null;
        yield return new WaitForSeconds(respawnTime);

        SpawnRandomWeapon();
    }

}
