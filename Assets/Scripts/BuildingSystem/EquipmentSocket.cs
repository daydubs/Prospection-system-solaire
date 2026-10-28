using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a connection point (Socket) for equipments inside base modules.
/// Similar to ModuleSocket, it allows equipment to snap into place within modules.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class EquipmentSocket : MonoBehaviour
{
    [Header("Socket Settings")]
    [Tooltip("Type of socket, used to restrict which equipments can connect here (e.g. 'General', 'Scrubber', 'WallMount').")]
    public string socketType = "General";

    [Tooltip("Radius / size of the socket for visualization and overlap checks.")]
    public float socketSize = 0.5f;

    [Tooltip("If true, this socket is a mount point in a module/wall. If false, this socket is on the equipment itself.")]
    public bool isHostSocket = true;

    [Tooltip("If true, an equipment is currently mounted to this socket.")]
    public bool isOccupied = false;

    [Tooltip("Reference to the currently mounted equipment.")]
    public GameObject occupiedEquipment;

    [Header("Visual Preview / Ghost")]
    [Tooltip("Optional preview object using ghost materials (e.g. GeneralGhost) to aid visual placement in the editor.")]
    [SerializeField] private GameObject ghostPreview;

    [Tooltip("If true, automatically hides the ghost preview during Play mode.")]
    public bool hidePreviewAtRuntime = true;

    public GameObject GhostPreview
    {
        get => ghostPreview;
        set => ghostPreview = value;
    }

    /// <summary>
    /// Gets the forward direction representing the outward direction of this socket.
    /// For a host socket on a wall, this points away from the wall into the room.
    /// For an equipment socket, this points outwards from the mount face towards the wall.
    /// </summary>
    public Vector3 OutwardDirection => transform.forward;

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = socketSize;
        }

        if (Application.isPlaying && hidePreviewAtRuntime && ghostPreview != null)
        {
            ghostPreview.SetActive(false);
        }
    }

    private void Start()
    {
        UpdateVisualState();
    }

    public void SetOccupied(bool occupied, GameObject equipment = null)
    {
        isOccupied = occupied;
        occupiedEquipment = equipment;
        if (occupied && ghostPreview != null)
        {
            ghostPreview.SetActive(false);
        }
        UpdateVisualState();
    }

    /// <summary>
    /// Displays or hides the ghost preview on this socket (e.g. during placement hovering).
    /// </summary>
    public void ShowGhostPreview(bool show)
    {
        if (isOccupied)
        {
            if (ghostPreview != null) ghostPreview.SetActive(false);
            return;
        }

        if (ghostPreview != null)
        {
            ghostPreview.SetActive(show);
        }
    }

    public void UpdateVisualState()
    {
        if (ghostPreview != null)
        {
            if (isOccupied)
            {
                ghostPreview.SetActive(false);
            }
            else if (Application.isPlaying && hidePreviewAtRuntime)
            {
                ghostPreview.SetActive(false);
            }
            else
            {
                ghostPreview.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Checks if this socket can connect with another equipment socket.
    /// </summary>
    public bool CanConnect(EquipmentSocket other)
    {
        if (other == null) return false;
        if (isOccupied || other.isOccupied) return false;

        return CanConnectType(other.socketType);
    }

    /// <summary>
    /// Checks if this socket type is compatible with a desired equipment socket type.
    /// </summary>
    public bool CanConnectType(string desiredType)
    {
        if (string.IsNullOrEmpty(socketType) || string.IsNullOrEmpty(desiredType)) return true;
        if (string.Equals(socketType, "General", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(desiredType, "General", System.StringComparison.OrdinalIgnoreCase)) return true;

        return string.Equals(socketType, desiredType, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Snaps an equipment root GameObject so that its EquipmentSocket aligns flush with this socket.
    /// </summary>
    public void SnapEquipment(GameObject equipmentRoot, EquipmentSocket equipmentSocket = null)
    {
        if (equipmentRoot == null) return;
        if (equipmentSocket == null)
            equipmentSocket = equipmentRoot.GetComponentInChildren<EquipmentSocket>();

        if (equipmentSocket == null)
        {
            equipmentRoot.transform.position = transform.position;
            equipmentRoot.transform.rotation = transform.rotation;
            return;
        }

        // Align rotation: equipment socket forward aligns to -this.OutwardDirection
        Vector3 socketUp = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(OutwardDirection, Vector3.up)) > 0.8f)
        {
            socketUp = transform.up;
            if (Vector3.Cross(OutwardDirection, socketUp).sqrMagnitude < 0.01f)
                socketUp = Vector3.forward;
        }

        Quaternion targetRotation = Quaternion.LookRotation(-OutwardDirection, socketUp);
        Quaternion rotationOffset = Quaternion.Inverse(equipmentSocket.transform.localRotation);
        equipmentRoot.transform.rotation = targetRotation * rotationOffset;

        // Align position
        Vector3 positionOffset = equipmentRoot.transform.position - equipmentSocket.transform.position;
        equipmentRoot.transform.position = transform.position + positionOffset;
    }

    /// <summary>
    /// Static helper to find the closest unoccupied host socket to a given point.
    /// </summary>
    public static EquipmentSocket FindClosestUnoccupiedHostSocket(Vector3 point, float maxDistance, string desiredType = null)
    {
        EquipmentSocket closest = null;
        float closestDist = maxDistance;

        EquipmentSocket[] allSockets = Object.FindObjectsByType<EquipmentSocket>(FindObjectsInactive.Exclude);
        foreach (var s in allSockets)
        {
            if (!s.isHostSocket || s.isOccupied) continue;

            if (!string.IsNullOrEmpty(desiredType) && !string.IsNullOrEmpty(s.socketType))
            {
                if (!string.Equals(s.socketType, "General", System.StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(desiredType, "General", System.StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(s.socketType, desiredType, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            float dist = Vector3.Distance(point, s.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = s;
            }
        }

        return closest;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : (isHostSocket ? Color.cyan : Color.yellow);

        // Draw center sphere
        Gizmos.DrawWireSphere(transform.position, 0.15f * socketSize);

        // Draw outward direction arrow
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * socketSize);

        // Draw up direction arrow
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * (socketSize * 0.5f));

        // Draw socket frame
        Gizmos.color = isOccupied ? Color.red : (isHostSocket ? Color.cyan : Color.yellow);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.forward * (socketSize * 0.25f), new Vector3(socketSize, socketSize, socketSize * 0.5f));
        Gizmos.matrix = oldMatrix;
    }
}
