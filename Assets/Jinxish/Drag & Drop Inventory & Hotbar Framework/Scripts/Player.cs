using UnityEngine;
using UnityEngine.InputSystem;

namespace InventoryFramework
{
    public class Player : MonoBehaviour
    {
        public FPSController fPSController;
        public GameObject inventory;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }


        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                fPSController.canMove = !fPSController.canMove;
                inventory.SetActive(!inventory.activeSelf);

                if (fPSController.canMove)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
    }
}

