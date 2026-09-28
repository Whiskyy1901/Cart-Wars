using System;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : NetworkBehaviour
{
    private PlayerInput _input;
    [Header("Camera Variables")]
    [SerializeField] float _sensitivity;
    [SerializeField] private float _maxPitch = 80f;
    [SerializeField] private float _minPitch = -80f;
    private Transform _cameraForward;
    [SerializeField] private Camera _camera;
    [SerializeField] private AudioListener _listener;
    private float _pitch;

    [Header("FOV Punch (Grapple Pull)")]
    [SerializeField] private GrappleGun _grapple;
    [SerializeField] private float _pullFov = 75f;
    [SerializeField] private float _fovLerpSpeed = 8f;
    private float _baseFov;

    public Transform CameraForward => _cameraForward;
    public Camera Camera => _camera;


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _camera.enabled = true;
        _camera.tag = "MainCamera";
        if(_listener != null)
            _listener.enabled  = true;
    }

    private void Awake()
    {
        _input  = GetComponent<PlayerInput>();
        _cameraForward = _camera.gameObject.transform;
        _grapple = GetComponentInChildren<GrappleGun>();
        _baseFov = _camera.fieldOfView;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        if(!isLocalPlayer)
        {
            _camera.enabled = false;
            if(_listener != null)
                _listener.enabled = false;
        }
    }

    private void Update()
    {
        if(!isLocalPlayer) return;

        if(Mouse.current.middleButton.wasPressedThisFrame)
        {
            if(Cursor.lockState == CursorLockMode.Locked)
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
        UpdateFov();
       
            
    } 

    private void LateUpdate()
    {
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
    private void UpdateFov()
    {
        if(_camera == null || _grapple == null )
            return;
        
       
        
        float targetFov = _grapple.IsPulling || _grapple.IsGrappling ? _pullFov : _baseFov;
        _camera .fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, _fovLerpSpeed);
    }

}
