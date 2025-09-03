using UnityEngine;

public class CameraLookAt : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 3.0f;
    [SerializeField] private Transform target;
    [SerializeField] private float distanceFromTarget = 3.0f;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private Vector2 rotationXMinMax = new Vector2(-40, 40);

    [Header("Input")]
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;

    private float rotationY;
    private float rotationX;
    private Vector3 currentRotation;
    private Vector3 smoothVelocity = Vector3.zero;
    private bool isCursorLocked = true;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("OrbitCamera: No target assigned!");
            return;
        }

        LockCursor();
    }

    void Update()
    {
        HandleInput();

        if (isCursorLocked)
        {
            HandleMouseLook();
        }

        UpdateCameraPosition();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            if (isCursorLocked) UnlockCursor();
            else LockCursor();
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationY += mouseX;
        rotationX += mouseY;

        // Apply clamping for x rotation
        rotationX = Mathf.Clamp(rotationX, rotationXMinMax.x, rotationXMinMax.y);

        Vector3 nextRotation = new Vector3(rotationX, rotationY);

        // Apply damping between rotation changes
        currentRotation = Vector3.SmoothDamp(currentRotation, nextRotation, ref smoothVelocity, smoothTime);

        transform.localEulerAngles = currentRotation;
    }

    void UpdateCameraPosition()
    {
        if (target == null) return;

        // Subtract forward vector of the GameObject to point its forward vector to the target
        transform.position = target.position - transform.forward * distanceFromTarget;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isCursorLocked = true;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isCursorLocked = false;
    }

    void OnDrawGizmos()
    {
        if (target != null)
        {
            // Draw line from camera to target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);

            // Draw target sphere
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.2f);
        }
    }
}