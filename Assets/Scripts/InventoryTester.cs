using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    private PlayerInventory inventory;

    void Start()
    {
        // Get a reference to the PlayerInventory script on this GameObject.
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("InventoryTester requires a PlayerInventory component on the same GameObject.");
            return;
        }

        // --- Now we test the inventory functionality ---

        Debug.Log("--- Starting Inventory Test ---");

        // Test 1: Add a new item.
        inventory.AddItem("Wood", 5);

        // Test 2: Check if we have a specific item.
        Debug.Log("Do we have enough wood for a fire? " + inventory.HasItems(new System.Collections.Generic.Dictionary<string, int>() { { "Wood", 3 } }));

        // Test 3: Remove some items.
        inventory.RemoveItem("Wood", 2);

        // Test 4: Try to remove more items than we have (this should fail).
        inventory.RemoveItem("Wood", 10);

        Debug.Log("--- Inventory Test Complete ---");
    }
}