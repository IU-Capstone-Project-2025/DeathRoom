using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rifle : Gun
{
    public ParticleSystem shootParticle;
    public ParticleSystem shellParticle;
    public TrailRenderer shootTrial;
    public GameObject[] shootHoles;
    bool canShoot = true;
    
    public override void Shoot()
    {
        base.Shoot();
        if (canShoot && isAmo)
        {
            StartCoroutine(ShootCoroutine(fireRate));
        }
    }

    private IEnumerator ShootCoroutine(float shootTime)
    {
        canShoot = false;
        yield return new WaitForSeconds(shootTime);
        canShoot = true;
        shootParticle.Play();
        shellParticle.Play();
        TrailRenderer trail = Instantiate(shootTrial, shootOut.position, Quaternion.identity);
        RaycastHit hit;
        
        if (client != null)
        {
            if (Input.GetMouseButton(0) && CheckAmo())
            {
                Debug.Log("Shooting!");
                Shoot();

                // Visualize the ray in the editor with longer duration for debugging
                Debug.DrawRay(shootOut.position, shootOut.forward * 100f, Color.red, 2f);
                    
                // Log ray origin and direction for debugging
                Debug.Log($"Ray Origin: {shootOut.position}, Direction: {shootOut.forward}");

                int layerMask = ~0; // All layers
                if (Physics.Raycast(shootOut.position, shootOut.forward, out hit, 100f, layerMask))
                {
                    Debug.Log($"Hit: {hit.collider.name} at distance {hit.distance}");
                    var networkPlayer = hit.collider.GetComponent<NetworkPlayer>();
                    if (networkPlayer != null)
                    {
                        Debug.Log("Hit player: " + networkPlayer.PlayerId);
                        client.PerformShoot(shootOut.position, shootOut.forward);
                    }
                    else
                    {
                        Debug.Log($"Hit non-player object: {hit.collider.gameObject.layer}");
                        client.PerformShoot(shootOut.position,shootOut.forward);
                    }
                }
                else
                {
                    Debug.LogWarning("Ray missed! Check if objects have colliders and are in the right layers");
                    client.PerformShoot(shootOut.position, shootOut.forward);
                }
            }
        }
        else
        {
            Debug.Log("Client is null");
        }
        
        amo--;
        if (amo <= 0)
        {
            isAmo = false;
        }
        gunInfo.UpdateInfo();
        Quaternion recoilRotation = Quaternion.AngleAxis(Random.RandomRange(-recoil, recoil), transform.up) * Quaternion.AngleAxis(Random.RandomRange(-recoil, recoil), transform.right);
        bool isHit = Physics.Raycast(shootOut.position, recoilRotation * shootOut.forward * 1000f, out hit);
        if (isHit)
        {

            StartCoroutine(SpawnTrail(trail, hit, isHit));
        }
        else {
            hit.point = shootOut.forward * 100f;
            StartCoroutine(SpawnTrail(trail, hit, isHit));
        }
    }

    private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit, bool isHit) {

        float time = 0;
        Vector3 startPosition = trail.transform.position;

        while (time < 1) {

            trail.transform.position = Vector3.MoveTowards(startPosition, hit.point, time * 20f);
            time += Time.deltaTime / trail.time;
            yield return null;
        }
        if (isHit)
        {
            GameObject holePrefab = null;
            trail.transform.position = hit.point;
            Destroy(trail.gameObject, trail.time);
            string tag = hit.transform.tag;
            switch (tag)
            {
                case "Enemy":
                    holePrefab = shootHoles[1];
                    crosshair.HitEnemy();
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
                hole.transform.parent = hit.transform;
            }
            try
            {
                Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
                rb.AddForce(-hit.normal * 10f, ForceMode.Impulse);
            }
            catch { }
        }
    }

}
