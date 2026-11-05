using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace BuildingSystem
{
    public class InputSystemFixer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void FixInputModule()
        {
            StandaloneInputModule[] standaloneModules = FindObjectsOfType<StandaloneInputModule>();
            foreach (var standalone in standaloneModules)
            {
                GameObject go = standalone.gameObject;
                Destroy(standalone);

                InputSystemUIInputModule inputSystemModule = go.GetComponent<InputSystemUIInputModule>();
                if (inputSystemModule == null)
                {
                    go.AddComponent<InputSystemUIInputModule>();
                }

                Debug.LogWarning($"Automatically replaced legacy StandaloneInputModule with InputSystemUIInputModule on {go.name}. Please update this permanently in the Unity Editor.");
            }
        }
    }
}
