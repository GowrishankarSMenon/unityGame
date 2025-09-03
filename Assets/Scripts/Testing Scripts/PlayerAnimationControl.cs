using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationControl : MonoBehaviour
{
    public Animator characterAnimator;
    private PlayerController _playerController;

    [SerializeField] private Camera followCamera;
    [SerializeField] private float chargeSpeed = 2f;
    [SerializeField] private float decaySpeed = 4f;

    private float animX = 0f;
    private float animY = 0f;
    private bool isUTurn = false;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogError("PlayerController not found on the same GameObject!");
        }

        if (followCamera == null)
        {
            followCamera = Camera.main;
        }
    }

    void Update()
    {
        if (isUTurn) return; // lock animation control during U-Turn

        Vector3 movementDirection = _playerController.MovementDirection;

        // Convert world-space movement to camera-relative local space
        Vector3 camForward = followCamera.transform.forward;
        Vector3 camRight = followCamera.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        float targetX = Vector3.Dot(movementDirection, camRight);
        float targetY = Vector3.Dot(movementDirection, camForward);

        // --- Charge/decay X ---
        if (Mathf.Abs(targetX) > 0.01f)
            animX += targetX * chargeSpeed * Time.deltaTime;
        else
            animX = Mathf.MoveTowards(animX, 0f, decaySpeed * Time.deltaTime);

        // --- Charge/decay Y ---
        if (Mathf.Abs(targetY) > 0.01f)
            animY += targetY * chargeSpeed * Time.deltaTime;
        else
            animY = Mathf.MoveTowards(animY, 0f, decaySpeed * Time.deltaTime);

        // Clamp ranges
        animX = Mathf.Clamp(animX, -2f, 2f);
        animY = Mathf.Clamp(animY, -2f, 2f);

        // --- Check for U-Turn ---
        Vector3 inputDir = new Vector3(targetX, 0, targetY);
        if (inputDir.sqrMagnitude > 0.01f)
        {
            inputDir.Normalize();
            Vector3 forward = transform.forward;
            float dot = Vector3.Dot(forward, inputDir);

            if (dot < -0.9f) // almost opposite
            {
                StartCoroutine(DoUTurn(inputDir));
                return;
            }
        }

        // Send to Animator
        characterAnimator.SetFloat("X", animX);
        characterAnimator.SetFloat("Y", animY);
    }

    IEnumerator DoUTurn(Vector3 newDir)
    {
        isUTurn = true;
        _playerController.IsTurning = true;

        characterAnimator.SetTrigger("UTurn");

        // Wait until animator enters the UTurn state
        yield return null;
        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);

        // Wait for the length of the animation clip
        yield return new WaitForSeconds(stateInfo.length);

        // Snap facing after animation
        transform.forward = newDir;
        animX = 0f;
        animY = 0f;

        _playerController.IsTurning = false;
        isUTurn = false;
    }

}
