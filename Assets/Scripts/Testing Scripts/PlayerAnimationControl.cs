using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimationControl : MonoBehaviour
{
    public Animator characterAnimator;
    private PlayerController _playerController;

    [SerializeField] private Camera followCamera;   // reference same camera as PlayerController
    [SerializeField] private float chargeSpeed = 2f;
    [SerializeField] private float decaySpeed = 4f;

    private float animX = 0f;
    private float animY = 0f;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogError("PlayerController not found on the same GameObject!");
        }

        // fallback: auto-grab main camera if not assigned
        if (followCamera == null)
        {
            followCamera = Camera.main;
        }
    }

    void Update()
    {
        Vector3 movementDirection = _playerController.MovementDirection;

        // Convert world-space movement to camera-relative local space
        Vector3 camForward = followCamera.transform.forward;
        Vector3 camRight = followCamera.transform.right;

        // Ignore camera tilt
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Project movement into camera space
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
        animY = Mathf.Clamp(animY, 0f, 2f);

        // Send to Animator
        characterAnimator.SetFloat("X", animX);
        characterAnimator.SetFloat("Y", animY);
    }
}
