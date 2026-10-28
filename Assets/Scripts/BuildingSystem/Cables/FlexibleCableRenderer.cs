using UnityEngine;

namespace BuildingSystem.Cables
{
    /// <summary>
    /// Generates a procedural 3D cylindrical flexible cable/hose along a cubic Bézier curve with catenary gravity sag.
    /// Provides smooth orientation at sockets, realistic gravity sagging, and seamless texture tiling.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FlexibleCableRenderer : MonoBehaviour
    {
        [Header("Anchors & Transforms")]
        [Tooltip("Starting attachment point / socket.")]
        [SerializeField] private Transform startPoint;

        [Tooltip("Ending attachment point / socket.")]
        [SerializeField] private Transform endPoint;

        [Tooltip("Optional visual plug object at the start of the cable.")]
        [SerializeField] private Transform startPlug;

        [Tooltip("Optional visual plug object at the end of the cable.")]
        [SerializeField] private Transform endPlug;

        [Header("Cable Dimensions")]
        [Tooltip("Radius of the cable in meters.")]
        [Range(0.01f, 0.25f)]
        [SerializeField] private float cableRadius = 0.045f;

        [Tooltip("Base gravity sag / droop amount. Higher values produce more slack.")]
        [Range(0f, 2f)]
        [SerializeField] private float gravitySag = 0.35f;

        [Tooltip("Tangential stiffening factor at connection points. Controls how straight the cable exits the socket before drooping.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float tangentStiffness = 0.8f;

        [Header("Mesh Resolution")]
        [Tooltip("Number of longitudinal divisions along the length of the cable.")]
        [Range(8, 64)]
        [SerializeField] private int lengthSegments = 28;

        [Tooltip("Number of radial sides forming the tube cross-section.")]
        [Range(4, 16)]
        [SerializeField] private int radialSegments = 8;

        [Header("Texture Mapping")]
        [Tooltip("Texture tile repetition factor per meter along the cable length.")]
        [SerializeField] private float uvTilePerMeter = 3.0f;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _proceduralMesh;

        // Cached positions to avoid rebuilding when static
        private Vector3 _lastStartPos;
        private Vector3 _lastEndPos;
        private Quaternion _lastStartRot;
        private Quaternion _lastEndRot;

        public Transform StartPoint
        {
            get => startPoint;
            set { startPoint = value; RegenerateMesh(); }
        }

        public Transform EndPoint
        {
            get => endPoint;
            set { endPoint = value; RegenerateMesh(); }
        }

        public Transform StartPlug
        {
            get => startPlug;
            set => startPlug = value;
        }

        public Transform EndPlug
        {
            get => endPlug;
            set => endPlug = value;
        }

        public float CableRadius
        {
            get => cableRadius;
            set { cableRadius = value; RegenerateMesh(); }
        }

        public float GravitySag
        {
            get => gravitySag;
            set { gravitySag = value; RegenerateMesh(); }
        }

        private void Awake()
        {
            EnsureMeshComponents();
        }

        private void OnEnable()
        {
            EnsureMeshComponents();
            RegenerateMesh();
        }

        private void OnDestroy()
        {
            if (_proceduralMesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_proceduralMesh);
                else
                    DestroyImmediate(_proceduralMesh);
            }
        }

        private void LateUpdate()
        {
            if (startPoint == null || endPoint == null) return;

            // Only rebuild if endpoints or their rotations moved
            if (startPoint.position != _lastStartPos ||
                endPoint.position != _lastEndPos ||
                startPoint.rotation != _lastStartRot ||
                endPoint.rotation != _lastEndRot)
            {
                RegenerateMesh();
            }
        }

        private void EnsureMeshComponents()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();

            if (_proceduralMesh == null)
            {
                _proceduralMesh = new Mesh();
                _proceduralMesh.name = "FlexibleCable_ProceduralMesh";
                _meshFilter.sharedMesh = _proceduralMesh;
            }
        }

        /// <summary>
        /// Explicitly set cable endpoint world positions (e.g. during player dragging).
        /// </summary>
        public void SetEndpoints(Vector3 startPos, Vector3 startFwd, Vector3 endPos, Vector3 endFwd)
        {
            GenerateMeshInternal(startPos, startFwd, endPos, endFwd);
        }

        /// <summary>
        /// Rebuilds the 3D procedural tube mesh connecting startPoint and endPoint.
        /// </summary>
        [ContextMenu("Regenerate Cable Mesh")]
        public void RegenerateMesh()
        {
            if (startPoint == null || endPoint == null)
            {
                if (_proceduralMesh != null) _proceduralMesh.Clear();
                return;
            }

            _lastStartPos = startPoint.position;
            _lastEndPos = endPoint.position;
            _lastStartRot = startPoint.rotation;
            _lastEndRot = endPoint.rotation;

            // Auto-detect plugs from anchors if not explicitly linked
            if (startPlug == null && startPoint != null)
            {
                startPlug = startPoint.Find("ConnectorPlug");
            }
            if (endPlug == null && endPoint != null)
            {
                endPlug = endPoint.Find("ConnectorPlug");
            }

            // Position and orient plugs if assigned
            if (startPlug != null)
            {
                startPlug.position = startPoint.position;
                startPlug.rotation = Quaternion.LookRotation(startPoint.forward, startPoint.up);
            }

            if (endPlug != null)
            {
                endPlug.position = endPoint.position;
                endPlug.rotation = Quaternion.LookRotation(endPoint.forward, endPoint.up);
            }

            Vector3 pStart = GetCableExitPoint(startPoint, startPlug);
            Vector3 fwdStart = GetCableExitDirection(startPoint, startPlug);
            Vector3 pEnd = GetCableExitPoint(endPoint, endPlug);
            Vector3 fwdEnd = GetCableExitDirection(endPoint, endPlug);

            GenerateMeshInternal(pStart, fwdStart, pEnd, fwdEnd);
        }

        private Vector3 GetCableExitPoint(Transform anchor, Transform plug)
        {
            if (plug == null && anchor != null)
            {
                plug = anchor.Find("ConnectorPlug");
            }

            if (plug != null)
            {
                Transform exit = plug.Find("CableExit");
                if (exit != null)
                {
                    return exit.position;
                }
                return plug.position + plug.forward * 0.19f;
            }
            return anchor != null ? anchor.position : Vector3.zero;
        }

        private Vector3 GetCableExitDirection(Transform anchor, Transform plug)
        {
            if (plug == null && anchor != null)
            {
                plug = anchor.Find("ConnectorPlug");
            }

            if (plug != null)
            {
                Transform exit = plug.Find("CableExit");
                if (exit != null)
                {
                    return exit.forward;
                }
                return plug.forward;
            }
            return anchor != null ? anchor.forward : Vector3.forward;
        }

        private void GenerateMeshInternal(Vector3 p0, Vector3 dir0, Vector3 p3, Vector3 dir1)
        {
            EnsureMeshComponents();

            float distance = Vector3.Distance(p0, p3);
            if (distance < 0.05f)
            {
                _proceduralMesh.Clear();
                return;
            }

            // Tangent handles: project along socket normal direction
            float handleLength = Mathf.Clamp(distance * 0.35f, 0.4f, 2.5f) * tangentStiffness;
            Vector3 p1 = p0 + dir0.normalized * handleLength;
            Vector3 p2 = p3 + dir1.normalized * handleLength;

            // Dynamic gravity droop: increases with distance
            float effectiveSag = gravitySag * Mathf.Clamp(distance * 0.18f, 0.2f, 1.5f);

            int rings = lengthSegments + 1;
            int ringVerts = radialSegments + 1; // +1 to close UV seam
            int totalVerts = rings * ringVerts;

            Vector3[] vertices = new Vector3[totalVerts];
            Vector3[] normals = new Vector3[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];
            int[] triangles = new int[lengthSegments * radialSegments * 6];

            // Sample curve points and accumulate length for UVs
            Vector3[] centerLine = new Vector3[rings];
            float[] accumulatedLengths = new float[rings];
            accumulatedLengths[0] = 0f;

            for (int i = 0; i < rings; i++)
            {
                float t = (float)i / lengthSegments;
                // Cubic Bezier base
                Vector3 pt = SampleCubicBezier(p0, p1, p2, p3, t);

                // Additional parabolic gravitational droop (natural catenary shape)
                float sagCurve = 4f * t * (1f - t); // Parabola: 0 at ends, 1 at mid
                pt += Vector3.down * (sagCurve * effectiveSag);

                centerLine[i] = pt;
                if (i > 0)
                {
                    accumulatedLengths[i] = accumulatedLengths[i - 1] + Vector3.Distance(centerLine[i], centerLine[i - 1]);
                }
            }

            float totalLength = accumulatedLengths[rings - 1];

            // Build parallel transport frame to prevent tube twisting
            Vector3 forward = (centerLine[1] - centerLine[0]).normalized;
            Vector3 normal = Vector3.Cross(forward, Vector3.up);
            if (normal.sqrMagnitude < 0.001f)
                normal = Vector3.Cross(forward, Vector3.right);
            normal.Normalize();
            Vector3 binormal = Vector3.Cross(forward, normal).normalized;

            int vertIndex = 0;
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

            for (int ring = 0; ring < rings; ring++)
            {
                Vector3 center = centerLine[ring];

                // Update frame along curve
                if (ring < rings - 1)
                {
                    Vector3 nextFwd = (centerLine[ring + 1] - center).normalized;
                    Quaternion rot = Quaternion.FromToRotation(forward, nextFwd);
                    normal = (rot * normal).normalized;
                    binormal = (rot * binormal).normalized;
                    forward = nextFwd;
                }

                float vCoord = (accumulatedLengths[ring] * uvTilePerMeter);

                for (int rad = 0; rad < ringVerts; rad++)
                {
                    float angle = ((float)rad / radialSegments) * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    Vector3 radialDir = (normal * cos + binormal * sin).normalized;
                    Vector3 worldPos = center + radialDir * cableRadius;

                    vertices[vertIndex] = worldToLocal.MultiplyPoint3x4(worldPos);
                    normals[vertIndex] = worldToLocal.MultiplyVector(radialDir).normalized;
                    uvs[vertIndex] = new Vector2((float)rad / radialSegments, vCoord);

                    vertIndex++;
                }
            }

            // Generate quads / triangles
            int triIndex = 0;
            for (int ring = 0; ring < lengthSegments; ring++)
            {
                int currentRingStart = ring * ringVerts;
                int nextRingStart = (ring + 1) * ringVerts;

                for (int rad = 0; rad < radialSegments; rad++)
                {
                    int current = currentRingStart + rad;
                    int next = nextRingStart + rad;

                    // Triangle 1
                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = current + 1;

                    // Triangle 2
                    triangles[triIndex++] = current + 1;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = next + 1;
                }
            }

            _proceduralMesh.Clear();
            _proceduralMesh.vertices = vertices;
            _proceduralMesh.normals = normals;
            _proceduralMesh.uv = uvs;
            _proceduralMesh.triangles = triangles;
            _proceduralMesh.RecalculateTangents();
            _proceduralMesh.RecalculateBounds();
        }

        private static Vector3 SampleCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (ttt * p3);
        }
    }
}
