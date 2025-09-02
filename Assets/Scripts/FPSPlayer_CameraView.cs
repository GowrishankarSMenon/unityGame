using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayer_CameraView : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public PlayerInventory inventory;
    public Animator characterAnimator;
    public Transform playerModel; // The actual character model
    public Transform cameraPivot; // This is the new camera pivot transform

    [Header("Camera")]
    public Vector3 cameraOffset = new Vector3(0, 1.8f, -2.5f);
    public bool lookAtPlayer = false;
    [Tooltip("Distance behind player when looking at them")]
    public float lookAtDistance = 3f;
    [Tooltip("How fast the camera follows the player")]
    public float cameraSmoothTime = 0.1f;

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
    public float acceleration = 5f;
    [Tooltip("Speed at which player rotates to face movement direction")]
    public float rotationSpeed = 10f;

    [Header("Input")]
    public KeyCode toggleCursorKey = KeyCode.Escape;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode pickupKey = KeyCode.F;
    public KeyCode lookAtPlayerKey = KeyCode.V;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask = ~0;

    // internal
    private CharacterController controller;
    private float pitch = 0f;
    private float yaw = 0f;
    private float pitchVel = 0f;
    private float yawVel = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCursorLocked = true;
    private float smoothY;
    private Vector3 cameraFollowVelocity = Vector3.zero;

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

        if (playerModel == null)
        {
            playerModel = transform;
        }

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform, false);
            float localY = controller.center.y - (controller.height * 0.5f) + controller.radius - 0.02f;
            gc.transform.localPosition = new Vector3(0f, localY, 0f);
            groundCheck = gc.transform;
        }

        // Detach the camera from the player in the hierarchy
        if (playerCamera.transform.parent != null)
        {
            playerCamera.transform.parent = null;
        }

        LockCursor();
    }

    void Update()
    {
        HandleCursorToggle();
        HandleLookAtToggle();
        if (!isCursorLocked) return;

        HandleMouseLook();
        HandleMovementAndAnimation();
        HandlePickup();
    }

    void LateUpdate()
    {
        // Smoothly follow the player's position
        playerCamera.transform.position = Vector3.SmoothDamp(playerCamera.transform.position, playerModel.position + cameraOffset, ref cameraFollowVelocity, cameraSmoothTime);

        if (lookAtPlayer)
        {
            Vector3 behindPlayer = playerModel.position - playerModel.forward * lookAtDistance;
            behindPlayer.y = playerModel.position.y + cameraOffset.y;
            playerCamera.transform.position = Vector3.SmoothDamp(playerCamera.transform.position, behindPlayer, ref cameraFollowVelocity, cameraSmoothTime);
            playerCamera.transform.LookAt(playerModel.position + Vector3.up * 1f);
        }
    }

    void HandleLookAtToggle()
    {
        if (Input.GetKeyDown(lookAtPlayerKey))
        {
            lookAtPlayer = !lookAtPlayer;
        }
    }

    void HandleMouseLook()
    {
        if (lookAtPlayer) return;

        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        if (invertY) my = -my;

        float scale = useDeltaTimeForMouse ? mouseSensitivity * Time.deltaTime : mouseSensitivity;

        yaw += mx * scale;
        pitch -= my * scale;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        if (enableSmoothing)
        {
            float smoothedYaw = Mathf.SmoothDampAngle(playerCamera.transform.localEulerAngles.y, yaw, ref yawVel, smoothTime);
            float currentCamPitch = playerCamera.transform.localEulerAngles.x;
            if (currentCamPitch > 180f) currentCamPitch -= 360f;
            float smoothedPitch = Mathf.SmoothDampAngle(currentCamPitch, pitch, ref pitchVel, smoothTime);
            playerCamera.transform.localRotation = Quaternion.Euler(smoothedPitch, smoothedYaw, 0f);
        }
        else
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    void HandleMovementAndAnimation()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(h, 0f, v).normalized;
        float inputMagnitude = inputDirection.magnitude;

        // Animate the blend tree based on the normalized input direction and magnitude
        characterAnimator.SetFloat("X", inputDirection.x);

        float targetY = (v > 0) ? 2f : 0f;
        smoothY = Mathf.Lerp(smoothY, targetY, acceleration * Time.deltaTime);

        if (targetY == 0f && Mathf.Abs(smoothY) < 0.01f)
        {
            smoothY = 0f;
        }

        characterAnimator.SetFloat("Y", smoothY);
        characterAnimator.SetFloat("Magnitude", inputMagnitude);

        float targetSpeed = Mathf.Lerp(walkSpeed, runSpeed, inputMagnitude);

        Vector3 camForward = playerCamera.transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = playerCamera.transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 moveDirection = (camForward * v + camRight * h);

        // Rotate the player to face the direction of movement
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Vector3 currentVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        Vector3 targetVelocity = moveDirection.normalized * targetSpeed;

        Vector3 move = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

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
