using System.Collections.Generic;
using UnityEngine;
using InventoryFramework;

namespace CraftingSystem
{
    [System.Serializable]
    public class RecipeIngredient
    {
        [Tooltip("L'item ressource nécessaire pour la fabrication.")]
        public Item item;

        [Tooltip("Quantité requise.")]
        [Min(1)]
        public int amount = 1;
    }

    /// <summary>
    /// Définition d'une recette de fabrication pour l'imprimante 3D du Lander.
    /// Peut être créée via clic droit -> Create -> Crafting -> Recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Informations Générales")]
        [Tooltip("Nom de la recette (si vide, utilise le nom de l'item produit).")]
        public string recipeName;

        [Tooltip("Catégorie de la recette (ex: Composants, Armatures, Outils).")]
        public string category = "Composants";

        [TextArea(2, 4)]
        [Tooltip("Description facultative ou instructions de fabrication.")]
        public string description;

        [Header("Résultat")]
        [Tooltip("Item fabriqué lors de l'exécution de la recette.")]
        public Item outputItem;

        [Tooltip("Quantité d'items produits par craft.")]
        [Min(1)]
        public int outputAmount = 1;

        [Header("Ingrédients")]
        [Tooltip("Liste des ressources requises pour fabriquer cet item.")]
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(recipeName)) return recipeName;
            if (outputItem != null && !string.IsNullOrEmpty(outputItem.itemName)) return outputItem.itemName;
            return name;
        }

        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(description)) return description;
            if (outputItem != null && !string.IsNullOrEmpty(outputItem.description)) return outputItem.description;
            return string.Empty;
        }

        public Sprite GetIcon()
        {
            if (outputItem != null && outputItem.icon != null) return outputItem.icon;
            return null;
        }
    }
}
