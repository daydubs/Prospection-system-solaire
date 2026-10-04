using UnityEngine;

namespace InventoryFramework
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        public int id;
        public string itemName;
        public string description;
        public Sprite icon;
        public int maxStack = 1;
        public GameObject model;
        public double basePrice;

        [Tooltip("Couleur représentant cet item, par exemple pour le rendu des objets physiques récoltés.")]
        public Color itemColor = Color.white;
    }
}
