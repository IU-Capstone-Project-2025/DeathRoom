using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shootgun : Gun
{
    [Header("Shotgun Settings")]
    [SerializeField] private int pelletCount = 8;
    [SerializeField] private float spreadAngle = 6f;
    [SerializeField] private float range = 50f;

    [Header("Effects")]
    public ParticleSystem shootParticle;
    public ParticleSystem shellParticle;
    public TrailRenderer shootTrail;
    public GameObject[] shootHoles;

    private bool canShoot = true;

    public override void Shoot()
    {
        base.Shoot();
        if (canShoot && isAmo)
        {
            StartCoroutine(ShootCoroutine(fireRate));
        }
    }

    private IEnumerator ShootCoroutine(float shootDelay)
    {
        canShoot = false;
        yield return new WaitForSeconds(shootDelay);
        canShoot = true;

        shootParticle?.Play();
        shellParticle?.Play();

        if (client != null)
        {
            if (Input.GetMouseButton(0) && CheckAmo())
            {
                Debug.Log("Shotgun Shooting!");

                // Visualize the main ray in the editor for debugging
                Debug.DrawRay(shootOut.position, shootOut.forward * range, Color.red, 2f);
                
                // Log ray origin and direction for debugging
                Debug.Log($"Shotgun Ray Origin: {shootOut.position}, Direction: {shootOut.forward}");

                bool hitDetected = false;
                int layerMask = ~0; // All layers

                // Fire multiple pellets with spread
                for (int i = 0; i < pelletCount; i++)
                {
                    Vector3 direction = GetSpreadDirection();
                    
                    if (Physics.Raycast(shootOut.position, direction, out RaycastHit hit, range, layerMask))
                    {
                        Debug.Log($"Shotgun pellet {i} hit: {hit.collider.name} at distance {hit.distance}");
                        var networkPlayer = hit.collider.GetComponent<NetworkPlayer>();
                        if (networkPlayer != null)
                        {
                            Debug.Log("Shotgun hit player: " + networkPlayer.PlayerId);
                            hitDetected = true;
                        }
                        
                        HandleHit(hit, true);
                        SpawnTrail(hit.point);
                    }
                    else
                    {
                        Vector3 missPoint = shootOut.position + direction * range;
                        SpawnTrail(missPoint);
                    }
                }

                // Send shoot packet to server (use main direction for server communication)
                if (hitDetected)
                {
                    Debug.Log("Shotgun detected hit, sending shoot packet");
                }
                else
                {
                    Debug.LogWarning("Shotgun missed all pellets");
                }
                
                // Always send the shoot packet with the main direction
                client.PerformShoot(shootOut.position, shootOut.forward);
            }
        }
        else
        {
            Debug.Log("Client is null in Shotgun");
        }

        amo--;
        if (amo <= 0) isAmo = false;
        gunInfo?.UpdateInfo();
    }

    private Vector3 GetSpreadDirection()
    {
        Quaternion spreadRot = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        );
        return spreadRot * shootOut.forward;
    }

    private void HandleHit(RaycastHit hit, bool isHit)
    {
        string tag = hit.transform.tag;
        GameObject holePrefab = null;

        switch (tag)
        {
            case "Enemy":
                holePrefab = shootHoles[1];
                crosshair?.HitEnemy();
                break;
            case "InvisibleWall":
                break;
            default:
                holePrefab = shootHoles[0];
                break;
        }

        if (holePrefab)
        {
            GameObject hole = Instantiate(holePrefab, hit.point, Quaternion.LookRotation(hit.normal));
            hole.transform.SetParent(hit.transform);
        }

        Rigidbody rb = hit.rigidbody;
        if (rb != null)
        {
            rb.AddForce(-hit.normal * 10f, ForceMode.Impulse);
        }
    }

    private void SpawnTrail(Vector3 target)
    {
        TrailRenderer trail = Instantiate(shootTrail, shootOut.position, Quaternion.identity);
        StartCoroutine(AnimateTrail(trail, target));
    }

    private IEnumerator AnimateTrail(TrailRenderer trail, Vector3 target)
    {
        float time = 0f;
        Vector3 start = trail.transform.position;

        while (time < 1f)
        {
            trail.transform.position = Vector3.Lerp(start, target, time);
            time += Time.deltaTime / trail.time;
            yield return null;
        }

        trail.transform.position = target;
        Destroy(trail.gameObject, trail.time);
    }
}
