using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a connection point (Socket) for base building modules.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class ModuleSocket : MonoBehaviour
{
    [Tooltip("Size of the socket for visualization and overlap checks.")]
    public float socketSize = 1f;

    [Tooltip("If true, a module is currently connected to this socket.")]
    public bool isOccupied = false;

    [Tooltip("The blocker wall covering this hatch when not connected to another module.")]
    [SerializeField] private GameObject hatchBlocker;

    public GameObject HatchBlocker
    {
        get => hatchBlocker;
        set => hatchBlocker = value;
    }

    private void Awake()
    {
        // Ensure the collider is set to trigger so it doesn't block physics movement
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = socketSize / 2f;
        }

        if (hatchBlocker == null)
        {
            AutoFindHatchBlocker();
        }

        UpdateBlockerState();
    }

    private void Start()
    {
        UpdateBlockerState();
    }

    private void OnValidate()
    {
        if (hatchBlocker == null)
        {
            AutoFindHatchBlocker();
        }
    }

    public void SetConnected(bool connected)
    {
        isOccupied = connected;
        UpdateBlockerState();
    }

    public void UpdateBlockerState()
    {
        if (hatchBlocker == null)
        {
            AutoFindHatchBlocker();
        }

        if (hatchBlocker != null)
        {
            hatchBlocker.SetActive(!isOccupied);
        }
    }

    /// <summary>
    /// Automatically searches the module hierarchy for the nearest hatch blocker object.
    /// </summary>
    public void AutoFindHatchBlocker()
    {
        if (hatchBlocker != null) return;

        Transform rootModule = transform.parent != null ? transform.parent : transform;
        while (rootModule.parent != null && rootModule.parent.GetComponent<ModuleSocket>() == null)
        {
            rootModule = rootModule.parent;
        }

        Transform[] allTransforms = rootModule.GetComponentsInChildren<Transform>(true);
        Transform closest = null;
        float minDistance = 2.0f; // Sockets and their hatch blockers are typically within 0.01 - 0.5m

        foreach (var t in allTransforms)
        {
            if (t == transform) continue;
            string lowerName = t.name.ToLower();
            if (lowerName.Contains("hatch") || lowerName.Contains("blocker"))
            {
                if (lowerName.Contains("door_inner") || lowerName.Contains("door_outer")) continue;

                float dist = Vector3.Distance(transform.position, t.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = t;
                }
            }
        }

        if (closest != null)
        {
            hatchBlocker = closest.gameObject;
        }
    }

    [Tooltip("Type of socket, used to restrict what can connect here. (Optional)")]
    public string socketType = "StandardDoor";

    /// <summary>
    /// Gets the forward direction of the socket, representing the direction
    /// the connected module should be pushed out towards.
    /// </summary>
    public Vector3 OutwardDirection => transform.forward;

    private void OnDrawGizmos()
    {
        // Draw a visual representation of the socket in the Unity Editor
        Gizmos.color = isOccupied ? Color.red : Color.green;

        // Draw a small sphere at the socket location
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        // Draw a line showing the outward direction
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * socketSize);

        // Draw a box to show the face
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.forward * (socketSize * 0.5f), new Vector3(socketSize, socketSize, socketSize));
        Gizmos.matrix = oldMatrix;
    }
}
