using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : NetworkBehaviour
{
    private PlayerInput _input;
    
    [Header("Camera Variables")]
    [SerializeField] float _sensitivity;
    [SerializeField] private int _maxPitch = 80;
    [SerializeField] private int _minPitch = -80;
    private Transform _cameraForward;
    [SerializeField]private Camera _camera;
    [SerializeField] private AudioListener _listener;
    private float _pitch;
    
    public Transform CameraForward => _cameraForward;

    public override void OnStartLocalPlayer()
    {
        // To make players have their own camera and not a shared one.
        base.OnStartLocalPlayer();
        _camera.enabled = true;
        _camera.tag = "MainCamera";
        if (_listener != null)
            _listener.enabled = true;
    }
    
    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _cameraForward = _camera.gameObject.transform;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            _camera.enabled = false;
            if (_listener != null)
                _listener.enabled = false;
        }
    }

    private void Update()
    {
        if(!isLocalPlayer)
            return;

        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        Look();
    }

    private void Look()
    {
        Vector2 lookValue = _input.LookVector;
        float mouseX = lookValue.x * _sensitivity;
        float mouseY = lookValue.y * _sensitivity;

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        _camera.gameObject.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        
        this.gameObject.transform.Rotate(Vector3.up * mouseX);
    }
}
