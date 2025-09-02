using UnityEngine;

[CreateAssetMenu(fileName = "New Item Data", menuName = "Custom/Item Data")]
public class ItemData : ScriptableObject
{
    // The unique ID of the item (e.g., "Wood", "Stone").
    public string itemId;

    // The GameObject prefab that represents this item in the world.
    public GameObject itemPrefab;
}