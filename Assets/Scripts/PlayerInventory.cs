using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // A dictionary to hold the item ID and the quantity.
    public Dictionary<string, int> items = new Dictionary<string, int>();
    public UIManager uiManager;
    /// <summary>
    /// Adds a specified quantity of an item to the inventory.
    /// </summary>
    /// <param name="itemId">The ID of the item to add.</param>
    /// <param name="quantity">The number of items to add.</param>
    public void AddItem(string itemId, int quantity)
    {
        if (items.ContainsKey(itemId))
        {
            items[itemId] += quantity;
        }
        else
        {
            items.Add(itemId, quantity);
        }
        uiManager.UpdateInventoryUI();
        Debug.Log("Added " + quantity + " " + itemId + " to inventory.");
    }

    /// <summary>
    /// Removes a specified quantity of an item from the inventory.
    /// </summary>
    /// <param name="itemId">The ID of the item to remove.</param>
    /// <param name="quantity">The number of items to remove.</param>
    /// <returns>True if the items were successfully removed, false otherwise.</returns>
    public bool RemoveItem(string itemId, int quantity)
    {
        if (items.ContainsKey(itemId) && items[itemId] >= quantity)
        {
            items[itemId] -= quantity;
            if (items[itemId] == 0)
            {
                items.Remove(itemId);
            }
            uiManager.UpdateInventoryUI();
            Debug.Log("Removed " + quantity + " " + itemId + " from inventory.");
            return true;
        }

        Debug.LogWarning("Failed to remove " + quantity + " " + itemId + ". Not enough items in inventory.");
        return false;
    }

    /// <summary>
    /// Checks if the inventory contains the required items and quantities for a recipe.
    /// </summary>
    /// <param name="requiredItems">A dictionary of items required for a recipe.</param>
    /// <returns>True if the inventory contains all the required items, false otherwise.</returns>
    public bool HasItems(Dictionary<string, int> requiredItems)
    {
        foreach (var item in requiredItems)
        {
            if (!items.ContainsKey(item.Key) || items[item.Key] < item.Value)
            {
                return false;
            }
        }
        return true;
    }

    public string GetFirstAvailableItemId()
    {
        foreach (var item in items) // assuming you store items in a List or Dictionary
        {
            if (item.Value > 0) // check if you have at least one
                return item.Key; // return the ID of the item
        }
        return null; // no items available
    }

}