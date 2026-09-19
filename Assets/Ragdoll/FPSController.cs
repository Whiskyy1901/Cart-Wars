using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]

public class FPSController : MonoBehaviour
{
    [Header("Camera & look")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 8.5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("InputAction References")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;


    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch = 0.0f;



    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }
    private void OnEnable()
    {
        moveAction?.action.Enable();
        lookAction?.action.Enable();
        jumpAction?.action.Enable();
        sprintAction?.action.Enable();

    }

    private void OnDisable()
    {
        moveAction?.action.Disable();
        lookAction?.action.Disable();
        jumpAction?.action.Disable();
        sprintAction?.action.Disable();
    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

   
    void Update()
    {
        HandleMovement();
    }
    void LateUpdate()
    {
        HandleLook();
    }

    private void HandleLook()
    {
        if(lookAction == null) return;
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        //Vertical look
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if(cameraHolder == null) return;
        else
        {
            cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        }

        transform.Rotate(Vector3.up * mouseX);

    }

    private void HandleMovement()
    {
        if(moveAction == null) return;

        bool isGrounded = characterController.isGrounded;
        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;

        }

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDirection = transform.right *moveInput.x + transform.forward * moveInput.y;

        bool isSprinting = sprintAction!= null && sprintAction.action.IsPressed();
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        if(jumpAction != null && jumpAction.action.WasPressedThisFrame() &&isGrounded)
        {
            velocity.y = Mathf.Sqrt(-2 * gravity * jumpHeight);
        }

        velocity.y +=gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
