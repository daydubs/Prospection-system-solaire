using System;
using UnityEngine;

public class PlayerVitals : MonoBehaviour
{
    public static PlayerVitals Instance { get; private set; }

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Oxygen Settings")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float currentOxygen = 100f;
    [Tooltip("Consommation d'oxygène par seconde")]
    [SerializeField] private float oxygenDepletionRate = 0.5f;
    [Tooltip("Dégâts par seconde subis en cas de panne d'oxygène")]
    [SerializeField] private float suffocationDamageRate = 4.0f;
    [SerializeField] private bool consumeOxygen = true;

    [Header("Inventory Reference")]
    [SerializeField] private InventoryFramework.Inventory playerInventory;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float MaxOxygen => maxOxygen;
    public float CurrentOxygen => currentOxygen;
    public bool ConsumeOxygen { get => consumeOxygen; set => consumeOxygen = value; }

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnOxygenChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponentInChildren<InventoryFramework.Inventory>(true) ?? FindAnyObjectByType<InventoryFramework.Inventory>();
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }

    private void Update()
    {
        // Consommation progressive d'oxygène en milieu hostile (Lune / espace)
        if (consumeOxygen && currentOxygen > 0f)
        {
            float rate = oxygenDepletionRate * Time.deltaTime;
            if (playerInventory != null)
            {
                float drawn = playerInventory.ConsumeEquippedOxygen(rate);
                rate -= drawn;
            }

            if (rate > 0f)
            {
                currentOxygen = Mathf.Max(0f, currentOxygen - rate);
                OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
            }
            else if (currentOxygen < maxOxygen && playerInventory != null)
            {
                // Si la combinaison a perdu un peu d'O2, recharge progressive depuis les bouteilles
                float recharge = Mathf.Min(maxOxygen - currentOxygen, 2f * Time.deltaTime);
                float drawn = playerInventory.ConsumeEquippedOxygen(recharge);
                if (drawn > 0f)
                {
                    currentOxygen = Mathf.Min(maxOxygen, currentOxygen + drawn);
                    OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
                }
            }
        }

        // Dégâts d'asphyxie lorsque l'oxygène tombe à zéro
        if (currentOxygen <= 0f && currentHealth > 0f)
        {
            TakeDamage(suffocationDamageRate * Time.deltaTime);
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddOxygen(float amount)
    {
        if (amount <= 0f) return;
        currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
        OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
    }

    private void Die()
    {
        Debug.LogWarning("[PlayerVitals] Le joueur est à court de vie !");
        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ShowNotification("Alerte Critique : Signes vitaux compromis !");
        }
    }
}
