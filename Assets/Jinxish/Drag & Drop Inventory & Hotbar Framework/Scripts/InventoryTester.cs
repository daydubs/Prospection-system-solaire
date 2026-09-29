using UnityEngine;
using UnityEngine.InputSystem;

namespace InventoryFramework
{
    public class InventoryTester : MonoBehaviour
    {
        public Inventory inventory;
        public Item testItem;
        public Item testItem2;
        public Item testItem3;

        private ItemPickupHandler itemPickupHandler;

        private InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
            inputActions.Player.Hotbar.performed += OnHotbarPerformed;
        }

        private void OnDisable()
        {
            inputActions.Player.Hotbar.performed -= OnHotbarPerformed;
            inputActions.Player.Disable();
        }

        void Start()
        {
            itemPickupHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<ItemPickupHandler>();
        }

        private void OnHotbarPerformed(InputAction.CallbackContext context)
        {
            string controlName = context.control.name;
            if (int.TryParse(controlName, out int number))
            {
                if (number == 1)
                {
                    AddItem(testItem);
                }
                else if (number == 2)
                {
                    AddItem(testItem2);
                }
                else if (number == 3)
                {
                    AddItem(testItem3);
                }
            }
        }

        public void AddItem(Item item)
        {
            itemPickupHandler.PickupItem(item);
        }
    }

}
