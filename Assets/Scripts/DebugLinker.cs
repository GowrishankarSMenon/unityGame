using UnityEngine;

public class DebugLinker : MonoBehaviour
{
    [Header("Drag the Prefab and Data here")]
    public GameObject prefabToLink;
    public ItemData itemDataAsset;

    private void Start()
    {
        if (prefabToLink != null && itemDataAsset != null)
        {
            itemDataAsset.itemPrefab = prefabToLink;
            Debug.Log("Successfully linked the prefab to the ScriptableObject.");
        }
    }
}