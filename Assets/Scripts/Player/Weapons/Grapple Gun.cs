using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    private PlayerInput _input;
    private PlayerLook _playerLook;
    private LineRenderer _line;
    private Rigidbody _playerRb;
    private SpringJoint _springJoint;

    [Header("Gun Settings")]
    [SerializeField] private float _grappleRange = 20f;
    [SerializeField] private float _grappleRayRadius = 0.5f;
    [SerializeField] private float _grappleCooldown = 2f;
    [SerializeField] private Transform _shootPos;
    [SerializeField] private GameObject _hookObject;
    [SerializeField] private LayerMask _hookAttachLayers;

    [Header("Spring Settings")]
    [SerializeField] private float _spring = 400f;
    [SerializeField] private float _damper = 20f;
    [SerializeField] private float _massScale = 1f;
    [SerializeField] private float _reelSpeed = 5f;
    [SerializeField] private float _minReelDistance = 1f;

    private float _timeFromLastShot;
    private Vector3 _currentHookPos;
    private bool _isGrappling;

    public bool IsGrappling => _isGrappling;
    
    private GameObject hook;

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInput>();
        _playerLook = GetComponentInParent<PlayerLook>();
        _playerRb = GetComponentInParent<Rigidbody>();
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 0;
    }

    private void Update()
    {
        _timeFromLastShot += Time.deltaTime;

        if (_input.ShootAction.WasPressedThisFrame() && _timeFromLastShot >= _grappleCooldown && !_isGrappling)
        {
            Ray ray = new Ray(_shootPos.position, _playerLook.CameraForward.forward);
            RaycastHit hit;
            bool didHit = Physics.SphereCast(ray, _grappleRayRadius, out hit, _grappleRange, _hookAttachLayers);
            _timeFromLastShot = 0;

            if (didHit)
            {
                SpawnHook(hit.point, hit.normal);
            }
        }

        if (_input.ShootAction.WasReleasedThisFrame() && _isGrappling)
        {
            ReleaseHook();
        }

        if (_isGrappling)
        {
            SpawnRope(_currentHookPos);
            if (_springJoint.maxDistance > _minReelDistance)
            {
                _springJoint.maxDistance -= _reelSpeed * Time.deltaTime;
            }
        }
    }

    private void SpawnRope(Vector3 SpawnPos)
    {
        _line.positionCount = 2;
        _line.SetPosition(0, _shootPos.position);
        _line.SetPosition(1, SpawnPos);
    }

    private void SpawnHook(Vector3 SpawnPos, Vector3 SpawnNormal)
    {
        Quaternion rotation = Quaternion.LookRotation(SpawnNormal);
        hook = Instantiate(_hookObject, SpawnPos, rotation);
        _currentHookPos = SpawnPos;
        _isGrappling = true;

        _springJoint = _playerRb.gameObject.AddComponent<SpringJoint>();
        _springJoint.autoConfigureConnectedAnchor = false;
        _springJoint.connectedBody = null;
        _springJoint.connectedAnchor = SpawnPos;

        float distanceToHit = Vector3.Distance(_playerRb.position, SpawnPos);
        _springJoint.minDistance = 0f;
        _springJoint.maxDistance = distanceToHit;

        _springJoint.spring = _spring;
        _springJoint.damper = _damper;
        _springJoint.massScale = _massScale;

        Debug.Log("Spawned");
    }

    private void ReleaseHook()
    {
        _isGrappling = false;

        if (_springJoint != null)
            Destroy(_springJoint);

        if (hook != null)
            Destroy(hook);

        _line.positionCount = 0;
        _currentHookPos = Vector3.zero;
    }
}