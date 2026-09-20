using System;
using Mirror;
using UnityEngine;


[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : NetworkBehaviour
{
    private PlayerInput _input;
    private Rigidbody _rb;
    private PlayerLook _playerLook;
    private GrappleGun _grapple;

    [Header("Movement Variables")]
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _sprintSpeed = 15f;
    [SerializeField] private float _airControlStrength = 0.2f;
    private float _currentSpeed;
    
    [Header("Jump Variables")]
    [SerializeField] private float _jumpPower = 5f;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _checkRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    private bool _isGrounded;

    [SerializeField] private GameObject[] _menusToDiableMovement;
    private bool _menuActive;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();
        _playerLook = GetComponent<PlayerLook>();
        _grapple = GetComponentInChildren<GrappleGun>();
        _currentSpeed = _moveSpeed;
    }

    private void Update()
    {
        if(!isLocalPlayer)
            return;
        
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _checkRadius, _groundLayer);
        SpeedHandler();

        if (_input.JumpAction.WasPressedThisFrame() && _isGrounded)
        {
            _isGrounded = false;
            Jump(_jumpPower);
        }
    }

    private void FixedUpdate()
    {
        if(!isLocalPlayer)
            return;
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
        Vector3 camForward = _playerLook.CameraForward.forward;
        Vector3 camRight = _playerLook.CameraForward.right;
        camForward.y = 0f;
        camRight.y = 0f;

        Vector3 moveDirection = (camForward * moveVector.y + camRight * moveVector.x);
        Vector3 horizontalVelocity = moveDirection * _currentSpeed;
        if(!_grapple.MoveDisableGrapple)
            _rb.linearVelocity = new Vector3(horizontalVelocity.x, _rb.linearVelocity.y, horizontalVelocity.z);
        else
            _rb.AddForce(moveDirection * _airControlStrength, ForceMode.Acceleration);
    }
    
    private void Jump(float jumpForce)
    {
        _rb.AddForce(Vector3.up*jumpForce, ForceMode.Impulse);
    }
    
}
