using UnityEngine;

/// <summary>
/// Orients the GameObject to always face the active camera.
/// </summary>
[ExecuteAlways]
public class FaceCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        Camera cam = Camera.main;
#if UNITY_EDITOR
        if (!Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
        {
            cam = UnityEditor.SceneView.lastActiveSceneView.camera;
        }
#endif
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
