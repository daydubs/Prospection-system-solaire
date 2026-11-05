using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CraftingSystem;

namespace InventoryFramework.EditorTools
{
    public static class ContentAuthoringChecks
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("CHECK FAILED: " + message);
            Debug.Log("[ContentCheck] PASS " + message);
        }

        [MenuItem("Tools/Contenu/Vérifier le workflow (fixtures temporaires)")]
        public static void Run()
        {
            string prefix = "__ContentChecks_" + Guid.NewGuid().ToString("N");
            string fixturePath = "Assets/" + prefix + "Station.prefab";
            string ghostPath = "Assets/" + prefix + "Ghost.prefab";
            var before = new HashSet<string>(AssetDatabase.GetAllAssetPaths());
            var drafts = new List<UnityEngine.Object>();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var seed = ContentValidationUtility.Items().First(i => i.icon != null && !i.IsBattery && i.GetType() == typeof(Item));
                var item = ScriptableObject.CreateInstance<Item>(); drafts.Add(item); item.itemName = prefix + "Simple"; item.description = "Description unique"; item.icon = seed.icon;
                var module = ScriptableObject.CreateInstance<BaseModuleData>(); drafts.Add(module);
                var recipe = ScriptableObject.CreateInstance<CraftingRecipe>(); drafts.Add(recipe);
                int baseline = ContentValidationUtility.Items().Length;
                var first = ContentAuthoringService.Save(item, null, module, null, false, recipe, null, false, null, null);
                Assert(first.id > 0 && !first.IsConstructible, "Item simple : ID positif, non constructible");
                int stableId = first.id; string stableGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(first));
                var edit = UnityEngine.Object.Instantiate(first); drafts.Add(edit); edit.itemName = prefix + "Renamed";
                ContentAuthoringService.Save(edit, first, module, null, false, recipe, null, false, null, null);
                Assert(first.id == stableId && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(first)) == stableGuid && ContentValidationUtility.Items().Length == baseline + 1, "Édition : ID/GUID stables, aucun nouvel asset");
                var second = ContentAuthoringService.Save(item, null, module, null, false, recipe, null, false, null, null);
                Assert(second.id != first.id, "Même nom, IDs indépendants");
                int count = ContentValidationUtility.Items().Length;
                var invalid = UnityEngine.Object.Instantiate(item); drafts.Add(invalid); invalid.icon = null;
                bool rejected = false; try { ContentAuthoringService.Save(invalid, null, module, null, false, recipe, null, false, null, null); } catch (InvalidOperationException) { rejected = true; }
                Assert(rejected && ContentValidationUtility.Items().Length == count, "Validation invalide : aucune demi-création");
                var go = new GameObject("CheckStation"); go.SetActive(false); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, preview);
                go.AddComponent<BoxCollider>(); go.AddComponent<LanderCraftingStation>();
                var stationPrefab = PrefabUtility.SaveAsPrefabAsset(go, fixturePath);
                var station = stationPrefab.GetComponent<LanderCraftingStation>();
                var sourceModule = AssetDatabase.LoadAssetAtPath<BaseModuleData>("Assets/construction/Crusher.asset");
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Base/Ghost/GeneralGhost.mat");
                var ghost = GhostPrefabFactory.Create(sourceModule.finalPrefab, material, ghostPath);
                Assert(ghost.GetComponentsInChildren<MonoBehaviour>(true).All(c => c is ConstructibleGhost || c is ModuleSocket), "Ghost : aucune logique machine/énergie/câble");
                Assert(ghost.GetComponentsInChildren<Renderer>(true).Length == sourceModule.finalPrefab.GetComponentsInChildren<MeshRenderer>(true).Length, "Ghost : géométrie conservée");
                module.finalPrefab = sourceModule.finalPrefab; module.ghostPrefab = ghost; module.groundPlacementOnly = true;
                item.itemName = prefix + "Constructible";
                var built = ContentAuthoringService.Save(item, null, module, null, true, recipe, null, true, station, null);
                var outputRecipe = AssetDatabase.FindAssets("t:CraftingRecipe").Select(g => AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(g))).Single(r => r.outputItem == built);
                Assert(built.IsConstructible && built.moduleData.Item == built && outputRecipe.outputItem == built, "Création Item + module + recette : liens cohérents");
                Assert(built.moduleData.description == built.description && outputRecipe.GetDescription() == built.description && outputRecipe.GetIcon() == built.icon, "Informations communes issues de l'Item");
                AssetDatabase.ImportAsset(fixturePath, ImportAssetOptions.ForceSynchronousImport);
                station = AssetDatabase.LoadAssetAtPath<GameObject>(fixturePath).GetComponent<LanderCraftingStation>();
                Assert(station.stationRecipes.Contains(outputRecipe), "Recette persistante dans la station prefab après réimport");
                var builtDraft = UnityEngine.Object.Instantiate(built); drafts.Add(builtDraft);
                var moduleEdit = UnityEngine.Object.Instantiate(built.moduleData); drafts.Add(moduleEdit);
                var recipeEdit = UnityEngine.Object.Instantiate(outputRecipe); drafts.Add(recipeEdit);
                int assetCount = AssetDatabase.GetAllAssetPaths().Length;
                ContentAuthoringService.Save(builtDraft, built, moduleEdit, built.moduleData, true, recipeEdit, outputRecipe, true, station, null);
                Assert(AssetDatabase.GetAllAssetPaths().Length == assetCount && station.stationRecipes.Count(r => r == outputRecipe) == 1, "Deuxième Enregistrer : aucune duplication d'asset ou recette");
                ContentAuthoringService.Save(builtDraft, built, moduleEdit, built.moduleData, false, recipeEdit, outputRecipe, false, null, null);
                Assert(!built.IsConstructible && built.moduleData != null && outputRecipe != null, "Décocher : aucune destruction des données liées");
                var oxygenSource = AssetDatabase.LoadAssetAtPath<OxygenBottleItem>("Assets/Items/OxygenBottle.asset");
                var oxygenDraft = UnityEngine.Object.Instantiate(oxygenSource); drafts.Add(oxygenDraft); oxygenDraft.itemName = prefix + "Oxygen";
                var oxygen = ContentAuthoringService.Save(oxygenDraft, null, module, null, false, recipe, null, false, null, null) as OxygenBottleItem;
                Assert(oxygen != null && oxygen.maxOxygenCapacity == oxygenSource.maxOxygenCapacity, "Sous-type oxygène et capacité conservés");
                var batterySource = AssetDatabase.LoadAssetAtPath<Item>("Assets/Items/BaterieExterneItem.asset");
                var batteryDraft = UnityEngine.Object.Instantiate(batterySource); drafts.Add(batteryDraft); batteryDraft.itemName = prefix + "Battery";
                var battery = ContentAuthoringService.Save(batteryDraft, null, module, null, false, recipe, null, false, null, null);
                Assert(battery.BatteryDefinition == batterySource.BatteryDefinition && battery.maxStack == 1 && batterySource.id == 2603, "Définition batterie conservée, source ID 2603 inchangée");
                Debug.Log("[ContentCheck] ALL CHECKS PASSED");
            }
            finally
            {
                foreach (var o in drafts) if (o != null) UnityEngine.Object.DestroyImmediate(o);
                EditorSceneManager.ClosePreviewScene(preview);
                foreach (var p in AssetDatabase.GetAllAssetPaths().Where(p => !before.Contains(p) && p.Contains(prefix)).OrderByDescending(p => p.Length).ToArray()) AssetDatabase.DeleteAsset(p);
            }
        }
    }
}
