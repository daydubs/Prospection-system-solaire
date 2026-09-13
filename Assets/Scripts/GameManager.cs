using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
            }
            return instance;
        }
        private set => instance = value;
    }

    [Header("In-Game Time Settings")]
    [Tooltip("Starting date and current time in the simulation")]
    public GameDateTime gameTime = new GameDateTime(2150, 1, 1, 8, 0);

    [Tooltip("How many in-game seconds pass per real second at 1x simulation scale. (e.g. 720 = 1 day per 2 real minutes at 1x)")]
    public float inGameSecondsPerRealSecond = 720f;
    public bool isTimePaused = false;

    [Header("Economy & Credits")]
    [SerializeField] private double credits = 25000.0; // Starting capital in Credits (CR)

    public double Credits => credits;

    [Header("Player & Vessel Stats")]
    public PlayerStats playerStats = new PlayerStats();
    public ShipStats shipStats = new ShipStats();

    [Header("Exploration & Discovery")]
    public List<string> discoveredBodies = new List<string> { "Soleil", "Terre", "Lune" };

    [Header("Technology & Research")]
    public List<TechnologyData> allTechnologies = new List<TechnologyData>();
    private HashSet<string> unlockedTechIds = new HashSet<string>();

    // Events for UI and gameplay systems to subscribe to
    public event Action<GameDateTime> OnTimeUpdated;
    public event Action<GameDateTime> OnDayChanged;
    public event Action<GameDateTime> OnMonthChanged;
    public event Action<GameDateTime> OnYearChanged;
    public event Action<double, double> OnCreditsChanged; // currentCredits, delta
    public event Action<string> OnTechUnlocked;
    public event Action<PlayerStats> OnPlayerStatsChanged;
    public event Action<ShipStats> OnShipStatsChanged;
    public event Action<string, float, float> OnCargoChanged; // resId, currentQty, delta
    public event Action<string> OnBodyDiscovered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeDefaultTechTree();
    }

    private void OnValidate()
    {
        if (allTechnologies == null || allTechnologies.Count == 0)
        {
            InitializeDefaultTechTree();
        }
    }

    private void Start()
    {
        // Initial event fire for any listening UI
        OnCreditsChanged?.Invoke(credits, 0);
        OnPlayerStatsChanged?.Invoke(playerStats);
        OnShipStatsChanged?.Invoke(shipStats);
        OnTimeUpdated?.Invoke(gameTime);
    }

    private void Update()
    {
        UpdateTimeProgression();
        UpdateShipPassiveRegen();
    }

    #region Time Progression

    private void UpdateTimeProgression()
    {
        if (isTimePaused) return;

        // Check if SolarSystemManager is paused or has a custom time scale
        float simScale = 1.0f;
        if (SolarSystemManager.Instance != null)
        {
            if (SolarSystemManager.Instance.isPaused) return;
            simScale = SolarSystemManager.Instance.timeScale;
        }

        float deltaInGameSec = Time.deltaTime * inGameSecondsPerRealSecond * simScale;
        if (deltaInGameSec <= 0f) return;

        bool dayChanged = gameTime.AdvanceSeconds(deltaInGameSec, out bool monthChanged, out bool yearChanged);

        OnTimeUpdated?.Invoke(gameTime);

        if (dayChanged)
        {
            OnDayChanged?.Invoke(gameTime);
        }
        if (monthChanged)
        {
            OnMonthChanged?.Invoke(gameTime);
        }
        if (yearChanged)
        {
            OnYearChanged?.Invoke(gameTime);
        }
    }

    public void AdvanceDays(int days)
    {
        if (days <= 0) return;
        gameTime.AdvanceDays(days);
        OnTimeUpdated?.Invoke(gameTime);
        OnDayChanged?.Invoke(gameTime);
        Debug.Log($"[GameManager] Time advanced by {days} days. Current Date: {gameTime.ToFullDateTimeString()}");
    }

    public void SetTimePaused(bool pause)
    {
        isTimePaused = pause;
    }

    #endregion

    #region Economy & Credits

    public void AddCredits(double amount, string reason = "")
    {
        if (amount <= 0) return;
        credits += amount;
        playerStats.totalCreditsEarned += amount;
        OnCreditsChanged?.Invoke(credits, amount);
        Debug.Log($"[GameManager] +{amount:N0} CR ({reason}). Total: {credits:N0} CR");
    }

    public bool SpendCredits(double amount, string reason = "")
    {
        if (amount <= 0) return true;
        if (credits < amount)
        {
            Debug.LogWarning($"[GameManager] Crédits insuffisants! Requis: {amount:N0} CR, Actuel: {credits:N0} CR ({reason})");
            return false;
        }

        credits -= amount;
        OnCreditsChanged?.Invoke(credits, -amount);
        Debug.Log($"[GameManager] -{amount:N0} CR ({reason}). Restant: {credits:N0} CR");
        return true;
    }

    public bool CanAfford(double amount)
    {
        return credits >= amount;
    }

    #endregion

    #region Technology Management

    public void InitializeDefaultTechTree()
    {
        if (allTechnologies == null)
        {
            allTechnologies = new List<TechnologyData>();
        }

        if (allTechnologies.Count == 0)
        {
            // Base starter tech tree
            allTechnologies.Add(new TechnologyData("tech_propulsion_1", "Propulseur Ionique Standard", TechCategory.Propulsion, "Augmente la vitesse de croisière de 20% et réduit la consommation de carburant de 15%.", 15000, 3f, true));
            allTechnologies.Add(new TechnologyData("tech_propulsion_2", "Moteur Warp Sub-Luminique", TechCategory.Propulsion, "Double la vitesse de déplacement rapide entre les planètes.", 45000, 7f, false));
            
            allTechnologies.Add(new TechnologyData("tech_scan_1", "Scanner Spectrométrique Basique", TechCategory.Scanning, "Permet d'analyser la composition des astéroïdes à moyenne portée.", 8000, 2f, true));
            allTechnologies.Add(new TechnologyData("tech_scan_2", "Radar Géologique Terahertz", TechCategory.Scanning, "Détecte les filons de métaux rares (Or, Platine) et les poches d'Hélium-3.", 28000, 5f, false));

            allTechnologies.Add(new TechnologyData("tech_mining_1", "Laser Minier Impulsionnel", TechCategory.Extraction, "Foreuse laser d'entrée de gamme pour fragmenter les astéroïdes rocheux.", 12000, 2f, true));
            allTechnologies.Add(new TechnologyData("tech_mining_2", "Rayon Extracteur à Plasma", TechCategory.Extraction, "Augmente le rendement de minage de 50% et extrait les glaces volatiles.", 35000, 6f, false));

            allTechnologies.Add(new TechnologyData("tech_hull_1", "Blindage Composite Titane", TechCategory.HullAndShield, "Renforce la coque du vaisseau de +50 PV max.", 18000, 4f, false));
            allTechnologies.Add(new TechnologyData("tech_shield_1", "Déflecteur Électrostatique", TechCategory.HullAndShield, "Génère un bouclier capable d'absorber les micro-météorites.", 22000, 4f, false));
            
            allTechnologies.Add(new TechnologyData("tech_cargo_1", "Soutes Compactes Modulaires", TechCategory.Science, "Augmente la capacité de fret de +50 tonnes.", 20000, 4f, false));
        }

        if (unlockedTechIds == null)
        {
            unlockedTechIds = new HashSet<string>();
        }
        else
        {
            unlockedTechIds.Clear();
        }

        foreach (var t in allTechnologies)
        {
            if (t.isUnlocked)
            {
                unlockedTechIds.Add(t.id);
            }
        }
    }

    public bool IsTechUnlocked(string techId)
    {
        if (unlockedTechIds == null) return false;
        return unlockedTechIds.Contains(techId);
    }

    public bool UnlockTechnology(string techId)
    {
        TechnologyData tech = allTechnologies.Find(t => t.id == techId);
        if (tech == null)
        {
            Debug.LogWarning($"[GameManager] Technologie '{techId}' introuvable.");
            return false;
        }

        if (tech.isUnlocked)
        {
            return true;
        }

        // Check prerequisites
        foreach (var prereq in tech.prerequisites)
        {
            if (!IsTechUnlocked(prereq))
            {
                Debug.LogWarning($"[GameManager] Prérequis manquant '{prereq}' pour '{tech.displayName}'.");
                return false;
            }
        }

        // Deduct cost
        if (!SpendCredits(tech.researchCost, $"Recherche : {tech.displayName}"))
        {
            return false;
        }

        tech.isUnlocked = true;
        unlockedTechIds.Add(tech.id);
        ApplyTechEffects(tech);

        OnTechUnlocked?.Invoke(techId);
        Debug.Log($"[GameManager] 🔬 Technologie débloquée avec succès: {tech.displayName} !");
        return true;
    }

    private void ApplyTechEffects(TechnologyData tech)
    {
        switch (tech.id)
        {
            case "tech_propulsion_1":
                shipStats.cruiseSpeedModifier = 1.2f;
                shipStats.fuelConsumptionRate *= 0.85f;
                break;
            case "tech_propulsion_2":
                shipStats.warpDriveTier = 2;
                break;
            case "tech_scan_2":
                shipStats.scannerRange = 500f;
                break;
            case "tech_mining_2":
                shipStats.miningLaserPower *= 1.5f;
                break;
            case "tech_hull_1":
                shipStats.maxHull += 50f;
                shipStats.currentHull += 50f;
                break;
            case "tech_shield_1":
                shipStats.maxShield += 50f;
                shipStats.currentShield += 50f;
                break;
            case "tech_cargo_1":
                shipStats.maxCargoCapacity += 50f;
                break;
        }
        OnShipStatsChanged?.Invoke(shipStats);
    }

    public List<TechnologyData> GetUnlockedTechnologies()
    {
        return allTechnologies.FindAll(t => t.isUnlocked);
    }

    #endregion

    #region Ship Operations & Cargo

    private void UpdateShipPassiveRegen()
    {
        // Regenerate shield if not fully depleted
        if (shipStats.currentShield < shipStats.maxShield)
        {
            shipStats.currentShield = Mathf.Min(shipStats.maxShield, shipStats.currentShield + shipStats.shieldRegenRate * Time.deltaTime);
        }

        // Regenerate energy
        if (shipStats.currentEnergy < shipStats.maxEnergy)
        {
            shipStats.currentEnergy = Mathf.Min(shipStats.maxEnergy, shipStats.currentEnergy + shipStats.energyRechargeRate * Time.deltaTime);
        }
    }

    public bool DamageShip(float damage)
    {
        if (damage <= 0) return false;

        // Damage shield first
        if (shipStats.currentShield > 0)
        {
            float shieldDmg = Mathf.Min(shipStats.currentShield, damage);
            shipStats.currentShield -= shieldDmg;
            damage -= shieldDmg;
        }

        // Remaining damage goes to hull
        if (damage > 0)
        {
            shipStats.currentHull = Mathf.Max(0f, shipStats.currentHull - damage);
        }

        OnShipStatsChanged?.Invoke(shipStats);

        if (shipStats.currentHull <= 0)
        {
            Debug.LogError("[GameManager] Coque détruite ! Le vaisseau est hors service !");
            return true; // Destroyed
        }
        return false;
    }

    public void RepairHull(float amount, double costPerPoint = 10.0)
    {
        float needed = shipStats.maxHull - shipStats.currentHull;
        float actualRepair = Mathf.Min(amount, needed);
        if (actualRepair <= 0) return;

        double totalCost = actualRepair * costPerPoint;
        if (SpendCredits(totalCost, "Réparation de la coque"))
        {
            shipStats.currentHull += actualRepair;
            OnShipStatsChanged?.Invoke(shipStats);
            Debug.Log($"[GameManager] Coque réparée de +{actualRepair:F1} PV pour {totalCost:N0} CR.");
        }
    }

    public void Refuel(float amount, double costPerLiter = 2.0)
    {
        float needed = shipStats.maxFuel - shipStats.currentFuel;
        float actualFuel = Mathf.Min(amount, needed);
        if (actualFuel <= 0) return;

        double totalCost = actualFuel * costPerLiter;
        if (SpendCredits(totalCost, "Ravitaillement en carburant"))
        {
            shipStats.currentFuel += actualFuel;
            OnShipStatsChanged?.Invoke(shipStats);
            Debug.Log($"[GameManager] Ravitaillement de +{actualFuel:F1} L effectué pour {totalCost:N0} CR.");
        }
    }

    public bool ConsumeFuel(float amount)
    {
        if (shipStats.currentFuel < amount)
        {
            shipStats.currentFuel = 0f;
            OnShipStatsChanged?.Invoke(shipStats);
            return false;
        }
        shipStats.currentFuel -= amount;
        OnShipStatsChanged?.Invoke(shipStats);
        return true;
    }

    public bool AddCargo(string resourceId, string resourceName, float quantityTons, double basePrice)
    {
        bool success = shipStats.AddCargo(resourceId, resourceName, quantityTons, basePrice);
        if (success)
        {
            playerStats.totalCargoMinedTons += quantityTons;
            OnShipStatsChanged?.Invoke(shipStats);
            OnCargoChanged?.Invoke(resourceId, shipStats.GetCargoAmount(resourceId), quantityTons);
            Debug.Log($"[GameManager] Ajout au fret : +{quantityTons:F1}t de {resourceName}. Soute : {shipStats.CurrentCargoTons:F1}/{shipStats.maxCargoCapacity:F1}t");
        }
        else
        {
            Debug.LogWarning($"[GameManager] Soute pleine ! Impossible de charger {quantityTons:F1}t de {resourceName}.");
        }
        return success;
    }

    public bool SellCargo(string resourceId, float quantityTons, double priceMultiplier = 1.0)
    {
        CargoItem item = shipStats.cargo.Find(c => c.resourceId == resourceId);
        if (item == null || item.quantity < quantityTons)
        {
            Debug.LogWarning($"[GameManager] Quantité insuffisante de {resourceId} pour vendre.");
            return false;
        }

        double earned = quantityTons * item.basePricePerTon * priceMultiplier;
        shipStats.RemoveCargo(resourceId, quantityTons);
        AddCredits(earned, $"Vente de {quantityTons:F1}t de {item.resourceName}");

        OnShipStatsChanged?.Invoke(shipStats);
        OnCargoChanged?.Invoke(resourceId, shipStats.GetCargoAmount(resourceId), -quantityTons);
        return true;
    }

    #endregion

    #region Discovery & Records

    public void RegisterDiscoveredBody(string bodyName)
    {
        if (string.IsNullOrEmpty(bodyName)) return;

        if (!discoveredBodies.Contains(bodyName))
        {
            discoveredBodies.Add(bodyName);
            playerStats.totalBodiesVisited++;
            playerStats.AddXP(150);
            OnBodyDiscovered?.Invoke(bodyName);
            Debug.Log($"[GameManager] 🌟 Nouveau corps céleste exploré : {bodyName} (+150 XP) !");
        }
    }

    #endregion

    #region New Game Initialization

    public void StartNewGame(string pilotName, string corpName, int difficultyLevel)
    {
        // Reset or init time
        gameTime = new GameDateTime(2150, 1, 1, 8, 0);

        // Reset player stats
        playerStats = new PlayerStats
        {
            pilotName = string.IsNullOrWhiteSpace(pilotName) ? "Commandant Dany" : pilotName.Trim(),
            corporationName = string.IsNullOrWhiteSpace(corpName) ? "Astraea Mining Corp" : corpName.Trim(),
            level = 1,
            currentXP = 0,
            xpToNextLevel = 1000,
            miningSkill = 1,
            scanningSkill = 1,
            pilotingSkill = 1,
            tradingSkill = 1,
            totalBodiesVisited = 1,
            totalAsteroidsMined = 0,
            totalCreditsEarned = 0,
            totalCargoMinedTons = 0f,
            totalDistanceTraveledKm = 0f,
            difficultyLevel = difficultyLevel
        };

        // Reset ship stats
        shipStats = new ShipStats();

        // Configure difficulty specifics (credits & faction influence/reputations)
        switch (difficultyLevel)
        {
            case 0: // Facile / Débutant
                playerStats.difficultyName = "Facile (Pionnier Subventionné)";
                playerStats.title = "Directeur de Prospection";
                credits = 50000.0;
                playerStats.repEarthCoalition = 25f;
                playerStats.repMarsRepublic = 20f;
                playerStats.repBeltAlliance = 20f;
                playerStats.repJovianConsortium = 20f;
                break;

            case 1: // Normal / Intermédiaire
                playerStats.difficultyName = "Normal (Prospecteur Standard)";
                playerStats.title = "Prospecteur Indépendant";
                credits = 25000.0;
                playerStats.repEarthCoalition = 10f;
                playerStats.repMarsRepublic = 0f;
                playerStats.repBeltAlliance = 5f;
                playerStats.repJovianConsortium = 0f;
                break;

            case 2: // Difficile / Expert
                playerStats.difficultyName = "Difficile (Vétéran Endetté)";
                playerStats.title = "Prospecteur Endetté";
                credits = 10000.0;
                playerStats.repEarthCoalition = -10f;
                playerStats.repMarsRepublic = -5f;
                playerStats.repBeltAlliance = 10f;
                playerStats.repJovianConsortium = -10f;
                break;
        }

        // Reset discoveries & techs
        discoveredBodies = new List<string> { "Soleil", "Terre", "Lune" };
        InitializeDefaultTechTree();

        // Notify systems
        OnCreditsChanged?.Invoke(credits, 0);
        OnPlayerStatsChanged?.Invoke(playerStats);
        OnShipStatsChanged?.Invoke(shipStats);
        OnTimeUpdated?.Invoke(gameTime);

        Debug.Log($"[GameManager] 🚀 Nouvelle partie démarrée ! Pilote: {playerStats.pilotName}, Corp: {playerStats.corporationName}, Difficulté: {playerStats.difficultyName}, Crédits: {credits:N0} CR");
    }

    #endregion

    #region Save & Load System

    public void SaveGame(int slotIndex = 1)
    {
        SaveGameData save = new SaveGameData
        {
            saveSlotName = $"Slot_{slotIndex}",
            saveDateRealTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            gameTime = gameTime.Clone(),
            credits = this.credits,
            unlockedTechIds = new List<string>(unlockedTechIds),
            playerStats = this.playerStats,
            shipStats = this.shipStats,
            discoveredBodies = new List<string>(this.discoveredBodies)
        };

        string json = JsonUtility.ToJson(save, true);
        PlayerPrefs.SetString($"SaveSlot_{slotIndex}", json);
        PlayerPrefs.Save();

        Debug.Log($"[GameManager] 💾 Partie sauvegardée dans l'emplacement #{slotIndex} ! ({save.saveDateRealTime})");
    }

    public bool LoadGame(int slotIndex = 1)
    {
        string key = $"SaveSlot_{slotIndex}";
        if (!PlayerPrefs.HasKey(key))
        {
            Debug.LogWarning($"[GameManager] Aucun fichier de sauvegarde trouvé pour l'emplacement #{slotIndex}.");
            return false;
        }

        try
        {
            string json = PlayerPrefs.GetString(key);
            SaveGameData save = JsonUtility.FromJson<SaveGameData>(json);

            if (save != null)
            {
                this.gameTime = save.gameTime ?? new GameDateTime(2150, 1, 1, 8, 0);
                this.credits = save.credits;
                this.playerStats = save.playerStats ?? new PlayerStats();
                this.shipStats = save.shipStats ?? new ShipStats();
                this.discoveredBodies = save.discoveredBodies ?? new List<string>();

                unlockedTechIds.Clear();
                if (save.unlockedTechIds != null)
                {
                    foreach (var id in save.unlockedTechIds)
                    {
                        unlockedTechIds.Add(id);
                        TechnologyData td = allTechnologies.Find(t => t.id == id);
                        if (td != null) td.isUnlocked = true;
                    }
                }

                // Notify all listeners
                OnTimeUpdated?.Invoke(this.gameTime);
                OnCreditsChanged?.Invoke(this.credits, 0);
                OnPlayerStatsChanged?.Invoke(this.playerStats);
                OnShipStatsChanged?.Invoke(this.shipStats);

                Debug.Log($"[GameManager] 📂 Partie chargée avec succès depuis l'emplacement #{slotIndex} !");
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] Erreur lors du chargement de la sauvegarde #{slotIndex} : {ex.Message}");
        }

        return false;
    }

    public bool HasSaveGame(int slotIndex = 1)
    {
        return PlayerPrefs.HasKey($"SaveSlot_{slotIndex}");
    }

    #endregion
}
