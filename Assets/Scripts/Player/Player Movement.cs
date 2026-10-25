using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _input;
    private Rigidbody _rb;
    private PlayerLook _playerLook;
    private WallRun _wallRun;

    [Header("Movement Variables")]
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _sprintSpeed = 15f;
    private float _currentSpeed;

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
        _currentSpeed = _moveSpeed;

        if (_grapple == null)
            _grapple = GetComponentInChildren<GrappleGun>();
    }

    private void Update()
    {
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

    private void SpeedHandler()
    {
        _currentSpeed = _input.IsSprinting ? _sprintSpeed : _moveSpeed;
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