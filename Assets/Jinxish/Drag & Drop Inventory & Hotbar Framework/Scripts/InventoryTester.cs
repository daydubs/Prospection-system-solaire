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

        void Start()
        {
            itemPickupHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<ItemPickupHandler>();
        }

        void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                AddItem(testItem);
            }
            if (Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                AddItem(testItem2);
            }
            if (Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                AddItem(testItem3);
            }
        }

        public void AddItem(Item item)
        {
            itemPickupHandler.PickupItem(item);
        }
    }

}
