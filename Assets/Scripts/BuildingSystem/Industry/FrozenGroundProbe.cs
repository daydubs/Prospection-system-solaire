using UnityEngine;

namespace BuildingSystem.Industry
{
    [DisallowMultipleComponent]
    public class FrozenGroundProbe : MonoBehaviour
    {
        [Header("Sol glacé")]
        [SerializeField] private TerrainLayer _iceLayer;
        [SerializeField] private Transform _extractionPoint;
        [SerializeField, Range(0.01f, 1f)] private float _minimumWeight = 0.5f;
        [SerializeField, Min(0.05f)] private float _checkInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float _rayHeight = 0.5f;
        [SerializeField, Min(0.01f)] private float _rayDistance = 1.5f;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private float _nextCheck;
        private Vector3 _lastPoint;
        public bool IsFrozen { get; private set; }
        public float IceWeight { get; private set; }
        public string Status { get; private set; } = "Aucun terrain";

        private void OnEnable() { _nextCheck = 0f; Refresh(); }
        private void OnDisable() { IsFrozen = false; Status = "Sonde inactive"; }

        public void RefreshIfDue()
        {
            Vector3 point = _extractionPoint != null ? _extractionPoint.position : transform.position;
            if (Time.time >= _nextCheck || point != _lastPoint) Refresh();
        }

        public void Refresh()
        {
            IsFrozen = false;
            IceWeight = 0f;
            _nextCheck = Time.time + Mathf.Max(0.05f, _checkInterval);
            if (!isActiveAndEnabled) { Status = "Sonde inactive"; return; }
            if (_iceLayer == null || _extractionPoint == null || float.IsNaN(_minimumWeight) ||
                _minimumWeight <= 0f || _minimumWeight > 1f || _rayHeight <= 0f || _rayDistance <= 0f)
            { Status = "Configuration du sol incomplète"; return; }
            _lastPoint = _extractionPoint.position;
            int count = Physics.RaycastNonAlloc(_lastPoint + Vector3.up * _rayHeight, Vector3.down,
                _hits, _rayDistance, ~0, QueryTriggerInteraction.Ignore);
            // Saturation can hide a closer obstacle: fail closed rather than extract through it.
            if (count == _hits.Length) { Status = "Support du sol indéterminé"; return; }
            Collider support = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider == null || collider.transform.IsChildOf(transform)) continue;
                if (_hits[i].distance >= nearest) continue;
                nearest = _hits[i].distance;
                support = collider;
            }
            if (!(support is TerrainCollider)) { Status = "Aucun terrain sous le réacteur"; return; }
            Terrain terrain = support.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null) { Status = "Aucun terrain"; return; }
            TerrainData data = terrain.terrainData;
            Vector3 local = terrain.transform.InverseTransformPoint(_lastPoint);
            if (data.size.x <= 0f || data.size.z <= 0f || local.x < 0f || local.z < 0f ||
                local.x > data.size.x || local.z > data.size.z)
            { Status = "Hors du terrain"; return; }
            TerrainLayer[] layers = data.terrainLayers;
            int layerIndex = System.Array.IndexOf(layers, _iceLayer);
            if (layerIndex < 0 || layerIndex >= data.alphamapLayers)
            { Status = "Couche de glace absente"; return; }
            // Read at most four alpha pixels. Re-resolve layers every check so reordering is safe.
            float x = local.x / data.size.x * (data.alphamapWidth - 1);
            float z = local.z / data.size.z * (data.alphamapHeight - 1);
            int x0 = Mathf.FloorToInt(x), z0 = Mathf.FloorToInt(z);
            int width = Mathf.Min(2, data.alphamapWidth - x0);
            int height = Mathf.Min(2, data.alphamapHeight - z0);
            float[,,] alpha = data.GetAlphamaps(x0, z0, width, height);
            float south = Mathf.Lerp(alpha[0, 0, layerIndex], alpha[0, width - 1, layerIndex], x - x0);
            float north = Mathf.Lerp(alpha[height - 1, 0, layerIndex], alpha[height - 1, width - 1, layerIndex], x - x0);
            IceWeight = Mathf.Lerp(south, north, z - z0);
            IsFrozen = IceWeight >= _minimumWeight;
            Status = IsFrozen ? "Sol glacé" : "Sol non glacé";
        }
    }
}
