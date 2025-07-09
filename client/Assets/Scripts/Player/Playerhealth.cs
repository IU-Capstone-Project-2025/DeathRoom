using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Playerhealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxArmor = 100f;
    [SerializeField] private float currentArmor = 100f;
    [SerializeField] private float armorDamageReduction = 0.5f;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI ArmorText;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float damage)
    {
        float damageToHealth = damage;

        if (currentArmor > 0)
        {
            float damageToArmor = damage * armorDamageReduction;
            damageToHealth = damage * (1 - armorDamageReduction);
            
            currentArmor -= damageToArmor;
            currentArmor = Mathf.Max(currentArmor, 0);

            if (currentArmor <= 0)
            {
                currentArmor = 0;
            }
            
            UpdateArmorUI();
        }

        currentHealth -= damageToHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0) Die();
    }

    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }
    public void RepairArmor(float repairAmount)
    {
        currentArmor += repairAmount;
        currentArmor = Mathf.Clamp(currentArmor, 0, maxArmor);
        UpdateArmorUI();
    }

    private void UpdateHealthUI()
    {
        if (healthBarSlider != null)
            healthBarSlider.value = currentHealth / maxHealth;

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }
    private void UpdateArmorUI()
    {
        if (ArmorText != null)
            ArmorText.text = $"{Mathf.RoundToInt(currentArmor)} / {maxArmor}";
    }

    private void Die()
    {
        Debug.Log("Игрок умер.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            TakeDamage(10);
    }
}
