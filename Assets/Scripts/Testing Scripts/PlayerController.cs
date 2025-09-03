using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CharacterController _controller;
    private Vector3 _movementDirection;
    private bool cursorLocked = true;

    [SerializeField] private float _playerSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private Camera _followCamera;

    private Vector3 _playerVelocity;
    private bool _groundedPlayer;

    [SerializeField] private float _jumpHeight = 1.0f;
    [SerializeField] private float _gravityValue = -9.81f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private bool invertY = false;
    private float xRotation = 0f;

    public bool IsTurning { get; set; } = false;

    public System.Action OnJump;

    private PlayerAnimationControl _animControl;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animControl = GetComponent<PlayerAnimationControl>();
        ToggleCursor(true);
    }

    private void Update()
    {
        Movement();
        MouseLook();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cursorLocked = !cursorLocked;
            ToggleCursor(cursorLocked);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            invertY = !invertY;
        }
    }

    public void ToggleCursor(bool lockCursor)
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    void Movement()
    {
        _groundedPlayer = _controller.isGrounded;
        if (_groundedPlayer && _playerVelocity.y < 0)
        {
            _playerVelocity.y = 0f;
        }

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movementInput = Quaternion.Euler(0, _followCamera.transform.eulerAngles.y, 0) *
                                new Vector3(horizontalInput, 0, verticalInput);
        _movementDirection = movementInput.normalized;

        // Apply animation-driven speed factor
        float speedFactor = _animControl != null ? _animControl.CurrentSpeedFactor : 1f;
        float finalSpeed = _playerSpeed * Mathf.Lerp(0.5f, 1.5f, speedFactor);

        _controller.Move(finalSpeed * Time.deltaTime * _movementDirection);

        if (_movementDirection != Vector3.zero && !IsTurning)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(_movementDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, _rotationSpeed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && _groundedPlayer)
        {
            _playerVelocity.y += Mathf.Sqrt(_jumpHeight * -3.0f * _gravityValue);
            OnJump?.Invoke();
        }

        _playerVelocity.y += _gravityValue * Time.deltaTime;
        _controller.Move(_playerVelocity * Time.deltaTime);
    }

    void MouseLook()
    {
        if (!cursorLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (invertY) mouseY = -mouseY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        _followCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public Vector3 MovementDirection => _movementDirection;
}
