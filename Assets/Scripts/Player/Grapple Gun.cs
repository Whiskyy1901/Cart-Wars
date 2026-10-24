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

    private GameObject hook;

    public bool IsGrappling => _isGrappling;

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInput>();
        _playerLook = GetComponentInParent<PlayerLook>();
        _playerRb = GetComponentInParent<Rigidbody>();
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 0;
        _timeFromLastShot = _grappleCooldown;
    }

    private void Update()
    {
        _timeFromLastShot += Time.deltaTime;

        if (_input.LeftShootAction.WasPressedThisFrame() && _timeFromLastShot >= _grappleCooldown && !_isGrappling)
        {
            // Cast from the camera so the hit point matches the crosshair
            Transform cam = _playerLook.CameraForward;
            Ray ray = new Ray(cam.position, cam.forward);

            // A miss costs nothing, the cooldown only starts when a grapple is released
            if (Physics.Raycast(ray, out RaycastHit hit, _grappleRange, _hookAttachLayers))
            {
                SpawnHook(hit.point, hit.normal);
            }
        }

        if (_input.LeftShootAction.WasReleasedThisFrame() && _isGrappling)
        {
            ReleaseHook();
        }
    }

    private void FixedUpdate()
    {
        // Reel in on the physics step so it lines up with the joint
        if (_isGrappling && _springJoint != null && _springJoint.maxDistance > _minReelDistance)
        {
            _springJoint.maxDistance -= _reelSpeed * Time.fixedDeltaTime;
        }
    }

    private void LateUpdate()
    {
        // Draw after the player has moved this frame to avoid rope jitter
        if (_isGrappling)
        {
            SpawnRope(_currentHookPos);
        }
    }

    private void OnDisable()
    {
        if (_isGrappling)
            ReleaseHook();
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

        // Fresh joint every grapple, so no stale anchor or spring state carries over
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
    }

    private void ReleaseHook()
    {
        _isGrappling = false;

        if (_springJoint != null)
            Destroy(_springJoint);

        if (hook != null)
            Destroy(hook);

        if (_line != null)
            _line.positionCount = 0;

        _currentHookPos = Vector3.zero;
        _timeFromLastShot = 0f; // cooldown starts on release
    }
}