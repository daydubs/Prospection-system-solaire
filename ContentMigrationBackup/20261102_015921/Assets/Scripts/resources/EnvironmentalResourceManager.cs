using System.Collections.Generic;
using UnityEngine;

public enum PlanetPresetType
{
    Moon_Luna,
    Europa,
    Mars,
    Asteroid,
    Custom
}

public enum ResourcePlacementConstraint
{
    Anywhere,              // Peux apparaître partout sur la surface (ex: Regolite sur la Lune, ou Glace sur Europe)
    InCratersOnly,         // Uniquement dans un cratère / dépression (ex: Glace sur la Lune)
    OutsideCratersOnly,    // Uniquement en plaine / hors des cratères
    SpecificHeightRange    // Plage d'altitude personnalisée (minAltitude -> maxAltitude)
}

[System.Serializable]
public class EnvironmentalResourceRule
{
    [Tooltip("Nom de la ressource (pour organisation et logs)")]
    public string resourceName = "Regolith";

    [Tooltip("Prefab de la ressource minable à instancier")]
    public GameObject prefab;

    [Tooltip("Nom du sous-dossier dans la hiérarchie")]
    public string subFolderName = "Regolith_Nodes";

    [Tooltip("Nombre total de roches/noeuds à placer")]
    [Range(1, 300)]
    public int spawnCount = 35;

    [Header("Échelle (Scale)")]
    [Tooltip("Échelle minimale de l'objet (ex: 25)")]
    [Range(1f, 120f)]
    public float minScale = 25f;

    [Tooltip("Échelle maximale de l'objet (ex: 60)")]
    [Range(1f, 120f)]
    public float maxScale = 60f;

    [Tooltip("Appliquer une légère variation non-uniforme sur les axes pour un look rocheux naturel")]
    public bool slightNonUniformVariation = true;

    [Tooltip("Pourcentage de variation non-uniforme (0.1 = +/- 10%)")]
    [Range(0f, 0.3f)]
    public float variationAmount = 0.08f;

    [Header("Contraintes d'Emplacement (Cratères / Hauteur)")]
    [Tooltip("Règle de placement (Anywhere pour Europe ou Regolite Lune, InCratersOnly pour Glace Lune)")]
    public ResourcePlacementConstraint constraint = ResourcePlacementConstraint.Anywhere;

    [Tooltip("Plafond d'altitude pour considérer la zone comme un cratère (sur le terrain lunaire standard: ~6.8m)")]
    public float craterMaxAltitude = 6.8f;

    [Tooltip("Vérifier si le point est dans une cuvette / dépression par rapport au terrain environnant")]
    public bool checkLocalConcavity = true;

    [Tooltip("Dépression minimale requise (altitude moyenne environnante - altitude centre)")]
    public float minDepressionDepth = 1.5f;

    [Tooltip("Altitude minimale (utilisé avec SpecificHeightRange)")]
    public float minAltitude = 0f;

    [Tooltip("Altitude maximale (utilisé avec SpecificHeightRange)")]
    public float maxAltitude = 20f;

    [Header("Pente et Orientation")]
    [Tooltip("Pente maximale du terrain tolérée (en degrés, évite les falaises abruptes)")]
    [Range(5f, 60f)]
    public float maxSlopeAngle = 32f;

    [Tooltip("Alignement avec la normale du sol (0 = vertical, 1 = collé à la pente)")]
    [Range(0f, 1f)]
    public float normalAlignment = 0.35f;

    [Tooltip("Enfoncement dans le sol pour éviter les roches flottantes (en mètres)")]
    public float groundSinkOffset = 0.25f;

    [Tooltip("Distance minimale entre deux ressources de ce type")]
    public float minDistanceBetweenNodes = 7f;
}

[ExecuteInEditMode]
public class EnvironmentalResourceManager : MonoBehaviour
{
    private static EnvironmentalResourceManager instance;
    public static EnvironmentalResourceManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<EnvironmentalResourceManager>();
            }
            return instance;
        }
    }

    [Header("Planète / Preset")]
    [Tooltip("Preset d'environnement céleste actif")]
    public PlanetPresetType currentPlanet = PlanetPresetType.Moon_Luna;

    [Header("Configuration des Ressources")]
    [Tooltip("Règles de génération pour chaque ressource environnementale")]
    public List<EnvironmentalResourceRule> resourceRules = new List<EnvironmentalResourceRule>();

    [Header("Références Scène")]
    [Tooltip("Terrain cible (détecté automatiquement si null)")]
    public Terrain targetTerrain;

    [Tooltip("Conteneur parent des ressources (trouvé ou créé automatiquement si null)")]
    public Transform spawnContainer;

    [Tooltip("Transform du joueur pour éviter de faire spawner des roches sur son point d'apparition")]
    public Transform playerTransform;

    [Tooltip("Rayon d'exclusion autour du joueur / base (mètres)")]
    public float playerExclusionRadius = 20f;

    [Header("Cratères Spécifiques (Optionnel)")]
    [Tooltip("Marqueurs de cratères manuels si souhaité. Si vide, la détection topographique automatique est utilisée.")]
    public List<Transform> customCraterMarkers = new List<Transform>();
    public float customCraterRadius = 35f;

    [Header("Options de Génération")]
    [Tooltip("Générer automatiquement au lancement si aucun objet n'est présent")]
    public bool generateOnStartIfEmpty = false;

    [Tooltip("Graine aléatoire (0 = aléatoire à chaque clic/exécution)")]
    public int randomSeed = 0;

    [Tooltip("Marge depuis les bords du terrain (mètres)")]
    public float terrainEdgeMargin = 60f;

    private void Awake()
    {
        if (instance == null) instance = this;

        if (Application.isPlaying && generateOnStartIfEmpty)
        {
            if (spawnContainer == null || spawnContainer.childCount == 0)
            {
                GenerateAllResources();
            }
        }
    }

    private void OnValidate()
    {
        if (resourceRules == null || resourceRules.Count == 0)
        {
            InitializeDefaultMoonRules();
        }
    }

    /// <summary>
    /// Initialise les règles par défaut pour la Lune :
    /// - Régolite : distribuée sur les plaines (Anywhere, scale 25-60)
    /// - RockIce : restreinte strictement aux cratères (InCratersOnly, scale 25-60)
    /// </summary>
    [ContextMenu("Preset : Lune (Moon)")]
    public void ApplyPresetMoon()
    {
        currentPlanet = PlanetPresetType.Moon_Luna;
        resourceRules.Clear();

        GameObject regPrefab = null;
        GameObject icePrefab = null;

#if UNITY_EDITOR
        regPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/RegoliteRock.prefab");
        icePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/RockIce.prefab");
#endif

        // Règle 1 : Régolite (partout sur la surface)
        resourceRules.Add(new EnvironmentalResourceRule
        {
            resourceName = "Régolite Lunaire",
            prefab = regPrefab,
            subFolderName = "Regolith_Nodes",
            spawnCount = 40,
            minScale = 25f,
            maxScale = 60f,
            slightNonUniformVariation = true,
            variationAmount = 0.08f,
            constraint = ResourcePlacementConstraint.Anywhere,
            maxSlopeAngle = 35f,
            normalAlignment = 0.35f,
            groundSinkOffset = 0.25f,
            minDistanceBetweenNodes = 8f
        });

        // Règle 2 : Glace d'eau (uniquement dans les cratères froids / pièges thermiques)
        resourceRules.Add(new EnvironmentalResourceRule
        {
            resourceName = "Glace de Cratère (Rock Ice)",
            prefab = icePrefab,
            subFolderName = "RockIce_Nodes",
            spawnCount = 30,
            minScale = 25f,
            maxScale = 60f,
            slightNonUniformVariation = true,
            variationAmount = 0.08f,
            constraint = ResourcePlacementConstraint.InCratersOnly,
            craterMaxAltitude = 6.8f,
            checkLocalConcavity = true,
            minDepressionDepth = 1.5f,
            maxSlopeAngle = 28f,
            normalAlignment = 0.3f,
            groundSinkOffset = 0.3f,
            minDistanceBetweenNodes = 7f
        });

        Debug.Log("[EnvironmentalResourceManager] Preset 'Lune' appliqué avec succès (Glace restreinte aux cratères).");
    }

    /// <summary>
    /// Initialise les règles pour Europe (satellite de Jupiter recouvert d'une croûte de glace) :
    /// - RockIce : partout sur la surface (Anywhere, scale 25-60)
    /// - Minéraux / Sels d'impact : partout ou en cratères
    /// </summary>
    [ContextMenu("Preset : Europe (Europa)")]
    public void ApplyPresetEuropa()
    {
        currentPlanet = PlanetPresetType.Europa;
        resourceRules.Clear();

        GameObject regPrefab = null;
        GameObject icePrefab = null;

#if UNITY_EDITOR
        regPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/RegoliteRock.prefab");
        icePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/RockIce.prefab");
#endif

        // Règle 1 : Glace d'eau partout sur la croûte d'Europe (Anywhere!)
        resourceRules.Add(new EnvironmentalResourceRule
        {
            resourceName = "Blocs de Glace d'Europe",
            prefab = icePrefab,
            subFolderName = "Ice_Nodes",
            spawnCount = 65,
            minScale = 25f,
            maxScale = 60f,
            slightNonUniformVariation = true,
            variationAmount = 0.1f,
            constraint = ResourcePlacementConstraint.Anywhere, // Non restreint aux cratères !
            maxSlopeAngle = 35f,
            normalAlignment = 0.35f,
            groundSinkOffset = 0.25f,
            minDistanceBetweenNodes = 7f
        });

        // Règle 2 : Dépôts minéraux / sels
        resourceRules.Add(new EnvironmentalResourceRule
        {
            resourceName = "Dépôts Minéraux / Sels",
            prefab = regPrefab,
            subFolderName = "Mineral_Nodes",
            spawnCount = 20,
            minScale = 25f,
            maxScale = 55f,
            slightNonUniformVariation = true,
            variationAmount = 0.08f,
            constraint = ResourcePlacementConstraint.Anywhere,
            maxSlopeAngle = 30f,
            normalAlignment = 0.3f,
            groundSinkOffset = 0.25f,
            minDistanceBetweenNodes = 10f
        });

        Debug.Log("[EnvironmentalResourceManager] Preset 'Europe' appliqué avec succès (Glace présente partout sur la surface).");
    }

    /// <summary>
    /// Initialise les règles pour Mars :
    /// - Basalte / Régolite martienne partout
    /// - Glace d'eau / carbo-glace en cratères ou dépressions
    /// </summary>
    [ContextMenu("Preset : Mars")]
    public void ApplyPresetMars()
    {
        currentPlanet = PlanetPresetType.Mars;
        resourceRules.Clear();

        GameObject regPrefab = null;
        GameObject icePrefab = null;

#if UNITY_EDITOR
        regPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/RegoliteRock.prefab");
        icePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Item/RockIce.prefab");
#endif

        resourceRules.Add(new EnvironmentalResourceRule
        {
            resourceName = "Régolite / Basalte Martien",
            prefab = regPrefab,
            subFolderName = "MarsRegolith_Nodes",
            spawnCount = 50,
            minScale = 25f,
            maxScale = 60f,
            constraint = ResourcePlacementConstraint.Anywhere,
            maxSlopeAngle = 35f,
            minDistanceBetweenNodes = 8f
        });

        resourceRules.Add(new EnvironmentalResourceRule
        {
            resourceName = "Glace Polaire / Cratère Martien",
            prefab = icePrefab,
            subFolderName = "MarsIce_Nodes",
            spawnCount = 20,
            minScale = 25f,
            maxScale = 60f,
            constraint = ResourcePlacementConstraint.InCratersOnly,
            craterMaxAltitude = 6.5f,
            checkLocalConcavity = true,
            minDepressionDepth = 1.5f,
            maxSlopeAngle = 28f,
            minDistanceBetweenNodes = 8f
        });

        Debug.Log("[EnvironmentalResourceManager] Preset 'Mars' appliqué.");
    }

    private void InitializeDefaultMoonRules()
    {
        ApplyPresetMoon();
    }

    /// <summary>
    /// Génère et place automatiquement toutes les ressources configurées.
    /// </summary>
    [ContextMenu("Générer les Ressources")]
    public void GenerateAllResources()
    {
        EnsureReferences();

        if (targetTerrain == null)
        {
            Debug.LogError("[EnvironmentalResourceManager] Aucun Terrain trouvé pour générer les ressources !");
            return;
        }

        if (randomSeed != 0)
        {
            Random.InitState(randomSeed);
        }

        ClearGeneratedResources();

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        float minX = terrainPos.x + terrainEdgeMargin;
        float maxX = terrainPos.x + terrainSize.x - terrainEdgeMargin;
        float minZ = terrainPos.z + terrainEdgeMargin;
        float maxZ = terrainPos.z + terrainSize.z - terrainEdgeMargin;

        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
        bool hasPlayer = playerTransform != null;

        List<Vector3> allPlacedPositions = new List<Vector3>();
        int totalSpawned = 0;

        foreach (var rule in resourceRules)
        {
            if (rule.prefab == null)
            {
                Debug.LogWarning($"[EnvironmentalResourceManager] Prefab non assigné pour la règle '{rule.resourceName}'. Passage ignoré.");
                continue;
            }

            Transform subParent = GetOrCreateSubFolder(rule.subFolderName);
            List<Vector3> rulePositions = new List<Vector3>();

            int count = 0;
            int maxAttempts = rule.spawnCount * 120;
            int attempts = 0;

            while (count < rule.spawnCount && attempts < maxAttempts)
            {
                attempts++;

                float sampleX = Random.Range(minX, maxX);
                float sampleZ = Random.Range(minZ, maxZ);
                Vector3 checkPos = new Vector3(sampleX, 0f, sampleZ);

                // 1. Exclusion joueur / base initiale
                if (hasPlayer)
                {
                    float distToPlayer = Vector2.Distance(new Vector2(sampleX, sampleZ), new Vector2(playerPos.x, playerPos.z));
                    if (distToPlayer < playerExclusionRadius)
                    {
                        continue;
                    }
                }

                // 2. Altitude terrain
                float terrainY = targetTerrain.SampleHeight(checkPos) + terrainPos.y;
                checkPos.y = terrainY;

                // 3. Pente du terrain
                float normX = (sampleX - terrainPos.x) / terrainSize.x;
                float normZ = (sampleZ - terrainPos.z) / terrainSize.z;
                Vector3 normal = targetTerrain.terrainData.GetInterpolatedNormal(normX, normZ);
                float slopeAngle = Vector3.Angle(normal, Vector3.up);

                if (slopeAngle > rule.maxSlopeAngle)
                {
                    continue;
                }

                // 4. Vérification de la contrainte (Cratère / Plaine / Altitude)
                if (!IsConstraintSatisfied(rule, checkPos, terrainY))
                {
                    continue;
                }

                // 5. Distance minimale entre objets
                bool tooClose = false;
                foreach (var pos in rulePositions)
                {
                    if (Vector3.Distance(pos, checkPos) < rule.minDistanceBetweenNodes)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Emplacement valide trouvé !
                SpawnResourceInstance(rule, subParent, checkPos, normal, terrainY);
                rulePositions.Add(checkPos);
                allPlacedPositions.Add(checkPos);
                count++;
                totalSpawned++;
            }

            Debug.Log($"[EnvironmentalResourceManager] {count}/{rule.spawnCount} '{rule.resourceName}' placés avec succès sous '{rule.subFolderName}' ({attempts} tentatives).");
        }

        Debug.Log($"[EnvironmentalResourceManager] Placement terminé : {totalSpawned} ressources environnementales créées.");
    }

    private bool IsConstraintSatisfied(EnvironmentalResourceRule rule, Vector3 pos, float altitude)
    {
        switch (rule.constraint)
        {
            case ResourcePlacementConstraint.Anywhere:
                return true;

            case ResourcePlacementConstraint.InCratersOnly:
                return IsInsideCrater(rule, pos, altitude);

            case ResourcePlacementConstraint.OutsideCratersOnly:
                return !IsInsideCrater(rule, pos, altitude);

            case ResourcePlacementConstraint.SpecificHeightRange:
                return altitude >= rule.minAltitude && altitude <= rule.maxAltitude;

            default:
                return true;
        }
    }

    /// <summary>
    /// Détecte si un point est à l'intérieur d'un cratère soit via marqueur manuel, soit via analyse topographique.
    /// </summary>
    public bool IsInsideCrater(EnvironmentalResourceRule rule, Vector3 pos, float altitude)
    {
        // 1. Si des marqueurs manuels sont définis, vérifier si on est dans le rayon d'un cratère
        if (customCraterMarkers != null && customCraterMarkers.Count > 0)
        {
            foreach (var marker in customCraterMarkers)
            {
                if (marker == null) continue;
                float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(marker.position.x, marker.position.z));
                if (dist <= customCraterRadius) return true;
            }
        }

        // 2. Analyse topographique : seuil d'altitude max
        if (altitude > rule.craterMaxAltitude)
        {
            return false;
        }

        // 3. Analyse de concavité (cuvette) : le centre doit être plus bas que les bords
        if (rule.checkLocalConcavity && targetTerrain != null)
        {
            float checkDist = 25f;
            float hN = targetTerrain.SampleHeight(pos + Vector3.forward * checkDist);
            float hS = targetTerrain.SampleHeight(pos + Vector3.back * checkDist);
            float hE = targetTerrain.SampleHeight(pos + Vector3.right * checkDist);
            float hW = targetTerrain.SampleHeight(pos + Vector3.left * checkDist);

            float avgSurrounding = (hN + hS + hE + hW) * 0.25f;
            float depth = avgSurrounding - altitude;

            if (depth < rule.minDepressionDepth)
            {
                return false;
            }
        }

        return true;
    }

    private void SpawnResourceInstance(EnvironmentalResourceRule rule, Transform parent, Vector3 groundPos, Vector3 terrainNormal, float groundHeight)
    {
        // Calcul de l'échelle entre minScale et maxScale (25 à 60)
        float baseScale = Random.Range(rule.minScale, rule.maxScale);
        Vector3 finalScale;

        if (rule.slightNonUniformVariation)
        {
            float v = rule.variationAmount;
            float sx = baseScale * Random.Range(1f - v, 1f + v);
            float sy = baseScale * Random.Range(1f - v, 1f + v);
            float sz = baseScale * Random.Range(1f - v, 1f + v);
            finalScale = new Vector3(sx, sy, sz);
        }
        else
        {
            finalScale = Vector3.one * baseScale;
        }

        // Calcul de la position avec enfoncement pour assise naturelle
        Vector3 finalPos = new Vector3(groundPos.x, groundHeight - rule.groundSinkOffset, groundPos.z);

        // Rotation aléatoire en lacet (Yaw) + inclinaison douce avec la pente
        Quaternion randomYaw = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
        Quaternion slopeTilt = Quaternion.FromToRotation(Vector3.up, terrainNormal);
        Quaternion finalRot = Quaternion.Slerp(Quaternion.identity, slopeTilt, rule.normalAlignment) * randomYaw;

        GameObject instance;
#if UNITY_EDITOR
        instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(rule.prefab, parent);
        if (instance == null)
        {
            instance = Instantiate(rule.prefab, parent);
        }
        UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Generate Resource Node");
#else
        instance = Instantiate(rule.prefab, parent);
#endif

        instance.transform.position = finalPos;
        instance.transform.rotation = finalRot;
        instance.transform.localScale = finalScale;

        string cleanName = rule.prefab.name + $"_{Mathf.RoundToInt(groundPos.x)}_{Mathf.RoundToInt(groundPos.z)}";
        instance.name = cleanName;
    }

    /// <summary>
    /// Supprime toutes les ressources instanciées.
    /// </summary>
    [ContextMenu("Supprimer les Ressources")]
    public void ClearGeneratedResources()
    {
        EnsureReferences();

        if (spawnContainer == null) return;

        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = spawnContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = spawnContainer.GetChild(i);
            toDestroy.Add(child.gameObject);
        }

        foreach (var go in toDestroy)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(go);
#else
            Destroy(go);
#endif
        }

        Debug.Log("[EnvironmentalResourceManager] Conteneur de ressources vidé.");
    }

    private Transform GetOrCreateSubFolder(string folderName)
    {
        EnsureReferences();

        if (string.IsNullOrEmpty(folderName)) return spawnContainer;

        Transform sub = spawnContainer.Find(folderName);
        if (sub == null)
        {
            GameObject subObj = new GameObject(folderName);
            subObj.transform.SetParent(spawnContainer, false);
            subObj.transform.localPosition = Vector3.zero;
            sub = subObj.transform;
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(subObj, "Create Resource SubFolder");
#endif
        }
        return sub;
    }

    private void EnsureReferences()
    {
        if (targetTerrain == null)
        {
            targetTerrain = Terrain.activeTerrain;
        }

        if (playerTransform == null)
        {
            GameObject playerGo = GameObject.Find("Player");
            if (playerGo != null) playerTransform = playerGo.transform;
        }

        if (spawnContainer == null)
        {
            GameObject containerGo = GameObject.Find("Environment_Resources");
            if (containerGo == null)
            {
                containerGo = new GameObject("Environment_Resources");
                containerGo.transform.position = Vector3.zero;
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterCreatedObjectUndo(containerGo, "Create Environment_Resources Container");
#endif
            }
            spawnContainer = containerGo.transform;
        }
    }
}
