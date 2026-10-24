using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
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

    public Transform CameraForward => _cameraForward;
    public Camera Camera => _camera;


    public void Start()
    {
        _camera.enabled = true;
        _camera.tag = "MainCamera";
    }

    private void Awake()
    {
        _input  = GetComponent<PlayerInput>();
        _cameraForward = _camera.gameObject.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
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

        gameObject.transform.Rotate(Vector3.up * mouseX);

    }
}
