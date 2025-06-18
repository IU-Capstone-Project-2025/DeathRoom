using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject firePoint;
    public float bulletForce = 300f;
    public int BulletDamage = 10;

    public void Shoot(Vector3 shootDirection)
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.transform.position, Quaternion.LookRotation(shootDirection));
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(shootDirection.normalized * bulletForce, ForceMode.Impulse);
        Destroy(bullet, 5f);
    }

}