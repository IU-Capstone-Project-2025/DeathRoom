using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealArmorDevice : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private GameObject[] devicePrefabs;
    [SerializeField] private float respawnTime = 15f;
    [SerializeField] private float healAmount = 45f;
    [SerializeField] private float armorAmount = 100f;
    [SerializeField] private GameObject spawnPosition;
    private GameObject currentDevice = null;

    void Start()
    {
        SpawnRandomDevice();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Playerhealth healthManager = other.GetComponent<Playerhealth>();

            if (currentDevice == devicePrefabs[0])
            {
                healthManager.Heal(healAmount);
                Destroy(currentDevice);
                StartCoroutine(RespawnDevice());
            }
            else if (currentDevice == devicePrefabs[1])
            {
                healthManager.RepairArmor(armorAmount);
                Destroy(currentDevice);
                StartCoroutine(RespawnDevice());
            }
        }
    }
    private void SpawnRandomDevice()
    {
        if (devicePrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, devicePrefabs.Length);
        currentDevice = Instantiate(devicePrefabs[randomIndex], spawnPosition.transform.position, Quaternion.identity, transform);

        currentDevice.AddComponent<WeaponDevice>();
    }
    
    private IEnumerator RespawnDevice()
    {
        currentDevice = null;
        yield return new WaitForSeconds(respawnTime);

        SpawnRandomDevice();
    }
}
