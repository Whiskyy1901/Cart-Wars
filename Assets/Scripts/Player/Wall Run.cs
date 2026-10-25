using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
public class WallRun : MonoBehaviour
{
    private PlayerInput _input;
    private PlayerLook _playerLook;
    private Rigidbody _rb;

    [Header("References")]
    [SerializeField] private GrappleGun _grapple;

    [Header("Detection")]
    [SerializeField] private LayerMask _wallLayers;
    [SerializeField] private LayerMask _groundLayers;
    [SerializeField] private float _wallCheckDistance = 0.8f;
    [SerializeField] private float _minHeightAboveGround = 1.5f;
    [SerializeField] private float _maxWallNormalY = 0.2f;      // rejects slopes and floors

    [Header("Wall Run")]
    [SerializeField] private float _wallRunSpeed = 12f;
    [SerializeField] private float _wallStickForce = 20f;       // pushes you into the wall
    [SerializeField] private float _wallGravity = 2f;           // downward pull while running
    [SerializeField] private float _maxWallRunTime = 2.5f;
    [SerializeField] private float _minForwardInput = 0.1f;

    [Header("Wall Jump")]
    [SerializeField] private float _wallJumpUpForce = 7f;
    [SerializeField] private float _wallJumpSideForce = 9f;
    [SerializeField] private float _exitControlLockTime = 0.3f; // blocks instant re-attach
    [SerializeField] private float _wallRunCooldown = 0.3f;

    [Header("Camera")]
    [SerializeField] private float _cameraTiltAngle = 12f;
    [SerializeField] private float _tiltSpeed = 8f;

    private bool _isWallRunning;
    private bool _wallOnRight;
    private Vector3 _wallNormal;
    private float _wallRunTimer;
    private float _exitTimer;
    private float _cooldownTimer;
    private bool _jumpQueued;
    private float _currentTilt;

    public bool IsWallRunning => _isWallRunning;
    public bool IsInExitWindow => _exitTimer > 0f;
    public float CameraTilt => _currentTilt;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _playerLook = GetComponent<PlayerLook>();
        _rb = GetComponent<Rigidbody>();

        if (_grapple == null)
            _grapple = GetComponentInChildren<GrappleGun>();
    }

    private void Update()
    {
        // Input is read in Update so a press is never missed between physics steps
        if (_input.JumpAction.WasPressedThisFrame())
            _jumpQueued = true;

        UpdateTilt();
    }

    private void FixedUpdate()
    {
        if (_exitTimer > 0f) _exitTimer -= Time.fixedDeltaTime;
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.fixedDeltaTime;

        bool grappling = _grapple != null && _grapple.IsGrappling;

        if (_isWallRunning)
        {
            if (grappling || !TryDetectWall() || !HasForwardInput() || IsNearGround() || _wallRunTimer <= 0f)
            {
                StopWallRun(false);
            }
            else if (_jumpQueued)
            {
                WallJump();
            }
            else
            {
                WallRunMovement();
            }
        }
        else if (!grappling && _cooldownTimer <= 0f && _exitTimer <= 0f
                 && HasForwardInput() && !IsNearGround() && TryDetectWall())
        {
            StartWallRun();
        }

        _jumpQueued = false;
    }

    private bool HasForwardInput()
    {
        return _input.MoveVector.y > _minForwardInput;
    }

    private bool IsNearGround()
    {
        return Physics.Raycast(transform.position, Vector3.down, _minHeightAboveGround, _groundLayers);
    }

    private bool TryDetectWall()
    {
        bool hitRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit,
            _wallCheckDistance, _wallLayers);
        bool hitLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit,
            _wallCheckDistance, _wallLayers);

        // Prefer the closer wall if both sides hit (narrow corridor)
        if (hitRight && hitLeft)
        {
            if (rightHit.distance <= leftHit.distance) hitLeft = false;
            else hitRight = false;
        }

        if (hitRight && Mathf.Abs(rightHit.normal.y) <= _maxWallNormalY)
        {
            _wallOnRight = true;
            _wallNormal = rightHit.normal;
            return true;
        }

        if (hitLeft && Mathf.Abs(leftHit.normal.y) <= _maxWallNormalY)
        {
            _wallOnRight = false;
            _wallNormal = leftHit.normal;
            return true;
        }

        return false;
    }

    private void StartWallRun()
    {
        _isWallRunning = true;
        _wallRunTimer = _maxWallRunTime;
        _rb.useGravity = false;

        // Kill vertical speed so you don't carry a fall or jump into the run
        Vector3 v = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(v.x, 0f, v.z);
    }

    private void WallRunMovement()
    {
        _wallRunTimer -= Time.fixedDeltaTime;

        // Direction along the wall, flipped to match where the player is facing
        Vector3 wallForward = Vector3.Cross(_wallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f)
            wallForward = -wallForward;

        // Keep the existing vertical speed so gravity can slowly pull you down
        float verticalSpeed = _rb.linearVelocity.y;
        Vector3 alongWall = wallForward * _wallRunSpeed;
        _rb.linearVelocity = new Vector3(alongWall.x, verticalSpeed, alongWall.z);

        // Light custom gravity and a push into the wall to stay attached
        _rb.AddForce(Vector3.down * _wallGravity, ForceMode.Acceleration);
        _rb.AddForce(-_wallNormal * _wallStickForce, ForceMode.Acceleration);
    }

    private void WallJump()
    {
        Vector3 jumpDir = Vector3.up * _wallJumpUpForce + _wallNormal * _wallJumpSideForce;

        StopWallRun(true);

        // Reset vertical speed so the jump height is consistent
        Vector3 v = _rb.linearVelocity;
        _rb.linearVelocity = new Vector3(v.x, 0f, v.z);
        _rb.AddForce(jumpDir, ForceMode.Impulse);
    }

    private void StopWallRun(bool jumped)
    {
        _isWallRunning = false;
        _rb.useGravity = true;
        _cooldownTimer = _wallRunCooldown;

        if (jumped)
            _exitTimer = _exitControlLockTime;
    }

    private void UpdateTilt()
    {
        float target = 0f;
        if (_isWallRunning)
            target = _wallOnRight ? _cameraTiltAngle : -_cameraTiltAngle;

        _currentTilt = Mathf.Lerp(_currentTilt, target, _tiltSpeed * Time.deltaTime);
    }

    private void OnDisable()
    {
        if (_isWallRunning)
            StopWallRun(false);
    }
}