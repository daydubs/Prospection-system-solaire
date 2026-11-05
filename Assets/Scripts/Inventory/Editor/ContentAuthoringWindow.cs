using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CraftingSystem;

namespace InventoryFramework.EditorTools
{
    public class ContentAuthoringWindow : EditorWindow
    {
        [SerializeField] private Item _original;
        private Item _draft;
        private BaseModuleData _moduleDraft;
        private CraftingRecipe _recipeDraft;
        private BaseModuleData _existingModule;
        private CraftingRecipe _existingRecipe;
        private LanderCraftingStation _station;
        private CraftingUIManager _global;
        private SerializedObject _itemSO, _moduleSO, _recipeSO;
        private Vector2 _scroll;
        private bool _fabricable, _recipeChoiceRequired;
        private Material _ghostMaterial;
        private string _diagnostics = string.Empty;

        [MenuItem("Tools/Contenu/Items et modules")]
        public static void Open() => GetWindow<ContentAuthoringWindow>("Items et modules");

        public static void Open(Item item)
        {
            var window = GetWindow<ContentAuthoringWindow>("Items et modules");
            window.Load(item);
        }

        private void OnEnable()
        {
            minSize = new Vector2(480, 550);
            Load(_original);
            _ghostMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Base/Ghost/GeneralGhost.mat");
        }

        private void OnDisable() => Release();

        private void Release()
        {
            if (_draft != null) DestroyImmediate(_draft);
            if (_moduleDraft != null) DestroyImmediate(_moduleDraft);
            if (_recipeDraft != null) DestroyImmediate(_recipeDraft);
        }

        private void Load(Item item)
        {
            Release(); _original = item;
            _draft = item != null ? Instantiate(item) : ScriptableObject.CreateInstance<Item>();
            _draft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _itemSO = new SerializedObject(_draft);
            LoadModule(item != null ? item.moduleData : null);
            var recipes = item == null ? new CraftingRecipe[0] : AssetDatabase.FindAssets("t:CraftingRecipe")
                .Select(g => AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null && r.outputItem == item).ToArray();
            LoadRecipe(recipes.Length == 1 ? recipes[0] : null);
            _recipeChoiceRequired = recipes.Length > 1;
            _fabricable = recipes.Length > 0;
            _diagnostics = string.Empty;
        }

        private void LoadModule(BaseModuleData module)
        {
            if (_moduleDraft != null) DestroyImmediate(_moduleDraft);
            _existingModule = module;
            _moduleDraft = module != null ? Instantiate(module) : ScriptableObject.CreateInstance<BaseModuleData>();
            _moduleDraft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _moduleSO = new SerializedObject(_moduleDraft);
        }

        private void LoadRecipe(CraftingRecipe recipe)
        {
            if (_recipeDraft != null) DestroyImmediate(_recipeDraft);
            _existingRecipe = recipe;
            _recipeDraft = recipe != null ? Instantiate(recipe) : ScriptableObject.CreateInstance<CraftingRecipe>();
            _recipeDraft.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            _recipeSO = new SerializedObject(_recipeDraft);
        }

        private void Field(SerializedObject so, string name, string label)
            => EditorGUILayout.PropertyField(so.FindProperty(name), new GUIContent(label), true);

        private void OnGUI()
        {
            if (_draft == null) Load(_original);
            EditorGUILayout.LabelField("Contenu — Items et modules", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Nouvel Item")) Load(null);
            var selected = (Item)EditorGUILayout.ObjectField(_original, typeof(Item), false);
            if (selected != _original) Load(selected);
            EditorGUILayout.EndHorizontal();
            if (_original != null) EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(_original), EditorStyles.wordWrappedMiniLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _itemSO.Update(); _moduleSO.Update(); _recipeSO.Update();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Informations communes — saisies une fois", EditorStyles.boldLabel);
            Field(_itemSO, "itemName", "Nom"); Field(_itemSO, "description", "Description"); Field(_itemSO, "icon", "Icône");
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.IntField("ID stable", _original != null ? _original.id : 0);
            if (_original == null) EditorGUILayout.LabelField("ID attribué automatiquement à la création.", EditorStyles.miniLabel);
            Field(_itemSO, "maxStack", "Stack maximum"); Field(_itemSO, "basePrice", "Prix de base");
            Field(_itemSO, "itemColor", "Couleur"); Field(_itemSO, "model", "Modèle inventaire (optionnel)");
            Field(_itemSO, "_batteryDefinition", "Définition batterie (optionnelle)");
            if (_draft is OxygenBottleItem) Field(_itemSO, "maxOxygenCapacity", "Capacité oxygène");
            EditorGUILayout.EndVertical();
            Field(_itemSO, "_isConstructible", "Constructible");
            bool constructible = _itemSO.FindProperty("_isConstructible").boolValue;
            if (constructible || _existingModule != null)
            {
                EditorGUILayout.BeginVertical("box");
                var module = (BaseModuleData)EditorGUILayout.ObjectField("Données existantes", _existingModule, typeof(BaseModuleData), false);
                if (module != _existingModule) LoadModule(module);
                Field(_moduleSO, "finalPrefab", "Prefab final"); Field(_moduleSO, "ghostPrefab", "Prefab ghost");
                Field(_moduleSO, "groundPlacementOnly", "Terrain seulement"); Field(_moduleSO, "baseConstructionTime", "Temps de construction (s)");
                Field(_moduleSO, "resourceCosts", "Coûts de construction");
                _ghostMaterial = (Material)EditorGUILayout.ObjectField("Matériau ghost", _ghostMaterial, typeof(Material), false);
                if (GUILayout.Button("Créer un ghost géométrique dédié…"))
                {
                    _moduleSO.ApplyModifiedProperties();
                    string issue = GhostPrefabFactory.Review(_moduleDraft.finalPrefab);
                    if (!string.IsNullOrEmpty(issue)) _diagnostics = issue;
                    else
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Enregistrer le ghost", "Ghost_" + ContentAuthoringService.Stem(_draft.itemName), "prefab", "Le prefab final restera intact.");
                        if (!string.IsNullOrEmpty(path))
                        {
                            try { _moduleDraft.ghostPrefab = GhostPrefabFactory.Create(_moduleDraft.finalPrefab, _ghostMaterial, path); _moduleSO.Update(); _diagnostics = "Ghost créé : " + path + ". Il reste disponible si vous annulez l'édition de l'Item."; }
                            catch (Exception e) { _diagnostics = e.Message; }
                        }
                    }
                }
                if (!constructible) EditorGUILayout.HelpBox("Module conservé, option constructible désactivée. Aucun asset ne sera supprimé.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            _fabricable = EditorGUILayout.Toggle("Fabricable", _fabricable);
            if (_fabricable)
            {
                EditorGUILayout.BeginVertical("box");
                var recipe = (CraftingRecipe)EditorGUILayout.ObjectField("Recette existante", _existingRecipe, typeof(CraftingRecipe), false);
                if (recipe != _existingRecipe) { LoadRecipe(recipe); _recipeChoiceRequired = false; }
                if (_recipeChoiceRequired)
                {
                    EditorGUILayout.HelpBox("Plusieurs recettes produisent cet Item : sélectionner celle à modifier ou confirmer une recette supplémentaire.", MessageType.Warning);
                    if (GUILayout.Button("Créer une recette supplémentaire")) { LoadRecipe(null); _recipeChoiceRequired = false; }
                }
                Field(_recipeSO, "category", "Catégorie"); Field(_recipeSO, "outputAmount", "Quantité produite"); Field(_recipeSO, "ingredients", "Ingrédients d'impression");
                EditorGUILayout.LabelField("Surcharges facultatives — laisser vide pour utiliser l'Item", EditorStyles.wordWrappedMiniLabel);
                Field(_recipeSO, "recipeName", "Nom spécifique à la recette"); Field(_recipeSO, "description", "Instructions spécifiques");
                _station = (LanderCraftingStation)EditorGUILayout.ObjectField("Station destination", _station, typeof(LanderCraftingStation), true);
                _global = (CraftingUIManager)EditorGUILayout.ObjectField("Liste globale destination", _global, typeof(CraftingUIManager), true);
                EditorGUILayout.HelpBox("Choisir au moins une destination. Les listes existantes sont complétées, jamais remplacées. Coûts d'impression et de construction restent distincts.", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            else if (_existingRecipe != null) EditorGUILayout.HelpBox("Les recettes existantes restent intactes. Cette case ne retire pas leur disponibilité en jeu.", MessageType.Info);
            _itemSO.ApplyModifiedProperties(); _moduleSO.ApplyModifiedProperties(); _recipeSO.ApplyModifiedProperties();
            if (_draft.icon != null && _draft.icon.texture != null)
            {
                Rect r = GUILayoutUtility.GetRect(72, 72, GUILayout.Width(72));
                GUI.DrawTextureWithTexCoords(r, _draft.icon.texture, new Rect(_draft.icon.textureRect.x / _draft.icon.texture.width, _draft.icon.textureRect.y / _draft.icon.texture.height, _draft.icon.textureRect.width / _draft.icon.texture.width, _draft.icon.textureRect.height / _draft.icon.texture.height));
            }
            if (!string.IsNullOrEmpty(_diagnostics)) EditorGUILayout.HelpBox(_diagnostics, MessageType.Info);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Analyser"))
            {
                var errors = ContentValidationUtility.Validate(_draft, _original, _moduleDraft, constructible, _recipeDraft, _fabricable, _station != null || _global != null);
                _diagnostics = errors.Count > 0 ? string.Join("\n", errors) : (_original == null ? "Création : Item" : "Mise à jour en place : " + AssetDatabase.GetAssetPath(_original)) + (constructible ? " + données module" : string.Empty) + (_fabricable ? " + recette / destinations" : string.Empty) + ". GUID existants et IDs conservés.";
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || (_fabricable && _recipeChoiceRequired)))
            {
                if (GUILayout.Button(_original == null ? "Créer les assets liés" : "Enregistrer"))
                {
                    try
                    {
                        var saved = ContentAuthoringService.Save(_draft, _original, _moduleDraft, _existingModule, constructible, _recipeDraft, _existingRecipe, _fabricable, _station, _global);
                        Load(saved); _diagnostics = "Enregistré : " + AssetDatabase.GetAssetPath(saved); Selection.activeObject = saved;
                    }
                    catch (Exception e) { _diagnostics = e.Message; }
                }
            }
            if (GUILayout.Button("Annuler")) Load(_original);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Audit du catalogue")) _diagnostics = ContentValidationUtility.Audit();
            using (new EditorGUI.DisabledScope(_original == null || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Réparer l'ID sélectionné…")) { ContentAuthoringService.RepairId(_original); Load(_original); }
            EditorGUILayout.EndHorizontal();
        }
    }
}
