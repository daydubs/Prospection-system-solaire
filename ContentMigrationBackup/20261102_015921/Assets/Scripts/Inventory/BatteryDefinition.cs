using UnityEngine;
namespace InventoryFramework
{
    [CreateAssetMenu(menuName = "Inventory/Battery Definition")]
    public sealed class BatteryDefinition : ScriptableObject
    {
        [SerializeField, Min(0.001f)] private float _capacity = 100f;
        [SerializeField, Range(0f, 1f)] private float _initialFraction = 1f;
        public BatteryInstanceState CreateInstance() => new BatteryInstanceState(_capacity, _capacity * _initialFraction);
    }
}
