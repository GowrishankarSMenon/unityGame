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

    // Exposed speed factor from blend tree
    public float CurrentSpeedFactor { get; private set; } = 0f;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();

        if (followCamera == null)
            followCamera = Camera.main;
    }

    void OnEnable()
    {
        _playerController.OnJump += HandleJump;
    }

    void OnDisable()
    {
        _playerController.OnJump -= HandleJump;
    }

    void Update()
    {
        if (isUTurn) return;

        Vector3 movementDirection = _playerController.MovementDirection;

        Vector3 camForward = followCamera.transform.forward;
        Vector3 camRight = followCamera.transform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        float targetX = Vector3.Dot(movementDirection, camRight);
        float targetY = Vector3.Dot(movementDirection, camForward);

        if (Mathf.Abs(targetX) > 0.01f)
            animX += targetX * chargeSpeed * Time.deltaTime;
        else
            animX = Mathf.MoveTowards(animX, 0f, decaySpeed * Time.deltaTime);

        if (Mathf.Abs(targetY) > 0.01f)
            animY += targetY * chargeSpeed * Time.deltaTime;
        else
            animY = Mathf.MoveTowards(animY, 0f, decaySpeed * Time.deltaTime);

        animX = Mathf.Clamp(animX, -2f, 2f);
        animY = Mathf.Clamp(animY, -2f, 2f);

        Vector3 inputDir = new Vector3(targetX, 0, targetY);
        if (inputDir.sqrMagnitude > 0.01f)
        {
            inputDir.Normalize();
            Vector3 forward = transform.forward;
            float dot = Vector3.Dot(forward, inputDir);

            if (dot < -0.9f)
            {
                StartCoroutine(DoUTurn(inputDir));
                return;
            }
        }

        characterAnimator.SetFloat("X", animX);
        characterAnimator.SetFloat("Y", animY);

        // Calculate normalized speed (blend tree magnitude)
        CurrentSpeedFactor = Mathf.Clamp01(new Vector2(animX, animY).magnitude / 2f);
    }

    IEnumerator DoUTurn(Vector3 newDir)
    {
        isUTurn = true;
        _playerController.IsTurning = true;

        characterAnimator.SetTrigger("UTurn");

        yield return null;
        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);

        yield return new WaitForSeconds(stateInfo.length);

        transform.forward = newDir;
        animX = 0f;
        animY = 0f;

        _playerController.IsTurning = false;
        isUTurn = false;
    }

    private void HandleJump()
    {
        characterAnimator.SetTrigger("Jump");
    }
}
