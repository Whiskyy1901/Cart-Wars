using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerInput : NetworkBehaviour
{
    [SerializeField] private InputActionAsset _actionAsset;
    private InputAction _moveAction;
    private Vector2 _moveVector;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private bool _isSprinting;
    private InputAction _lookAction;
    private Vector2 _lookVector;
    private InputAction _interactionAction;
    private InputAction _pickAction;
    private InputAction _leftShootAction;
    private InputAction _rightShootAction;

    public InteractionInputData interactionInputData;
    public PickableInputData pickableInputData;
    public Vector2 MoveVector => _moveVector;
    public InputAction JumpAction => _jumpAction;
    public InputAction SprintAction => _sprintAction;
    public InputAction PickAction => _pickAction;
    public bool IsSprinting => _isSprinting;
    public Vector2 LookVector => _lookVector;
    public InputAction LeftShootAction => _leftShootAction;
    public InputAction RightShootAction => _rightShootAction;

    public bool _takingInput = true;

    private void Awake()
    {
        _takingInput = true;
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _lookAction = InputSystem.actions.FindAction("Look");
        _interactionAction = InputSystem.actions.FindAction("Interact");
        _pickAction = InputSystem.actions.FindAction("PickUp");
        _leftShootAction = InputSystem.actions.FindAction("LeftAttack");
        _rightShootAction = InputSystem.actions.FindAction("RightAttack");
        interactionInputData.ResetData();
    }

    private void Update()
    {
        if(!isLocalPlayer)
            return;
        if(!_takingInput)
            return;
        _moveVector = _moveAction.ReadValue<Vector2>();
        _moveVector.Normalize();
        _lookVector = _lookAction.ReadValue<Vector2>();
        _isSprinting = _sprintAction.IsInProgress();
        
        GetInteractionInputData();
        GetPickupInputData();
    }
    
    private void GetPickupInputData()
    {
        pickableInputData.PickClicked = _pickAction.WasPressedThisFrame();
        pickableInputData.PickReleased = _pickAction.WasReleasedThisFrame();
    }

    private void GetInteractionInputData()
    {
        interactionInputData.InteractionPressed = _interactionAction.WasPressedThisFrame();
        interactionInputData.InteractionReleased = _interactionAction.WasReleasedThisFrame();
    }
}
