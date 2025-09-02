using UnityEngine;

// This script will be a component on every collectible item in the world.
public class ItemComponent : MonoBehaviour
{
    // The unique ID of the item (e.g., "Wood", "Mushroom").
    public string itemId;

    // The quantity of the item you get when you pick it up.
    public int quantity = 1;
}