using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InventoryFramework;

public class PlayerHUDManager : MonoBehaviour
{
    public static PlayerHUDManager Instance { get; private set; }

    [Header("Time Display")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dateText;

    [Header("Equipped Item Display")]
    [SerializeField] private TextMeshProUGUI equippedItemText;

    [Header("Health & Oxygen Sliders")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthValueText;
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private TextMeshProUGUI oxygenValueText;

    [Header("Resource Radar (500m)")]
    [SerializeField] private float scanRadius = 500f;
    [SerializeField] private int maxDisplayedResources = 5;
    [SerializeField] private TextMeshProUGUI resourceRadarTitleText;
    [SerializeField] private TextMeshProUGUI resourceCountText;
    [SerializeField] private TextMeshProUGUI resourceListText;

    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerVitals playerVitals;
    [SerializeField] private HotbarUI hotbarUI;

    // Cache and scan timers
    private float resourceCacheTimer = 0f;
    private const float ResourceCacheInterval = 1.5f;
    private float radarUpdateTimer = 0f;
    private const float RadarUpdateInterval = 0.25f;

    private MineableResource[] cachedResources = new MineableResource[0];
    private struct ResourceDistance
    {
        public string name;
        public float distance;
    }
    private readonly List<ResourceDistance> detectedResources = new List<ResourceDistance>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (playerVitals != null)
        {
            playerVitals.OnHealthChanged -= UpdateHealthUI;
            playerVitals.OnOxygenChanged -= UpdateOxygenUI;
        }
    }

    private void Start()
    {
        ResolveReferences();

        if (playerVitals != null)
        {
            playerVitals.OnHealthChanged += UpdateHealthUI;
            playerVitals.OnOxygenChanged += UpdateOxygenUI;
            UpdateHealthUI(playerVitals.CurrentHealth, playerVitals.MaxHealth);
            UpdateOxygenUI(playerVitals.CurrentOxygen, playerVitals.MaxOxygen);
        }

        RefreshResourceCache();
        UpdateResourceRadar();
    }

    private void ResolveReferences()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerVitals == null && playerTransform != null)
        {
            playerVitals = playerTransform.GetComponent<PlayerVitals>();
            if (playerVitals == null)
            {
                playerVitals = PlayerVitals.Instance ?? FindAnyObjectByType<PlayerVitals>();
            }
        }

        if (hotbarUI == null)
        {
            hotbarUI = FindAnyObjectByType<HotbarUI>();
        }
    }

    private void Update()
    {
        UpdateTimeUI();
        UpdateEquippedItemUI();
        HandleResourceRadarTiming();

        // Fallback sync for vitals if not updated via events
        if (playerVitals != null)
        {
            if (healthSlider != null && Mathf.Abs(healthSlider.value - playerVitals.CurrentHealth) > 0.05f)
            {
                UpdateHealthUI(playerVitals.CurrentHealth, playerVitals.MaxHealth);
            }
            if (oxygenSlider != null && Mathf.Abs(oxygenSlider.value - playerVitals.CurrentOxygen) > 0.05f)
            {
                UpdateOxygenUI(playerVitals.CurrentOxygen, playerVitals.MaxOxygen);
            }
        }
    }

    #region Time UI

    private void UpdateTimeUI()
    {
        if (timeText == null) return;

        if (GameManager.Instance != null && GameManager.Instance.gameTime != null)
        {
            timeText.text = GameManager.Instance.gameTime.ToTimeString();

            if (dateText != null)
            {
                dateText.text = $"SOL {GameManager.Instance.gameTime.Day} • {GameManager.Instance.gameTime.Year}";
            }
        }
        else
        {
            timeText.text = System.DateTime.Now.ToString("HH:mm");
            if (dateText != null)
            {
                dateText.text = "LUNE • BASE ALPHA";
            }
        }
    }

    #endregion

    #region Equipped Item UI

    private void UpdateEquippedItemUI()
    {
        if (equippedItemText == null) return;

        if (hotbarUI == null)
        {
            hotbarUI = FindAnyObjectByType<HotbarUI>();
        }

        if (hotbarUI != null)
        {
            Item item = hotbarUI.GetSelectedItem();
            if (item != null)
            {
                if (item is BuildingSystem.BlueprintItem bp && bp.moduleData != null)
                {
                    equippedItemText.text = $"Plan : <color=#79ffe1>{bp.moduleData.moduleName}</color>";
                }
                else
                {
                    string name = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
                    equippedItemText.text = $"Équipé : <color=#ffdf5d>{name}</color>";
                }
            }
            else
            {
                equippedItemText.text = "Équipé : <color=#a0a8b4>Mains nues</color>";
            }
        }
        else
        {
            equippedItemText.text = "Équipé : <color=#a0a8b4>--</color>";
        }
    }

    #endregion

    #region Health & Oxygen UI

    private void UpdateHealthUI(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = max;
            healthSlider.value = Mathf.Clamp(current, 0f, max);
        }

        if (healthValueText != null)
        {
            healthValueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)} PV";
        }
    }

    private void UpdateOxygenUI(float current, float max)
    {
        if (oxygenSlider != null)
        {
            oxygenSlider.minValue = 0f;
            oxygenSlider.maxValue = max;
            oxygenSlider.value = Mathf.Clamp(current, 0f, max);
        }

        if (oxygenValueText != null)
        {
            int percent = Mathf.RoundToInt((current / Mathf.Max(1f, max)) * 100f);
            oxygenValueText.text = $"{percent}% O2";
        }
    }

    #endregion

    #region Resource Radar (500m)

    private void HandleResourceRadarTiming()
    {
        resourceCacheTimer += Time.deltaTime;
        if (resourceCacheTimer >= ResourceCacheInterval)
        {
            resourceCacheTimer = 0f;
            RefreshResourceCache();
        }

        radarUpdateTimer += Time.deltaTime;
        if (radarUpdateTimer >= RadarUpdateInterval)
        {
            radarUpdateTimer = 0f;
            UpdateResourceRadar();
        }
    }

    public void RefreshResourceCache()
    {
        cachedResources = FindObjectsByType<MineableResource>(FindObjectsInactive.Exclude);
    }

    public void UpdateResourceRadar()
    {
        if (playerTransform == null)
        {
            ResolveReferences();
            if (playerTransform == null) return;
        }

        Vector3 pPos = playerTransform.position;
        detectedResources.Clear();

        if (cachedResources != null)
        {
            for (int i = 0; i < cachedResources.Length; i++)
            {
                MineableResource res = cachedResources[i];
                if (res == null) continue;

                float dist = Vector3.Distance(pPos, res.transform.position);
                if (dist <= scanRadius)
                {
                    string displayName = GetFriendlyResourceName(res);
                    detectedResources.Add(new ResourceDistance
                    {
                        name = displayName,
                        distance = dist
                    });
                }
            }
        }

        // Sort by distance ascending
        detectedResources.Sort((a, b) => a.distance.CompareTo(b.distance));

        if (resourceRadarTitleText != null)
        {
            resourceRadarTitleText.text = "RADAR RESSOURCES (500m)";
        }

        if (resourceCountText != null)
        {
            resourceCountText.text = detectedResources.Count > 0
                ? $"<color=#70e5ff>{detectedResources.Count} gisements</color> à portée"
                : "<color=#999999>Aucun gisement détecté</color>";
        }

        if (resourceListText != null)
        {
            if (detectedResources.Count == 0)
            {
                resourceListText.text = "<color=#888888><i>Aucune ressource dans un rayon de 500m</i></color>";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                int displayCount = Mathf.Min(maxDisplayedResources, detectedResources.Count);

                for (int i = 0; i < displayCount; i++)
                {
                    var item = detectedResources[i];
                    string distColor = item.distance < 100f ? "#4aff82" : (item.distance < 250f ? "#ffd84a" : "#72d5ff");
                    sb.AppendLine($"• {item.name} : <color={distColor}>{Mathf.RoundToInt(item.distance)}m</color>");
                }

                if (detectedResources.Count > displayCount)
                {
                    sb.Append($"<size=80%><color=#aaaaaa>+ {detectedResources.Count - displayCount} autres à proximité...</color></size>");
                }

                resourceListText.text = sb.ToString().TrimEnd();
            }
        }
    }

    private string GetFriendlyResourceName(MineableResource res)
    {
        if (res == null) return "Ressource";

        if (!string.IsNullOrWhiteSpace(res.resourceName))
        {
            return res.resourceName.Trim();
        }

        if (res.multipleDrops != null && res.multipleDrops.Count > 0 && res.multipleDrops[0].item != null)
        {
            string dropName = res.multipleDrops[0].item.itemName;
            if (dropName == "IceResource" || dropName.Contains("Ice")) return "Glace d'Eau (Glace)";
            return !string.IsNullOrEmpty(dropName) ? dropName : res.multipleDrops[0].item.name;
        }

        if (res.resourceItem != null)
        {
            string itemName = res.resourceItem.itemName;
            return !string.IsNullOrEmpty(itemName) ? itemName : res.resourceItem.name;
        }

        if (!string.IsNullOrWhiteSpace(res.resourceId))
        {
            return res.resourceId.Trim();
        }

        string goName = res.gameObject.name;
        if (goName.StartsWith("RockIce")) return "Glace (RockIce)";
        if (goName.StartsWith("RegoliteRock")) return "Régolithe Lunaire";

        return goName;
    }

    #endregion
}
