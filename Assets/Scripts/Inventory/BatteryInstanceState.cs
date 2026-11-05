using System;
using UnityEngine;

namespace InventoryFramework
{
    [Serializable]
    public sealed class BatteryInstanceState
    {
        [SerializeField] private string _id;
        [SerializeField] private float _capacity;
        [SerializeField] private float _charge;
        public string Id => _id;
        public float Capacity => _capacity;
        public float Charge => _charge;
        public int Percent => _capacity > 0f ? Mathf.RoundToInt(100f * _charge / _capacity) : 0;
        public BatteryInstanceState(float capacity, float charge)
        {
            _id = Guid.NewGuid().ToString("N");
            _capacity = Mathf.Max(0.001f, capacity);
            _charge = Mathf.Clamp(charge, 0f, _capacity);
        }
        public float ConsumeWork(float seconds, float rate)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f || _charge <= 0f || rate <= 0f) return 0f;
            float paid = Mathf.Min(seconds, _charge / rate);
            _charge = Mathf.Max(0f, _charge - paid * rate);
            return paid;
        }
    }
}
