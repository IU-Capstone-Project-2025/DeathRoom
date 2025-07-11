using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDevice : MonoBehaviour
{
    [Header("Levitation Settings")]
    [SerializeField] private float amplitude = 0.15f; 
    [SerializeField] private float frequency = 1f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        transform.Rotate(0, 30 * Time.deltaTime, 0);
    }
}
