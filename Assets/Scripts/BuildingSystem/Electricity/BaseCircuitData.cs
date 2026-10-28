using System;
using UnityEngine;

namespace BuildingSystem.Electricity
{
    public enum PowerGridMode
    {
        Normal,     // 100% nominal consumption on all active circuits
        Eco,        // -40% consumption on secondary circuits
        Emergency   // Non-essential circuits automatically cut to preserve life support
    }

    [Serializable]
    public class BaseCircuitData
    {
        [Tooltip("Nom affiché du circuit dans l'interface.")]
        public string circuitName;

        [Tooltip("Identifiant unique du circuit.")]
        public string circuitId;

        [Tooltip("Consommation nominale en kilowatt (kW).")]
        public float nominalConsumptionKW;

        [Tooltip("Si vrai, ce circuit est critique (ex: Support Vital, Scrubber). Ne sera pas délesté automatiquement en mode urgence.")]
        public bool isEssential;

        [Tooltip("État d'activation du circuit.")]
        public bool isActive = true;

        public BaseCircuitData(string id, string name, float consumptionKW, bool essential, bool active = true)
        {
            circuitId = id;
            circuitName = name;
            nominalConsumptionKW = consumptionKW;
            isEssential = essential;
            isActive = active;
        }

        public float GetCurrentConsumption(PowerGridMode mode)
        {
            if (!isActive) return 0f;

            if (mode == PowerGridMode.Emergency && !isEssential)
            {
                return 0f;
            }

            if (mode == PowerGridMode.Eco && !isEssential)
            {
                return nominalConsumptionKW * 0.6f; // 40% reduction
            }

            return nominalConsumptionKW;
        }
    }
}
