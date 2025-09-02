using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Custom/Item Database")]
public class ItemDatabase : ScriptableObject
{
    // A list of all your ItemData ScriptableObjects.
    public List<ItemData> items = new List<ItemData>();

    // A dictionary for quick lookup by item ID.
    private Dictionary<string, GameObject> itemPrefabDictionary = new Dictionary<string, GameObject>();

    // This function will build the dictionary for fast access.
    public void BuildDictionary()
    {
        itemPrefabDictionary.Clear();
        foreach (ItemData itemData in items)
        {
            Debug.Log("Registering item: " + itemData.itemId + " with prefab: " + itemData.itemPrefab);
            if (!itemPrefabDictionary.ContainsKey(itemData.itemId))
            {
                itemPrefabDictionary.Add(itemData.itemId, itemData.itemPrefab);
            }
        }
    }

    /// <summary>
    /// Returns the prefab for a given item ID.
    /// </summary>
    public GameObject GetPrefab(string itemId)
    {
        if (itemPrefabDictionary.ContainsKey(itemId))
        {
            return itemPrefabDictionary[itemId];
        }
        Debug.LogWarning("Prefab for item ID " + itemId + " not found in the database.");
        return null;
    }

    // This is the new, crucial function that forces the database to refresh.
    private void OnEnable()
    {
        BuildDictionary();
    }
}