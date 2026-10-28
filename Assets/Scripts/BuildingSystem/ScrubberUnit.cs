using UnityEngine;
using InventoryFramework;

/// <summary>
/// Attached to the placed Scrubber unit.
/// Purifies CO2 and generates oxygen for the player inside the base/habitat.
/// Implements IPromptInteractable so the player can check its status or dismantle it.
/// </summary>
public class ScrubberUnit : MonoBehaviour, IPromptInteractable
{
    [Header("Socket & Item References")]
    public EquipmentSocket hostSocket;
    public Item scrubberItem;

    [Header("Oxygen Generation Settings")]
    [Tooltip("Effective radius in meters where the scrubber provides breathable atmosphere.")]
    public float effectiveRadius = 25f;

    [Tooltip("Amount of oxygen replenished per second to the player when in range.")]
    public float oxygenGenerationRate = 1.5f;

    [Tooltip("If true, the scrubber is actively purifying CO2.")]
    public bool isOperational = true;

    [Header("Audio")]
    [SerializeField] private AudioSource loopAudioSource;

    private void Start()
    {
        if (scrubberItem == null)
        {
#if UNITY_EDITOR
            scrubberItem = UnityEditor.AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/Scrubber Unit.asset");
#endif
        }

        if (hostSocket == null)
        {
            hostSocket = GetComponentInParent<EquipmentSocket>();
        }
    }

    public void Initialize(EquipmentSocket socket, Item item)
    {
        hostSocket = socket;
        scrubberItem = item;
        isOperational = true;
    }

    private void Update()
    {
        if (!isOperational) return;

        // Life support: replenish oxygen when player is in range of the scrubber
        if (PlayerVitals.Instance != null)
        {
            float dist = Vector3.Distance(transform.position, PlayerVitals.Instance.transform.position);
            if (dist <= effectiveRadius)
            {
                if (PlayerVitals.Instance.CurrentOxygen < PlayerVitals.Instance.MaxOxygen)
                {
                    PlayerVitals.Instance.AddOxygen(oxygenGenerationRate * Time.deltaTime);
                }
            }
        }
    }

    public string GetInteractionPrompt()
    {
        return "Scrubber d'Oxygène [Actif - Épuration CO2]";
    }

    public void Interact()
    {
        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ShowNotification("Scrubber d'Oxygène opérationnel : Atmosphère purifiée et régénération O2 active.");
        }
    }

    /// <summary>
    /// Dismantles the scrubber and returns the item to the player.
    /// </summary>
    public void Dismantle()
    {
        if (hostSocket != null)
        {
            hostSocket.SetOccupied(false, null);
        }

        if (scrubberItem != null)
        {
            var hotbar = Object.FindAnyObjectByType<Hotbar>();
            bool added = hotbar != null && hotbar.AddItem(scrubberItem, 1);
            if (!added && GameManager.Instance != null)
            {
                added = GameManager.Instance.AddPlayerItem(scrubberItem, 1);
            }

            var hotbarUI = Object.FindAnyObjectByType<HotbarUI>();
            if (hotbarUI != null) hotbarUI.RefreshUI();
            var invUI = Object.FindAnyObjectByType<InventoryUI>();
            if (invUI != null) invUI.RefreshUI();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayConstructionSound();
        }

        if (MainMenuController.Instance != null)
        {
            MainMenuController.Instance.ShowNotification("Scrubber démonté et récupéré dans l'inventaire.");
        }

        Destroy(gameObject);
    }
}
