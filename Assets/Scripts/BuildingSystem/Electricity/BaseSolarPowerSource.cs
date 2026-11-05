using UnityEngine;
using BuildingSystem.Cables;
namespace BuildingSystem.Electricity
{
    public class BaseSolarPowerSource : MonoBehaviour
    {
        [SerializeField] private ElectricConnectorSocket _outputSocket;
        [Tooltip("Production solaire nominale ; modèle jour/nuit hors périmètre.")]
        [SerializeField] private bool _generating = true;
        private void OnEnable() => UpdatePower();
        private void Update() => UpdatePower();
        private void UpdatePower() { if (_outputSocket != null) _outputSocket.SetPowered(_generating); }
        private void OnDisable() { if (_outputSocket != null) _outputSocket.SetPowered(false); }
    }
}
