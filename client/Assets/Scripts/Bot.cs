using UnityEngine;

public class Bot : MonoBehaviour
{
    public GameObject gunPrefab; 
    public Transform gunSpawnPoint;  
    public float rotationSpeed = 5f;  
    public float shootInterval = 2f;
    public int HP = 100;
    public int Armor = 50;

    private Player player;
    private Gun equippedGun;  
    private float shootTimer;

    void Start()
    {
        player = FindObjectOfType<Player>();

        GameObject gunInstance = Instantiate(gunPrefab, gunSpawnPoint.position, gunSpawnPoint.rotation, transform);
        equippedGun = gunInstance.GetComponent<Gun>();

        shootTimer = shootInterval;
    }

    void Update()
    {
        if (player == null || equippedGun == null) return;

        Vector3 directionToPlayer = player.transform.position - equippedGun.firePoint.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            ShootAtPlayer();
            shootTimer = shootInterval;
        }
    }

    void ShootAtPlayer()
    {
        Vector3 shootDirection = (player.transform.position - equippedGun.firePoint.transform.position).normalized;
        equippedGun.Shoot(shootDirection);
    }
}