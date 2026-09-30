with open("Assets/Scripts/MineableResource.cs", "r") as f:
    content = f.read()

search = """                bool success = GameManager.Instance.AddPlayerItem(resourceItem, dropAmount);
                if (success)
                {
                    Debug.Log($"[MineableResource] Miné avec succès : {dropAmount}x {resourceItem.itemName}");
"""

replace = """                ItemPickupHandler pickupHandler = FindAnyObjectByType<ItemPickupHandler>();
                bool success = false;
                if(pickupHandler != null)
                {
                    pickupHandler.PickupItem(resourceItem, dropAmount);
                    success = true;
                }
                else
                {
                    success = GameManager.Instance.AddPlayerItem(resourceItem, dropAmount);
                }

                if (success)
                {
                    Debug.Log($"[MineableResource] Miné avec succès : {dropAmount}x {resourceItem.itemName}");
"""

content = content.replace(search, replace)

with open("Assets/Scripts/MineableResource.cs", "w") as f:
    f.write(content)
