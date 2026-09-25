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

    private void Awake()
    {
        // Ensure the collider is set to trigger so it doesn't block physics movement
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.radius = socketSize / 2f;
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
