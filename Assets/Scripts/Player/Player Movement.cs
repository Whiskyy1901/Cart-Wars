using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _input;
    private Rigidbody _rb;
    private PlayerLook _playerLook;
    private WallRun _wallRun;
    private BoxCollider _collider;

    [Header("Movement Variables")]
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _sprintSpeed = 15f;
    [SerializeField] private bool _toggleSprint;
    [SerializeField] private float _crouchSpeed = 7f;
    [SerializeField] private bool _toggleCrouch;
    private float _currentSpeed;

    [Header("Crouch Collider")]
    [SerializeField] private float _crouchHeight = 1.5f;
    [SerializeField] private float _crouchTransitionSpeed = 8f; // units per second
    [SerializeField] private LayerMask _ceilingMask;            // layers that block standing up (NOT the player layer)
    private bool _isCrouching;
    private Vector3 _standSize;
    private Vector3 _standCenter;

    [Header("Crouch Visuals")]
    [SerializeField] private Transform _eyes;
    [SerializeField] private Transform _graphics;
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private float _visualSmoothing = 20f;
    private Vector3 _eyesStandPos;
    private Vector3 _holdStandPos;
    private Vector3 _graphicsStandPos;
    private Vector3 _graphicsStandScale;

    [Header("Grapple Air Control")]
    [SerializeField] private GrappleGun _grapple;
    [SerializeField] private float _airControlStrength = 12f;
    [SerializeField] private float _maxAirSteerSpeed = 12f;
    [SerializeField] private float _postGrappleControlTime = 0.25f;
    private bool _wasGrappling;
    private float _postGrappleTimer;

    [Header("Jump Variables")]
    [SerializeField] private float _jumpPower = 5f;
    [SerializeField] private float _airJumpPower = 5f;
    [SerializeField, Min(1)] private int _maxJumps = 2;
    [SerializeField] private bool _resetJumpsOnWallRun = true;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _checkRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    private bool _isGrounded;
    private bool _wasGrounded;
    private int _jumpsUsed;

    [SerializeField] private GameObject[] _menusToDiableMovement;
    private bool _menuActive;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();
        _playerLook = GetComponent<PlayerLook>();
        _wallRun = GetComponent<WallRun>();
        _collider = GetComponent<BoxCollider>();
        _currentSpeed = _moveSpeed;

        // Remember the standing collider so nothing is hardcoded
        _standSize = _collider.size;
        _standCenter = _collider.center;

        // Remember the standing pose of everything that should follow the crouch
        if (_eyes != null)
            _eyesStandPos = _eyes.localPosition;
        if (_holdPoint != null)
            _holdStandPos = _holdPoint.localPosition;
        if (_graphics != null)
        {
            _graphicsStandPos = _graphics.localPosition;
            _graphicsStandScale = _graphics.localScale;
        }

        if (_grapple == null)
            _grapple = GetComponentInChildren<GrappleGun>();

        // Apply toggle settings before the first input read
        _input.ToggleSprint = _toggleSprint;
        _input.ToggleCrouch = _toggleCrouch;
    }

    private void Update()
    {
        // Sync toggle settings every frame so they can be changed at runtime
        _input.ToggleSprint = _toggleSprint;
        _input.ToggleCrouch = _toggleCrouch;

        _isGrounded = Physics.CheckSphere(_groundCheck.position, _checkRadius, _groundLayer);
        SpeedHandler();

        bool wallRunning = _wallRun != null && _wallRun.IsWallRunning;

        if (_isGrounded && _rb.linearVelocity.y <= 0.1f)
            _jumpsUsed = 0;
        else if (_wasGrounded && !_isGrounded && _jumpsUsed == 0)
            _jumpsUsed = 1;

        if (wallRunning && _resetJumpsOnWallRun)
            _jumpsUsed = 0;

        if (_input.JumpAction.WasPressedThisFrame() && !wallRunning && _jumpsUsed < _maxJumps)
        {
            bool airJump = _jumpsUsed > 0;
            _jumpsUsed++;
            Jump(airJump ? _airJumpPower : _jumpPower);
        }

        _wasGrounded = _isGrounded;
    }

    private void FixedUpdate()
    {
        HandleCrouch();

        foreach (var menu in _menusToDiableMovement)
        {
            if (menu.gameObject.activeSelf)
            {
                _menuActive = true;
                break;
            }
            else
            {
                _menuActive = false;
            }
        }
        if (_menuActive)
            return;
        Movement(_input.MoveVector);
    }

    private void LateUpdate()
    {
        UpdateCrouchVisuals();
    }

    private void HandleCrouch()
    {
        if (_input.IsCrouching)
            _isCrouching = true;
        else if (_isCrouching && CanStandUp())
            _isCrouching = false;

        float targetHeight = _isCrouching ? _crouchHeight : _standSize.y;
        Vector3 size = _collider.size;

        if (Mathf.Approximately(size.y, targetHeight))
            return;

        size.y = Mathf.MoveTowards(size.y, targetHeight, _crouchTransitionSpeed * Time.fixedDeltaTime);
        _collider.size = size;

        Vector3 center = _standCenter;
        center.y -= (_standSize.y - size.y) * 0.5f;
        _collider.center = center;
    }

    private bool CanStandUp()
    {
        Vector3 center = transform.TransformPoint(_standCenter);
        Vector3 halfExtents = Vector3.Scale(_standSize, transform.lossyScale) * 0.5f - Vector3.one * 0.02f;

        return !Physics.CheckBox(center, halfExtents, transform.rotation, _ceilingMask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateCrouchVisuals()
    {
        float height = _collider.size.y;
        float lost = _standSize.y - height;
        float ratio = height / _standSize.y;
        float k = 1f - Mathf.Exp(-_visualSmoothing * Time.deltaTime);

        if (_eyes != null)
            _eyes.localPosition = Vector3.Lerp(_eyes.localPosition, _eyesStandPos + Vector3.down * lost, k);

        if (_holdPoint != null)
            _holdPoint.localPosition = Vector3.Lerp(_holdPoint.localPosition, _holdStandPos + Vector3.down * lost, k);

        if (_graphics != null)
        {
            Vector3 targetScale = new Vector3(_graphicsStandScale.x, _graphicsStandScale.y * ratio, _graphicsStandScale.z);
            Vector3 targetPos = _graphicsStandPos + Vector3.down * (lost * 0.5f);

            _graphics.localScale = Vector3.Lerp(_graphics.localScale, targetScale, k);
            _graphics.localPosition = Vector3.Lerp(_graphics.localPosition, targetPos, k);
        }
    }

    private void SpeedHandler()
    {
        if (_isCrouching)
            _currentSpeed = _crouchSpeed;
        else if (_input.IsSprinting)
            _currentSpeed = _sprintSpeed;
        else
            _currentSpeed = _moveSpeed;
    }

    private void Movement(Vector2 moveVector)
    {
        if (_wallRun != null && _wallRun.IsWallRunning)
            return;

        Vector3 camForward = _playerLook.CameraForward.forward;
        Vector3 camRight = _playerLook.CameraForward.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveVector.y + camRight * moveVector.x);
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        bool grappling = _grapple != null && _grapple.IsGrappling;

        if (_wasGrappling && !grappling)
            _postGrappleTimer = _postGrappleControlTime;
        _wasGrappling = grappling;

        if (_postGrappleTimer > 0f)
            _postGrappleTimer -= Time.fixedDeltaTime;

        bool wallJumpWindow = _wallRun != null && _wallRun.IsInExitWindow;

        if (!grappling && !wallJumpWindow && _postGrappleTimer <= 0f)
        {
            Vector3 horizontalVelocity = moveDirection * _currentSpeed;
            _rb.linearVelocity = new Vector3(horizontalVelocity.x, _rb.linearVelocity.y, horizontalVelocity.z);
        }
        else
        {
            SteerInAir(moveDirection);
        }
    }

    private void SteerInAir(Vector3 moveDirection)
    {
        if (moveDirection == Vector3.zero)
            return;

        float speedAlongInput = Vector3.Dot(_rb.linearVelocity, moveDirection.normalized);
        if (speedAlongInput < _maxAirSteerSpeed)
        {
            _rb.AddForce(moveDirection * _airControlStrength, ForceMode.Acceleration);
        }
    }

    private void Jump(float jumpForce)
    {
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}