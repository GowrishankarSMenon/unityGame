using UnityEngine;

public class ObjectPlacement : MonoBehaviour
{
    [Header("References")]
    public ItemDatabase itemDatabase;   // Assign your ItemDatabase ScriptableObject here
    public string currentItemId;        // The item ID you want to place (set this in Inspector or via UI)

    [Header("Ghost Settings")]
    private GameObject ghostObject;
    private Material ghostMaterial;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Create ghost when game starts (based on current item)
        CreateGhost(currentItemId);
    }

    private void Update()
    {
        if (ghostObject != null)
        {
            UpdateGhostPosition();

            // Place item when pressing "P"
            if (Input.GetKeyDown(KeyCode.P))
            {
                PlaceItem();
            }
        }
    }

    /// <summary>
    /// Spawns a transparent ghost version of the prefab.
    /// </summary>
    void CreateGhost(string itemId)
    {
        GameObject prefab = itemDatabase.GetPrefab(itemId);

        if (prefab == null)
        {
            Debug.LogError("No prefab found for ID: " + itemId);
            return;
        }

        // Destroy old ghost if any
        if (ghostObject != null)
        {
            Destroy(ghostObject);
        }

        ghostObject = Instantiate(prefab);
        ghostObject.name = "Ghost_" + itemId;

        // Make ghost semi-transparent
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                mat.shader = Shader.Find("Transparent/Diffuse");
                Color c = mat.color;
                c.a = 0.4f;
                mat.color = c;
            }
        }
    }

    /// <summary>
    /// Updates ghost position to mouse hit point.
    /// </summary>
    void UpdateGhostPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ghostObject.transform.position = hit.point;
        }
    }

    /// <summary>
    /// Places the real prefab at ghost position.
    /// </summary>
    void PlaceItem()
    {
        GameObject prefab = itemDatabase.GetPrefab(currentItemId);
        if (prefab == null) return;

        Instantiate(prefab, ghostObject.transform.position, ghostObject.transform.rotation);
    }

    /// <summary>
    /// Switch to a new item by ID.
    /// </summary>
    public void SelectItem(string newItemId)
    {
        currentItemId = newItemId;
        CreateGhost(currentItemId);
    }
}
