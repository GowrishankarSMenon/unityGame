using UnityEngine;
using UnityEngine.UIElements;

public class LevPick : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public UIDocument crosshairUI;

    [Header("Settings")]
    public float maxDistance = 5f;
    public float holdDistance = 2f;
    public float throwForce = 10f;
    public float heldDrag = 6f;
    public float jointBreakForce = 10000f;

    [Header("Crosshair Offset")]
    public Vector2 crosshairOffset = Vector2.zero; // Offset from screen center in pixels

    private Transform heldObject;
    private Rigidbody heldRb;
    private FixedJoint heldJoint;
    private GameObject holdAnchor;
    private Rigidbody holdAnchorRb;

    private float originalDrag;
    private CollisionDetectionMode originalCollisionMode;
    private RigidbodyInterpolation originalInterpolation;

    private bool isHolding = false;
    private Label crosshairLabel;

    void Start()
    {
        if (crosshairUI != null)
        {
            var root = crosshairUI.rootVisualElement;
            crosshairLabel = root.Q<Label>("crosshairLabel");
        }
    }

    void Update()
    {
        if (isHolding)
        {
            UpdateAnchor();
            if (Input.GetKeyDown(KeyCode.E))
                DropObject();
        }
        else
        {
            UpdateCrosshair();
            if (Input.GetKeyDown(KeyCode.E))
                TryPickUp();
        }
    }

    Vector2 GetCrosshairScreenPosition()
    {
        // Use screen center + your desired offset
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        return screenCenter + crosshairOffset;
    }

    void UpdateCrosshair()
    {
        if (crosshairLabel == null) return;

        Vector2 screenPos = GetCrosshairScreenPosition();
        Ray ray = playerCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance) && hit.transform.CompareTag("Levipick"))
        {
            crosshairLabel.style.color = Color.green;
        }
        else
        {
            crosshairLabel.style.color = Color.white;
        }
    }

    void TryPickUp()
    {
        Vector2 screenPos = GetCrosshairScreenPosition();
        Ray ray = playerCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            Transform target = hit.transform;
            if (target.CompareTag("Levipick"))
            {
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    PickUp(target, rb);
                }
            }
        }
    }

    // Rest of the methods remain the same...
    void PickUp(Transform obj, Rigidbody rb)
    {
        heldObject = obj;
        heldRb = rb;

        originalDrag = heldRb.drag;
        originalCollisionMode = heldRb.collisionDetectionMode;
        originalInterpolation = heldRb.interpolation;

        heldRb.drag = heldDrag;
        heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        heldRb.interpolation = RigidbodyInterpolation.Interpolate;

        holdAnchor = new GameObject("LevPickAnchor");
        holdAnchor.transform.position = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
        holdAnchorRb = holdAnchor.AddComponent<Rigidbody>();
        holdAnchorRb.isKinematic = true;

        heldJoint = heldObject.gameObject.AddComponent<FixedJoint>();
        heldJoint.connectedBody = holdAnchorRb;
        heldJoint.breakForce = jointBreakForce;
        heldJoint.breakTorque = jointBreakForce;

        isHolding = true;
    }

    void UpdateAnchor()
    {
        if (holdAnchor != null)
            holdAnchor.transform.position = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
    }

    void DropObject()
    {
        if (heldJoint != null) Destroy(heldJoint);
        if (holdAnchor != null) Destroy(holdAnchor);

        if (heldRb != null)
        {
            heldRb.drag = originalDrag;
            heldRb.collisionDetectionMode = originalCollisionMode;
            heldRb.interpolation = originalInterpolation;

            heldRb.AddForce(playerCamera.transform.forward * throwForce, ForceMode.Impulse);
        }

        heldObject = null;
        heldRb = null;
        heldJoint = null;
        holdAnchor = null;
        holdAnchorRb = null;

        isHolding = false;
    }

    void OnDisable()
    {
        if (isHolding) DropObject();
    }
}