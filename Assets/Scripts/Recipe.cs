using System.Collections.Generic;
using UnityEngine;

// This allows us to create new Recipe assets in Unity.
[System.Serializable]
public class Recipe
{
    // The unique ID of the item this recipe creates.
    public string craftedItemId;

    // The quantity of the item that is created.
    public int craftedQuantity;

    // A list of the items and quantities required for the recipe.
    public List<RequiredItem> requiredItems = new List<RequiredItem>();
}

// A helper class to define the items needed for each recipe.
[System.Serializable]
public class RequiredItem
{
    public string itemId;
    public int quantity;
}