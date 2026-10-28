using UnityEngine;

/// <summary>
/// Anchors celestial bodies (Sun, Earth) to the camera or player position
/// to realistically simulate celestial objects at astronomical infinity.
/// </summary>
[ExecuteAlways]
public class CelestialSkyFollow : MonoBehaviour
{
    [Tooltip("Target to follow. If null, automatically targets the main camera or player.")]
    public Transform target;

    [Tooltip("Keep following in editor scene view")]
    public bool followInEditor = true;

    private void LateUpdate()
    {
        if (target == null)
        {
            Camera cam = Camera.main;
            if (cam != null && cam.enabled)
            {
                target = cam.transform;
            }
            else
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        if (target != null)
        {
            transform.position = target.position;
        }
    }
}
