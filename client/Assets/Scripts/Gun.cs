using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject firePoint;
    public float bulletForce = 300f;

    public void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.transform.position, firePoint.transform.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(firePoint.transform.forward * bulletForce, ForceMode.Impulse);
        Destroy(bullet, 5f);
    }
}