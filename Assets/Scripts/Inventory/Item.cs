using UnityEngine;

namespace InventoryFramework
{
    public class Item : ScriptableObject
    {
        public int id;
        public string itemName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public int maxStack = 1;
        public GameObject model;
        public double basePrice;

        [Header("Construction")]
        [SerializeField] private bool _isConstructible;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("moduleData")]
        private BaseModuleData _moduleData;
        public BaseModuleData moduleData => _moduleData;
        public bool IsConstructible => _isConstructible && _moduleData != null;

        [SerializeField] private BatteryDefinition _batteryDefinition;
        public BatteryDefinition BatteryDefinition => _batteryDefinition;
        public bool IsBattery => _batteryDefinition != null;

        [Tooltip("Couleur représentant cet item, par exemple pour le rendu des objets physiques récoltés.")]
        public Color itemColor = Color.white;
    }
}
