using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InventoryFramework.EditorTools
{
    public static class ContentValidationUtility
    {
        public static Item[] Items() => AssetDatabase.FindAssets("t:Item")
            .Select(g => AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null).OrderBy(i => AssetDatabase.GetAssetPath(i), StringComparer.Ordinal).ToArray();

        public static int NextId()
        {
            var ids = new HashSet<int>(Items().Select(i => i.id));
            int next = Math.Max(1, ids.Count == 0 ? 1 : checked(ids.Max() + 1));
            while (ids.Contains(next)) next = checked(next + 1);
            return next;
        }

        public static string Audit()
        {
            var lines = new List<string>();
            var items = Items();
            foreach (var group in items.GroupBy(i => i.id).Where(g => g.Key <= 0 || g.Count() > 1))
                lines.Add("ID invalide/dupliqué " + group.Key + " : " + string.Join(", ", group.Select(AssetDatabase.GetAssetPath)));
            foreach (var i in items)
            {
                if (i.icon == null) lines.Add("Icône manquante : " + AssetDatabase.GetAssetPath(i));
                if (i.moduleData != null && i.moduleData.Item != i) lines.Add("Propriétaire incohérent : " + AssetDatabase.GetAssetPath(i));
                if (i.IsConstructible && (i.moduleData.finalPrefab == null || i.moduleData.ghostPrefab == null))
                    lines.Add("Prefabs de construction incomplets : " + AssetDatabase.GetAssetPath(i));
            }
            return lines.Count == 0 ? "Catalogue valide : " + items.Length + " Items, IDs uniques et positifs." : string.Join("\n", lines);
        }

        public static List<string> Validate(Item draft, Item original, BaseModuleData module, bool constructible,
            CraftingSystem.CraftingRecipe recipe, bool fabricable, bool hasDestination)
        {
            var errors = new List<string>();
            if (draft == null || string.IsNullOrWhiteSpace(draft.itemName)) errors.Add("Nom requis.");
            if (draft == null) return errors;
            if (draft.maxStack < 1) errors.Add("Stack >= 1 requis.");
            if (draft.IsBattery && draft.maxStack != 1) errors.Add("Une batterie ne peut pas être empilée.");
            if (draft.icon == null) errors.Add("Une icône Sprite est requise pour enregistrer ce contenu.");
            if (original != null && (original.id <= 0 || Items().Any(i => i != original && i.id == original.id)))
                errors.Add("ID invalide ou dupliqué : utiliser la réparation ciblée de l'audit.");
            if (draft.model != null && !EditorUtility.IsPersistent(draft.model)) errors.Add("Le modèle inventaire doit être un asset, pas un objet de scène.");
            if (constructible)
            {
                if (module == null || module.finalPrefab == null || module.ghostPrefab == null) errors.Add("Prefab final et ghost requis.");
                else
                {
                    if (!EditorUtility.IsPersistent(module.finalPrefab) || !EditorUtility.IsPersistent(module.ghostPrefab)) errors.Add("Les prefabs doivent être des assets persistants.");
                    if (module.Item != null && module.Item != original) errors.Add("Ce module appartient déjà à un autre Item.");
                    if (module.baseConstructionTime <= 0) errors.Add("Temps de construction positif requis.");
                    if (module.resourceCosts != null && module.resourceCosts.Any(c => c == null || c.item == null || c.amount < 1)) errors.Add("Coûts de construction invalides.");
                }
            }
            if (fabricable)
            {
                if (recipe == null || recipe.outputAmount < 1) errors.Add("Quantité de fabrication >= 1 requise.");
                if (recipe != null && recipe.ingredients.Any(c => c == null || c.item == null || c.amount < 1)) errors.Add("Ingrédients invalides.");
                if (!hasDestination) errors.Add("Choisir une station ou une liste globale pour inclure la recette dans le jeu/build.");
            }
            return errors;
        }
    }
}
