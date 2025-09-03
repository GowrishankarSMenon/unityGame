using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayer_CameraView : MonoBehaviour
{
    [Header("References")]
    public Animator characterAnimator;
    public Camera followCamera;

    [Header("Movement")]
    [SerializeField] private float playerSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravityValue = -9.81f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float smoothY = 0f;
    [SerializeField] private float acceleration = 8f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Movement();
    }

    void Movement()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Calculate movement direction relative to camera
        Vector3 movementInput = Quaternion.Euler(0, followCamera.transform.eulerAngles.y, 0) * new Vector3(horizontalInput, 0, verticalInput);
        Vector3 movementDirection = movementInput.normalized;

        // Move the character
        controller.Move(movementDirection * playerSpeed * Time.deltaTime);

        // Rotate player to face movement direction
        if (movementDirection != Vector3.zero)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }

        // Handle animations - KEEP EXACT SAME LOGIC
        if (characterAnimator != null)
        {
            Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
            characterAnimator.SetFloat("X", inputDirection.x);

            // Smooth Y animation value
            float targetY = (verticalInput > 0) ? 2f : 0f;
            smoothY = Mathf.Lerp(smoothY, targetY, acceleration * Time.deltaTime);
            if (targetY == 0f && Mathf.Abs(smoothY) < 0.01f)
            {
                smoothY = 0f;
            }
            characterAnimator.SetFloat("Y", smoothY);
            characterAnimator.SetFloat("Magnitude", inputDirection.magnitude);
        }

        // Handle jumping
        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger("Jump");
            }
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
}