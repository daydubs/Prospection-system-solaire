using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CraftingSystem
{
    /// <summary>
    /// Composant attaché au Lander (l'atterrisseur) pour servir de station d'imprimante 3D et de véhicule de retour au vaisseau.
    /// Implémente IPromptInteractable pour afficher le prompt [E] et ouvrir l'interface de craft, et [F] pour décoller.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LanderCraftingStation : MonoBehaviour, IPromptInteractable
    {
        [Header("Station Info")]
        [SerializeField] private string stationName = "Lander (Atterrisseur & Imprimante 3D)";
        [SerializeField] private string interactionPrompt = "Imprimante 3D [E] | Décoller [F]";
        [SerializeField] private float takeoffInteractionRange = 8f;

        [Header("Recettes")]
        [Tooltip("Recettes spécifiques à cette station. Si vide, toutes les recettes du jeu seront proposées.")]
        public List<CraftingRecipe> stationRecipes = new List<CraftingRecipe>();

        [Header("Audio & Effets (Optionnel)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip craftSound;

        public string StationName => stationName;

        private void Reset()
        {
            // S'assurer qu'un collider est présent pour que le raycast du joueur le détecte
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(4f, 4f, 4f);
            }
        }

        private void Update()
        {
            // Permet d'appuyer sur F pour décoller et rejoindre le vaisseau dès qu'on est à proximité du Lander
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (MainMenuController.Instance != null && MainMenuController.Instance.isMenuOpen) return;

                if (IsPlayerNear())
                {
                    ReturnToSpaceship();
                }
            }
        }

        public bool IsPlayerNear()
        {
            Camera cam = Camera.main;
            if (cam != null && Vector3.Distance(transform.position, cam.transform.position) <= takeoffInteractionRange)
            {
                return true;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null && Vector3.Distance(transform.position, player.transform.position) <= takeoffInteractionRange)
            {
                return true;
            }

            return false;
        }

        public void ReturnToSpaceship()
        {
            Debug.Log("[LanderCraftingStation] Décollage du Lander vers le vaisseau en orbite lunaire !");

            if (CraftingUIManager.Instance != null && CraftingUIManager.Instance.IsOpen)
            {
                CraftingUIManager.Instance.CloseUI();
            }

            PlayCraftEffect();

            // Configurer le SolarSystemManager pour qu'au chargement, le vaisseau apparaisse en orbite de la Lune
            SolarSystemManager.initialDestinationName = "Lune";

            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadSolarSystem();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneTransitionManager.SCENE_SOLAR_SYSTEM);
            }
        }

        public void Interact()
        {
            Debug.Log($"[LanderCraftingStation] Interaction avec la station: {stationName}");
            if (CraftingUIManager.Instance != null)
            {
                CraftingUIManager.Instance.OpenStation(this);
                Debug.Log(CraftingUIManager.Instance.GetType().Name + " ouvert avec succès !");
            }
            else
            {
                Debug.LogWarning("[LanderCraftingStation] CraftingUIManager.Instance introuvable dans la scène !");
            }
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        public void PlayCraftEffect()
        {
            if (audioSource != null && craftSound != null)
            {
                audioSource.PlayOneShot(craftSound);
            }
        }
    }
}
