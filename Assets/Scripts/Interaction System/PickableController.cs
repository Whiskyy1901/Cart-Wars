using Mirror;
using UnityEngine;

public class PickableController : NetworkBehaviour
{
    [Space, Header("Data")]
    [SerializeField] private PickableInputData _pickableInputData = null;
    [SerializeField] private PickableData _pickableData = null;

    public PickableInputData pickableInputData => _pickableInputData;
    public PickableData pickableData => _pickableData;

    [Space, Header("Hold Point")]
    [SerializeField] private Transform _holdPoint = null;
    public Transform HoldPoint => _holdPoint;

    [Space, Header("Ray Settings")]
    [SerializeField] private float _rayDistance = 0f;
    [SerializeField] private float _raySphereRadius = 0f;
    [SerializeField] private LayerMask _pickableLayer;

    private Camera _cam;

   public override void OnStartLocalPlayer()
    {
       _cam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;
        if (_pickableData.IsEmpty())
        {
            CheckForPickable();
        }
        else
        {
            CheckForPickableInput();
        }
    }

    void CheckForPickable()
    {
        if (!_pickableInputData.PickClicked)
            return;

        Ray _ray = new Ray(_cam.transform.position, _cam.transform.forward);
        RaycastHit _hitInfo;
        bool _hitSomething = Physics.SphereCast(_ray, _raySphereRadius, out _hitInfo, _rayDistance, _pickableLayer);

        if (_hitSomething)
        {
            IPickable _pickable = _hitInfo.transform.GetComponent<IPickable>();

            if (_pickable != null)
            {
                _pickableData.PickableItem = _pickable;
                _pickableData.Pick();
            }
        }

        _pickableInputData.PickClicked = false;
    }

    void CheckForPickableInput()
    {
        if (_pickableData.PickableItem == null)
        {
            _pickableData.ResetData();
            return;
        }
        
        if (_pickableInputData.PickClicked)
        {
            _pickableData.Release();
            _pickableInputData.PickClicked = false; 
            return;
        }

        _pickableData.PickableItem.Rigidbody.transform.position = _holdPoint.position;

        _pickableData.Hold();
    }
}