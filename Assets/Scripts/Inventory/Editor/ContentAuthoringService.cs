using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CraftingSystem;

namespace InventoryFramework.EditorTools
{
    public static class ContentAuthoringService
    {
        public static void SetLink(Item item, bool enabled, BaseModuleData module)
        {
            var so = new SerializedObject(item);
            so.FindProperty("_isConstructible").boolValue = enabled;
            so.FindProperty("_moduleData").objectReferenceValue = module;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (module != null)
            {
                var mo = new SerializedObject(module);
                mo.FindProperty("_item").objectReferenceValue = item;
                mo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static string Stem(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Item" : name.Trim();
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Item Save(Item draft, Item original, BaseModuleData moduleDraft, BaseModuleData existingModule,
            bool constructible, CraftingRecipe recipeDraft, CraftingRecipe existingRecipe, bool fabricable,
            LanderCraftingStation station, CraftingUIManager global)
        {
            var errors = ContentValidationUtility.Validate(draft, original, moduleDraft, constructible, recipeDraft, fabricable, station != null || global != null);
            if (existingModule != null && existingModule.Item != null && existingModule.Item != original) errors.Add("Module déjà possédé par un autre Item.");
            if (existingRecipe != null && existingRecipe.outputItem != original) errors.Add("La recette sélectionnée produit un autre Item.");
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var destinations = new UnityEngine.Object[] { station, global }.Where(o => o != null).ToArray();
            var scenes = destinations.OfType<Component>().Where(c => !EditorUtility.IsPersistent(c)).Select(c => c.gameObject.scene).Distinct().ToArray();
            foreach (var scene in scenes)
            {
                if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Enregistrer la scène destination avant de créer une recette.");
                if (scene.isDirty && !EditorUtility.DisplayDialog("Scène modifiée", "L'enregistrement de la recette sauvegardera aussi les modifications actuelles de " + scene.name + ". Continuer ?", "Enregistrer", "Annuler"))
                    throw new OperationCanceledException("Enregistrement annulé ; aucun asset modifié.");
            }
            var created = new List<string>();
            var snapshots = new Dictionary<UnityEngine.Object, string>();
            var disk = new Dictionary<string, byte[]>();
            foreach (var o in new UnityEngine.Object[] { original, existingModule, existingRecipe }.Concat(destinations).Where(o => o != null))
            {
                snapshots[o] = EditorJsonUtility.ToJson(o);
                string p = AssetDatabase.GetAssetPath(o);
                if (!string.IsNullOrEmpty(p) && File.Exists(p)) disk[p] = File.ReadAllBytes(p);
                if (o is Component c && !EditorUtility.IsPersistent(c) && File.Exists(c.gameObject.scene.path)) disk[c.gameObject.scene.path] = File.ReadAllBytes(c.gameObject.scene.path);
            }
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Contenu : enregistrer l'Item et les données liées");
            Item item = original;
            try
            {
                string stem = Stem(draft.itemName);
                if (item == null)
                {
                    Folder("Assets/Items");
                    item = (Item)ScriptableObject.CreateInstance(draft.GetType());
                    EditorUtility.CopySerialized(draft, item);
                    item.hideFlags = HideFlags.None;
                    item.name = stem;
                    item.id = ContentValidationUtility.NextId();
                    string p = AssetDatabase.GenerateUniqueAssetPath("Assets/Items/" + stem + ".asset");
                    AssetDatabase.CreateAsset(item, p); created.Add(p); Undo.RegisterCreatedObjectUndo(item, "Créer Item");
                }
                else
                {
                    Undo.RecordObject(item, "Modifier Item");
                    int id = item.id; string name = item.name;
                    EditorUtility.CopySerialized(draft, item);
                    item.hideFlags = HideFlags.None; item.id = id; item.name = name;
                }
                BaseModuleData module = existingModule;
                if (constructible || (existingModule != null && moduleDraft != null))
                {
                    if (module == null)
                    {
                        Folder("Assets/construction"); module = ScriptableObject.CreateInstance<BaseModuleData>();
                        string p = AssetDatabase.GenerateUniqueAssetPath("Assets/construction/" + stem + ".asset");
                        module.name = Path.GetFileNameWithoutExtension(p);
                        AssetDatabase.CreateAsset(module, p); created.Add(p); Undo.RegisterCreatedObjectUndo(module, "Créer module");
                    }
                    else Undo.RecordObject(module, "Modifier module");
                    string name = module.name; EditorUtility.CopySerialized(moduleDraft, module); module.name = name; module.hideFlags = HideFlags.None;
                    EditorUtility.SetDirty(module);
                }
                SetLink(item, constructible, module); EditorUtility.SetDirty(item);
                CraftingRecipe recipe = existingRecipe;
                if (fabricable)
                {
                    if (recipe == null)
                    {
                        Folder("Assets/Recipes"); recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
                        string p = AssetDatabase.GenerateUniqueAssetPath("Assets/Recipes/Recipe_" + stem + ".asset");
                        recipe.name = Path.GetFileNameWithoutExtension(p);
                        AssetDatabase.CreateAsset(recipe, p); created.Add(p); Undo.RegisterCreatedObjectUndo(recipe, "Créer recette");
                    }
                    else Undo.RecordObject(recipe, "Modifier recette");
                    string name = recipe.name; EditorUtility.CopySerialized(recipeDraft, recipe); recipe.name = name; recipe.hideFlags = HideFlags.None;
                    recipe.outputItem = item; EditorUtility.SetDirty(recipe);
                    if (station != null) Append(station, "stationRecipes", recipe);
                    if (global != null) Append(global, "globalRecipes", recipe);
                }
                if (module != null && module.DeconstructionRecipe == null && fabricable && recipe != null)
                {
                    var candidates = AssetDatabase.FindAssets("t:CraftingRecipe").Select(g => AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(g))).Where(r => r != null && r.outputItem == item).ToArray();
                    if (candidates.Length == 1)
                    {
                        var so = new SerializedObject(module);
                        so.FindProperty("_deconstructionRecipe").objectReferenceValue = recipe;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(module);
                    }
                }
                foreach (var o in new UnityEngine.Object[] { item, module, recipe }.Concat(destinations).Where(o => o != null && EditorUtility.IsPersistent(o)))
                    AssetDatabase.SaveAssetIfDirty(o);
                foreach (var scene in scenes)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Échec sauvegarde scène " + scene.path);
                }
                Undo.CollapseUndoOperations(undo);
                return item;
            }
            catch
            {
                foreach (var pair in snapshots) if (pair.Key != null) EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key);
                foreach (var p in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(p);
                foreach (var pair in disk) File.WriteAllBytes(pair.Key, pair.Value);
                AssetDatabase.Refresh();
                throw;
            }
        }

        private static void Append(UnityEngine.Object target, string field, CraftingRecipe recipe)
        {
            Undo.RecordObject(target, "Ajouter recette à la destination");
            var so = new SerializedObject(target); var list = so.FindProperty(field);
            for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == recipe) return;
            int index = list.arraySize; list.InsertArrayElementAtIndex(index); list.GetArrayElementAtIndex(index).objectReferenceValue = recipe;
            so.ApplyModifiedProperties(); EditorUtility.SetDirty(target);
            if (target is Component c && PrefabUtility.IsPartOfPrefabInstance(c)) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }

        public static void RepairId(Item item)
        {
            if (item == null) return;
            int id = ContentValidationUtility.NextId();
            if (!EditorUtility.DisplayDialog("Réparation ciblée", AssetDatabase.GetAssetPath(item) + "\nID " + item.id + " → " + id + "\nLes références d'assets restent inchangées. Vérifier vos usages d'IDs numériques avant de confirmer.", "Réparer", "Annuler")) return;
            string dir = "ContentMigrationBackup/ID_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"); Directory.CreateDirectory(dir);
            string p = AssetDatabase.GetAssetPath(item); File.Copy(p, dir + "/original.asset"); File.Copy(p + ".meta", dir + "/original.asset.meta");
            File.WriteAllText(dir + "/mapping.txt", p + " | " + AssetDatabase.AssetPathToGUID(p) + " | " + item.id + " -> " + id);
            Undo.RecordObject(item, "Réparer ID"); item.id = id; EditorUtility.SetDirty(item); AssetDatabase.SaveAssetIfDirty(item);
        }
    }
}
