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


        private InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
            inputActions.Player.Inventory.performed += OnInventoryToggled;
        }

        private void OnDisable()
        {
            inputActions.Player.Inventory.performed -= OnInventoryToggled;
            inputActions.Player.Disable();
        }

        private void OnInventoryToggled(InputAction.CallbackContext context)
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

