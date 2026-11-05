namespace InventoryFramework
{
    public static class DragContext
    {
        public static Item draggedItem;
        public static int draggedCount;
        public static float draggedOxygen = 100f;
        public static BatteryInstanceState draggedBattery;
        public static int fromSlotIndex;
        public static SlotOwner fromOwner;
    }
}


