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

    [Header("Interaction Settings")]
    [Tooltip("Objects to consider for levipick (set to your Interactable/Levipick layer)")]
    public LayerMask levipickLayer = ~0;
    [Tooltip("If true, tries to find a parent tagged with this tag when raycast hits a child")]
    public string levipickTag = "Levipick";
    public bool requireTag = true;

    [Header("Joint / physics tuning")]
    public float heldDrag = 6f;
    public float jointBreakForce = 10000f;

    [Header("Placement Settings")]
    public LayerMask placementLayerMask = -1;
    public Material previewMaterial;

    [Header("Debug")]
    public bool debugMode = false;

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

    private Transform highlightedRoot = null;

    public string selectedItemId = "";

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (uiManager != null) uiManager.placementUI.TogglePlacementPanel();
        }

        if (isPlacingMode)
        {
            UpdatePlacementPreview();

            if (placementPreview != null && placementPreview.activeInHierarchy)
                uiManager?.ShowCrosshair("[LMB] Place\n[RMB] Cancel");
            else
                uiManager?.HideCrosshair();

            if (Input.GetMouseButtonDown(0)) PlaceObject();
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.C)) ExitPlacementMode();
        }
        else if (isHoldingObject)
        {
            uiManager?.ShowCrosshair("[E] Drop");
            UpdateAnchorPosition();
            if (Input.GetKeyDown(KeyCode.E)) DropObject();
        }
        else
        {
            HandleHighlighting();

            if (highlightedRoot != null)
            {
                uiManager?.ShowCrosshair("[E] Pick Up");
                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpObject(highlightedRoot);
                }
            }
            else
            {
                uiManager?.HideCrosshair();
            }
        }
    }

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

    public void EnterPlacementMode(string itemId)
    {
        selectedItemId = itemId;
        isPlacingMode = true;
        StartPlacementPreview();
        ToggleCursor(false);
    }

    public void ExitPlacementMode()
    {
        isPlacingMode = false;
        StopPlacementPreview();
        ToggleCursor(true);
    }

    void StartPlacementPreview()
    {
        if (string.IsNullOrEmpty(selectedItemId)) return;
        GameObject prefabToSpawn = itemDatabase.GetPrefab(selectedItemId);
        if (prefabToSpawn == null) return;
        placementPreview = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);
        Renderer previewRenderer = placementPreview.GetComponent<Renderer>();
        if (previewRenderer != null) previewRenderer.material = previewMaterial;
        Rigidbody previewRb = placementPreview.GetComponent<Rigidbody>();
        if (previewRb != null) Destroy(previewRb);
        Collider[] previewColliders = placementPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in previewColliders) col.enabled = false;
    }

    void UpdatePlacementPreview()
    {
        if (placementPreview == null) return;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, placementDistance, placementLayerMask))
        {
            Collider previewCollider = placementPreview.GetComponent<Collider>();
            float heightOffset = 0f;
            if (previewCollider != null) heightOffset = previewCollider.bounds.extents.y;
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
        if (placementPreview != null) Destroy(placementPreview);
    }

    void PlaceObject()
    {
        if (placementPreview == null || !placementPreview.activeInHierarchy) return;
        if (!playerInventory.HasItems(new Dictionary<string, int>() { { selectedItemId, 1 } })) return;
        GameObject prefabToSpawn = itemDatabase.GetPrefab(selectedItemId);
        if (prefabToSpawn == null) return;
        Vector3 spawnPosition = placementPreview.transform.position;
        Quaternion spawnRotation = placementPreview.transform.rotation;
        Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        playerInventory.RemoveItem(selectedItemId, 1);
    }

    Transform FindRootLevipickObject(Transform childTransform)
    {
        Transform current = childTransform;
        while (current != null)
        {
            if (!string.IsNullOrEmpty(levipickTag) && current.CompareTag(levipickTag))
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
        highlightedRoot = null;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
            out hit, maxDistance, levipickLayer, QueryTriggerInteraction.Ignore))
        {
            Transform rootObject = null;

            if (requireTag && !string.IsNullOrEmpty(levipickTag))
            {
                if (hit.transform.CompareTag(levipickTag))
                    rootObject = hit.transform;
                else
                    rootObject = FindRootLevipickObject(hit.transform);
            }
            else
            {
                if (hit.rigidbody != null)
                    rootObject = hit.rigidbody.transform;
                else
                    rootObject = hit.transform;
            }

            if (rootObject != null)
            {
                // 👇 Print debug info when a pickable object is detected
                Debug.Log($"[LevitateSpell] Looking at pickable object: {rootObject.name}");

                Renderer[] renderers = rootObject.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    if (r != null && r.enabled) hitRenderers.Add(r);
                }
                highlightedRoot = rootObject;
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
            if (list1[i] != list2[i]) return false;
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
                for (int i = 0; i < renderer.materials.Length; i++) originals[i] = renderer.materials[i];
                originalMaterials[renderer] = originals;
            }
            Material[] glowMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < glowMaterials.Length; i++) glowMaterials[i] = glowMaterial;
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

        // Try to find a Rigidbody on the object or its children
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.GetComponentInChildren<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[LevitateSpell] PickUp failed: object has no Rigidbody.");
            return;
        }

        // Use the transform that has the Rigidbody as the held object
        heldObject = rb.transform;
        heldObjectRigidbody = rb;

        isHoldingObject = true;
        heldOriginalDrag = heldObjectRigidbody.linearDamping;
        heldOriginalCollisionMode = heldObjectRigidbody.collisionDetectionMode;
        heldOriginalInterpolation = heldObjectRigidbody.interpolation;
        heldObjectRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        heldObjectRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        heldObjectRigidbody.linearDamping = heldDrag;

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
                if (r != null && r.enabled) objRenderers.Add(r);
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
            heldObjectRigidbody.linearDamping = heldOriginalDrag;
            heldObjectRigidbody.collisionDetectionMode = heldOriginalCollisionMode;
            heldObjectRigidbody.interpolation = heldOriginalInterpolation;
            heldObjectRigidbody.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }
        ClearAllHighlights();
        originalMaterials.Clear();
        heldObject = null;
        heldObjectRigidbody = null;
    }

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
