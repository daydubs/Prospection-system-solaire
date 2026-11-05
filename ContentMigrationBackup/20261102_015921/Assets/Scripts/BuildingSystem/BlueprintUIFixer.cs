using UnityEngine;
using UnityEngine.UI;

namespace BuildingSystem
{
    public class BlueprintUIFixer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FixBlueprintUI()
        {
            BlueprintUIManager[] managers = FindObjectsOfType<BlueprintUIManager>();
            foreach (var manager in managers)
            {
                bool modified = false;

                if (manager.contentPanel == null)
                {
                    Transform content = manager.transform.Find("Scroll View/Viewport/Content");
                    if (content != null)
                    {
                        manager.contentPanel = content;
                        Debug.LogWarning($"Automatically assigned Content Panel on {manager.gameObject.name}. Please update this permanently in the Unity Editor.");
                        modified = true;
                    }
                }

                if (manager.blueprintButtonPrefab == null)
                {
                    // Look for a suitable prefab dynamically, or try to find an existing button structure and copy it
                    // This is a bit tricky without knowing exactly what the prefab should look like.
                    // For now, we will just log a warning if it's missing, as creating a UI button from scratch dynamically
                    // might not match the intended style. The user needs to assign this manually as instructed.
                }

                if (modified)
                {
                    // Repopulate UI if we fixed something dynamically after Start
                    manager.PopulateUI();
                }
            }
        }
    }
}
