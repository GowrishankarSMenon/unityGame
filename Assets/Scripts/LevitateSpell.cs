using System.Collections.Generic;
using UnityEngine;

public class LevitateSpell : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Material glowMaterial;
    public PlayerInventory playerInventory;
    public ItemDatabase itemDatabase;
    public UIManager uiManager;

    [Header("Spell Settings")]
    public float maxDistance = 5f;
    public float holdDistance = 2f;
    public float throwForce = 10f;
    public float placementDistance = 3f;

    [Header("Joint / physics tuning")]
    [Tooltip("How much extra drag to apply while object is held (helps stability).")]
    public float heldDrag = 6f;
    [Tooltip("Break force for the joint (high to avoid accidental break).")]
    public float jointBreakForce = 10000f;

    [Header("Placement Settings")]
    [Tooltip("Layer mask for surfaces where objects can be placed")]
    public LayerMask placementLayerMask = -1;
    [Tooltip("Material to use for placement preview")]
    public Material previewMaterial;

    [Header("Internal")]
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private List<Renderer> currentHighlightedRenderers = new List<Renderer>();

    private Transform heldObject;
    private Rigidbody heldObjectRigidbody;
    private FixedJoint heldJoint;
    private GameObject holdAnchor;
    private Rigidbody holdAnchorRigidbody;

    private float heldOriginalDrag = 0f;
    private CollisionDetectionMode heldOriginalCollisionMode = CollisionDetectionMode.Discrete;
    private RigidbodyInterpolation heldOriginalInterpolation = RigidbodyInterpolation.None;

    private bool isHoldingObject = false;
    private bool isPlacingMode = false;
    private GameObject placementPreview;

    public string selectedItemId = "";

    void Update()
    {
        // "C" key to toggle the placement UI
        if (Input.GetKeyDown(KeyCode.C))
        {
            uiManager.placementUI.TogglePlacementPanel();
        }

        // This is the core logic loop. Only one branch can be active at a time.
        if (isPlacingMode)
        {
            UpdatePlacementPreview();
            if (Input.GetMouseButtonDown(0))
            {
                PlaceObject();
            }
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.C))
            {
                ExitPlacementMode();
            }
        }
        else if (isHoldingObject)
        {
            UpdateAnchorPosition();
            if (Input.GetKeyDown(KeyCode.E))
            {
                DropObject();
            }
        }
        else
        {
            HandleHighlighting();
            if (currentHighlightedRenderers.Count > 0 && Input.GetKeyDown(KeyCode.E))
            {
                Transform rootObject = FindRootLevipickObject(currentHighlightedRenderers[0].transform);
                if (rootObject != null)
                {
                    PickUpObject(rootObject);
                }
            }
        }
    }

    // A new public method to manage the cursor state
    public void ToggleCursor(bool lockCursor)
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // A new public method to start placement mode from the UI
    public void EnterPlacementMode(string itemId)
    {
        selectedItemId = itemId;
        isPlacingMode = true;
        StartPlacementPreview();
        ToggleCursor(false);
    }

    // A new public method to exit placement mode
    public void ExitPlacementMode()
    {
        isPlacingMode = false;
        StopPlacementPreview();
        ToggleCursor(true);
    }

    #region Placement Functions
    void StartPlacementPreview()
    {
        if (string.IsNullOrEmpty(selectedItemId))
        {
            Debug.LogWarning("No item selected for placement.");
            return;
        }

        GameObject prefabToSpawn = itemDatabase.GetPrefab(selectedItemId);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Could not find prefab for item: {selectedItemId}");
            return;
        }

        placementPreview = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

        Renderer previewRenderer = placementPreview.GetComponent<Renderer>();
        if (previewRenderer != null)
        {
            previewRenderer.material = previewMaterial;
        }

        Rigidbody previewRb = placementPreview.GetComponent<Rigidbody>();
        if (previewRb != null) Destroy(previewRb);

        Collider[] previewColliders = placementPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in previewColliders)
        {
            col.enabled = false;
        }
    }

    void UpdatePlacementPreview()
    {
        if (placementPreview == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, placementDistance, placementLayerMask))
        {
            // Get the height of the object's collider.
            Collider previewCollider = placementPreview.GetComponent<Collider>();
            float heightOffset = 0f;
            if (previewCollider != null)
            {
                heightOffset = previewCollider.bounds.extents.y;
            }

            // Position the object on the ground, offset by its height.
            placementPreview.transform.position = hit.point + Vector3.up * heightOffset;
            placementPreview.SetActive(true);
        }
        else
        {
            placementPreview.SetActive(false);
        }
    }

    void StopPlacementPreview()
    {
        if (placementPreview != null)
        {
            Destroy(placementPreview);
        }
    }

    void PlaceObject()
    {
        if (placementPreview == null || !placementPreview.activeInHierarchy)
        {
            Debug.LogWarning("Cannot place object - no valid placement position");
            return;
        }

        if (!playerInventory.HasItems(new Dictionary<string, int>() { { selectedItemId, 1 } }))
        {
            Debug.LogWarning("You do not have any " + selectedItemId + " to place.");
            return;
        }

        GameObject prefabToSpawn = itemDatabase.GetPrefab(selectedItemId);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Could not find prefab for item: {selectedItemId}");
            return;
        }

        Vector3 spawnPosition = placementPreview.transform.position;
        Quaternion spawnRotation = placementPreview.transform.rotation;
        GameObject placedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        playerInventory.RemoveItem(selectedItemId, 1);
        Debug.Log($"{selectedItemId} has been placed.");
    }
    #endregion

    #region Core Levitate Logic (Unchanged from your previous code)
    Transform FindRootLevipickObject(Transform childTransform)
    {
        Transform current = childTransform;
        while (current != null)
        {
            if (current.CompareTag("Levipick"))
            {
                return current;
            }
            current = current.parent;
        }
        return null;
    }

    void HandleHighlighting()
    {
        RaycastHit hit;
        List<Renderer> hitRenderers = new List<Renderer>();
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, maxDistance))
        {
            Transform rootObject = null;
            if (hit.transform.CompareTag("Levipick"))
            {
                rootObject = hit.transform;
            }
            else
            {
                rootObject = FindRootLevipickObject(hit.transform);
            }

            if (rootObject != null)
            {
                Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    if (r != null && r.enabled)
                    {
                        hitRenderers.Add(r);
                    }
                }
            }
        }
        if (!ListsEqual(hitRenderers, currentHighlightedRenderers))
        {
            ClearAllHighlights();
            if (hitRenderers.Count > 0)
            {
                ApplyHighlight(hitRenderers);
            }
        }
    }

    bool ListsEqual(List<Renderer> list1, List<Renderer> list2)
    {
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }
        return true;
    }

    void ApplyHighlight(List<Renderer> renderers)
    {
        if (glowMaterial == null) return;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (!originalMaterials.ContainsKey(renderer))
            {
                Material[] originals = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    originals[i] = renderer.materials[i];
                }
                originalMaterials[renderer] = originals;
            }
            Material[] glowMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < glowMaterials.Length; i++)
            {
                glowMaterials[i] = glowMaterial;
            }
            renderer.materials = glowMaterials;
        }
        currentHighlightedRenderers = new List<Renderer>(renderers);
    }

    void ClearAllHighlights()
    {
        foreach (Renderer renderer in currentHighlightedRenderers)
        {
            if (renderer != null && originalMaterials.ContainsKey(renderer))
            {
                renderer.materials = originalMaterials[renderer];
                originalMaterials.Remove(renderer);
            }
        }
        currentHighlightedRenderers.Clear();
    }

    void PickUpObject(Transform obj)
    {
        if (obj == null) return;
        heldObject = obj;
        heldObjectRigidbody = heldObject.GetComponent<Rigidbody>();
        if (heldObjectRigidbody == null)
        {
            Debug.LogWarning("[LevitateSpell] PickUp failed: object has no Rigidbody.");
            heldObject = null;
            return;
        }
        isHoldingObject = true;
        heldOriginalDrag = heldObjectRigidbody.drag;
        heldOriginalCollisionMode = heldObjectRigidbody.collisionDetectionMode;
        heldOriginalInterpolation = heldObjectRigidbody.interpolation;
        heldObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        heldObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        heldObjectRigidbody.drag = heldDrag;
        holdAnchor = new GameObject("LevitateHoldAnchor");
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
        holdAnchor.transform.position = targetPosition;
        holdAnchorRigidbody = holdAnchor.AddComponent<Rigidbody>();
        holdAnchorRigidbody.isKinematic = true;
        heldJoint = heldObject.gameObject.AddComponent<FixedJoint>();
        heldJoint.connectedBody = holdAnchorRigidbody;
        heldJoint.anchor = Vector3.zero;
        heldJoint.connectedAnchor = Vector3.zero;
        heldJoint.breakForce = jointBreakForce;
        heldJoint.breakTorque = jointBreakForce;
        if (currentHighlightedRenderers.Count == 0)
        {
            List<Renderer> objRenderers = new List<Renderer>();
            Renderer[] renderers = heldObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r != null && r.enabled)
                {
                    objRenderers.Add(r);
                }
            }
            ApplyHighlight(objRenderers);
        }
    }

    void UpdateAnchorPosition()
    {
        if (holdAnchor == null || playerCamera == null) return;
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
        holdAnchor.transform.position = targetPosition;
    }

    void DropObject()
    {
        isHoldingObject = false;
        if (heldJoint != null)
        {
            Destroy(heldJoint);
            heldJoint = null;
        }
        if (holdAnchor != null)
        {
            Destroy(holdAnchor);
            holdAnchor = null;
            holdAnchorRigidbody = null;
        }
        if (heldObjectRigidbody != null)
        {
            heldObjectRigidbody.drag = heldOriginalDrag;
            heldObjectRigidbody.collisionDetectionMode = heldOriginalCollisionMode;
            heldObjectRigidbody.interpolation = heldOriginalInterpolation;
            heldObjectRigidbody.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }
        ClearAllHighlights();
        originalMaterials.Clear();
        heldObject = null;
        heldObjectRigidbody = null;
    }
    #endregion

    void OnDisable()
    {
        if (isHoldingObject) DropObject();
        ClearAllHighlights();
        originalMaterials.Clear();
        StopPlacementPreview();
    }
    void OnDestroy()
    {
        ClearAllHighlights();
        originalMaterials.Clear();
        StopPlacementPreview();
    }
}
