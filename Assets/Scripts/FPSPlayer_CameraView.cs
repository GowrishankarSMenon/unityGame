using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayer_CameraView : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera; // child camera (assign in inspector)
    public PlayerInventory inventory; // PlayerInventory script
    public Animator characterAnimator; // Character's Animator component

    [Header("Mouse / Camera")]
    public float mouseSensitivity = 100f;
    public bool useDeltaTimeForMouse = true;
    public bool invertY = false;
    public bool enableSmoothing = false;
    [Range(0.01f, 0.3f)] public float smoothTime = 0.08f;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravityMultiplier = 2f;
    [Range(0f, 1f)] public float airControl = 0.3f;

    [Header("Input")]
    public KeyCode toggleCursorKey = KeyCode.Escape;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode pickupKey = KeyCode.F;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask = ~0; // defaults to everything

    [Header("Behavior")]
    [Tooltip("If true, the Player GameObject will rotate to match camera yaw. Leave OFF to rotate camera only.")]
    public bool rotatePlayerWithCameraYaw = false;

    // internal
    private CharacterController controller;
    private float pitch = 0f; // up/down
    private float yaw = 0f;    // left/right
    private float pitchVel = 0f;
    private float yawVel = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCursorLocked = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("FPSPlayer_CameraView requires a CharacterController on the same GameObject.");
            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("FPSPlayer_CameraView requires a Camera child. Assign it in inspector.");
                enabled = false;
                return;
            }
        }

        yaw = playerCamera.transform.localEulerAngles.y;
        pitch = playerCamera.transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform, false);
            float localY = controller.center.y - (controller.height * 0.5f) + controller.radius - 0.02f;
            gc.transform.localPosition = new Vector3(0f, localY, 0f);
            groundCheck = gc.transform;
        }

        LockCursor();
    }

    void Update()
    {
        HandleCursorToggle();
        if (!isCursorLocked) return;

        HandleMouseLook();
        HandleMovement();
        HandlePickup();
    }

    void HandleMouseLook()
    {
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        if (invertY) my = -my;

        float scale = useDeltaTimeForMouse ? mouseSensitivity * Time.deltaTime : mouseSensitivity;

        yaw += mx * scale;
        pitch -= my * scale;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        if (rotatePlayerWithCameraYaw)
        {
            if (enableSmoothing)
            {
                float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, yaw, ref yawVel, smoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);

                float currentCamPitch = playerCamera.transform.localEulerAngles.x;
                if (currentCamPitch > 180f) currentCamPitch -= 360f;
                float smoothedPitch = Mathf.SmoothDampAngle(currentCamPitch, pitch, ref pitchVel, smoothTime);
                playerCamera.transform.localRotation = Quaternion.Euler(smoothedPitch, 0f, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
        else
        {
            if (enableSmoothing)
            {
                float currentLocalYaw = playerCamera.transform.localEulerAngles.y;
                if (currentLocalYaw > 180f) currentLocalYaw -= 360f;
                float smoothYaw = Mathf.SmoothDampAngle(currentLocalYaw, yaw, ref yawVel, smoothTime);

                float currentCamPitch = playerCamera.transform.localEulerAngles.x;
                if (currentCamPitch > 180f) currentCamPitch -= 360f;
                float smoothPitch = Mathf.SmoothDampAngle(currentCamPitch, pitch, ref pitchVel, smoothTime);

                playerCamera.transform.localRotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            }
            else
            {
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }
    }

    void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        characterAnimator.SetFloat("X", h);
        characterAnimator.SetFloat("Y", v);

        Vector3 camForward = playerCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = playerCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 direction = (camRight * h + camForward * v);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        float currentSpeed = Input.GetKey(runKey) ? runSpeed : walkSpeed;
        Vector3 move = direction * currentSpeed * (isGrounded ? 1f : airControl);

        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            float g = -Physics.gravity.y * gravityMultiplier;
            velocity.y = Mathf.Sqrt(2f * g * jumpHeight);
            characterAnimator.SetTrigger("Jump");
        }

        velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;

        Vector3 total = move + new Vector3(0f, velocity.y, 0f);
        controller.Move(total * Time.deltaTime);
    }

    private void HandlePickup()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 2f))
        {
            ItemComponent item = hit.transform.GetComponent<ItemComponent>();
            if (item != null)
            {
                if (Input.GetKeyDown(pickupKey))
                {
                    inventory.AddItem(item.itemId, item.quantity);
                    Destroy(hit.transform.gameObject);
                }
            }
        }
    }

    void HandleCursorToggle()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            if (isCursorLocked) UnlockCursor();
            else LockCursor();
        }
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

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
