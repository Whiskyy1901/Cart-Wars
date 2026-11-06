using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _input;
    private Rigidbody _rb;
    private PlayerLook _playerLook;
    private WallRun _wallRun;
    private CapsuleCollider _collider;

    [Header("Movement Variables")]
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _sprintSpeed = 15f;
    [SerializeField] private bool _toggleSprint;
    [SerializeField] private float _crouchSpeed = 7f;
    [SerializeField] private bool _toggleCrouch;
    private float _currentSpeed;

    [Header("Ground Feel")]
    [SerializeField] private float _groundAcceleration = 70f;
    [SerializeField] private float _groundTurnAcceleration = 110f;
    [SerializeField] private float _groundDeceleration = 45f;
    [SerializeField] private float _overspeedDeceleration = 10f;   // minimum bleed-off above your run speed
    [SerializeField] private float _overspeedDecelFactor = 2.5f;   // extra bleed per m/s of excess (fast = bleeds faster)

    [Header("Slopes")]
    [SerializeField] private float _maxSlopeAngle = 55f;
    [SerializeField] private float _groundSnapDistance = 0.35f;    // how far below the feet still counts as ground
    [SerializeField] private float _stickThreshold = 1.5f;         // moving away from the surface faster than this = airborne

    [Header("Air Feel")]
    [SerializeField] private float _airAcceleration = 35f;         // free acceleration while slower than Air Wish Speed
    [SerializeField] private float _airWishSpeed = 12f;
    [SerializeField] private float _airTurnRate = 140f;            // degrees/sec you can steer while fast (no speed added)
    [SerializeField] private float _airBrake = 6f;                 // pushing against your motion slows you a little
    [SerializeField] private float _airDrag = 0f;

    [Header("Speed Limits")]
    [SerializeField] private float _softSpeedCap = 30f;            // above this, speed decays smoothly
    [SerializeField] private float _softCapDrag = 0.8f;            // how hard (fraction of excess per second)
    [SerializeField] private float _hardSpeedCap = 50f;
    [SerializeField] private float _terminalFallSpeed = 45f;

    [Header("Gravity Feel")]
    [SerializeField] private float _fallMultiplier = 1.6f;
    [SerializeField] private float _lowJumpMultiplier = 2.2f;

    [Header("Slide")]
    [SerializeField] private float _slideMinSpeed = 11f;
    [SerializeField] private float _slideBoost = 2.5f;
    [SerializeField] private float _slideFriction = 5f;
    [SerializeField] private float _slideSlopeGravity = 1.2f;      // downhill slides speed you up

    [Header("Crouch Collider")]
    [SerializeField] private float _crouchHeight = 1.2f;
    [SerializeField] private float _crouchTransitionSpeed = 8f;
    [SerializeField] private LayerMask _ceilingMask;
    private bool _isCrouching;
    private bool _isSliding;
    private float _standHeight;
    private Vector3 _standCenter;

    [Header("Crouch Visuals")]
    [SerializeField] private Transform _eyes;
    [SerializeField] private Transform _graphics;
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private float _visualSmoothing = 20f;
    private Vector3 _eyesStandPos;
    private Vector3 _holdStandPos;
    private Vector3 _graphicsStandPos;
    private Vector3 _graphicsStandScale;

    [Header("Grapple")]
    [SerializeField] private GrappleGun _grapple;

    [Header("Jump Variables")]
    [SerializeField] private float _jumpPower = 5f;
    [SerializeField] private float _airJumpPower = 5f;
    [SerializeField, Min(1)] private int _maxJumps = 2;
    [SerializeField] private bool _resetJumpsOnWallRun = true;
    [SerializeField] private float _coyoteTime = 0.12f;
    [SerializeField] private float _jumpBufferTime = 0.12f;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _checkRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;
    private bool _isGrounded;
    private Vector3 _groundNormal = Vector3.up;
    private float _groundLockTimer;   // briefly ignore ground after jumping so we actually leave it
    private int _jumpsUsed;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _jumpCutActive;

    [SerializeField] private GameObject[] _menusToDiableMovement;
    private bool _menuActive;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();
        _playerLook = GetComponent<PlayerLook>();
        _wallRun = GetComponent<WallRun>();
        _collider = GetComponent<CapsuleCollider>();
        _currentSpeed = _moveSpeed;

        _standHeight = _collider.height;
        _standCenter = _collider.center;
        _crouchHeight = Mathf.Max(_crouchHeight, _collider.radius * 2f + 0.01f);

        if (_eyes != null)
            _eyesStandPos = _eyes.localPosition;
        if (_holdPoint != null)
            _holdStandPos = _holdPoint.localPosition;
        if (_graphics != null)
        {
            _graphicsStandPos = _graphics.localPosition;
            _graphicsStandScale = _graphics.localScale;
        }

        if (_grapple == null)
            _grapple = GetComponentInChildren<GrappleGun>();

        _input.ToggleSprint = _toggleSprint;
        _input.ToggleCrouch = _toggleCrouch;
    }

    private void Update()
    {
        _input.ToggleSprint = _toggleSprint;
        _input.ToggleCrouch = _toggleCrouch;

        if (_groundLockTimer > 0f)
            _groundLockTimer -= Time.deltaTime;

        CheckGround();
        SpeedHandler();

        bool wallRunning = _wallRun != null && _wallRun.IsWallRunning;
        bool grappling = _grapple != null && _grapple.IsGrappling;

        if (_isGrounded && Vector3.Dot(_rb.linearVelocity, _groundNormal) <= _stickThreshold)
        {
            _jumpsUsed = 0;
            _coyoteTimer = _coyoteTime;
            _jumpCutActive = false;
        }
        else
        {
            _coyoteTimer -= Time.deltaTime;
            if (_coyoteTimer <= 0f && _jumpsUsed == 0)
                _jumpsUsed = 1;
        }

        if (wallRunning && _resetJumpsOnWallRun)
            _jumpsUsed = 0;

        if (_input.JumpAction.WasPressedThisFrame())
            _jumpBufferTimer = _jumpBufferTime;
        else
            _jumpBufferTimer -= Time.deltaTime;

        if (_jumpBufferTimer > 0f && !wallRunning && !grappling && _jumpsUsed < _maxJumps)
        {
            bool airJump = _jumpsUsed > 0;
            _jumpsUsed++;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            Jump(airJump ? _airJumpPower : _jumpPower);
        }
    }

    private void FixedUpdate()
    {
        CheckGround();
        HandleCrouch();

        _menuActive = false;
        foreach (var menu in _menusToDiableMovement)
        {
            if (menu.gameObject.activeSelf)
            {
                _menuActive = true;
                break;
            }
        }
        if (_menuActive)
            return;

        Movement(_input.MoveVector);
    }

    private void LateUpdate()
    {
        UpdateCrouchVisuals();
    }



    private void CheckGround()
    {
        _isGrounded = false;
        _groundNormal = Vector3.up;

        if (_groundLockTimer > 0f)
            return;

        Vector3 origin = _groundCheck.position + Vector3.up * _checkRadius;
        float castRadius = _checkRadius * 0.9f;
        float castDistance = _checkRadius + _groundSnapDistance;

        if (Physics.SphereCast(origin, castRadius, Vector3.down, out RaycastHit hit, castDistance,
                _groundLayer, QueryTriggerInteraction.Ignore))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle <= _maxSlopeAngle)
            {
                _isGrounded = true;
                _groundNormal = hit.normal;
            }
        }
    }

    // ---------- Crouch ----------

    private void HandleCrouch()
    {
        bool wasCrouching = _isCrouching;

        if (_input.IsCrouching)
            _isCrouching = true;
        else if (_isCrouching && CanStandUp())
            _isCrouching = false;

        if (_isCrouching && !wasCrouching && _isGrounded)
        {
            Vector3 v = _rb.linearVelocity;
            Vector3 planar = Vector3.ProjectOnPlane(v, _groundNormal);
            if (planar.magnitude >= _slideMinSpeed)
            {
                _isSliding = true;
                _rb.linearVelocity = planar + planar.normalized * _slideBoost;
            }
        }

        if (!_isCrouching)
            _isSliding = false;

        float targetHeight = _isCrouching ? _crouchHeight : _standHeight;

        if (Mathf.Approximately(_collider.height, targetHeight))
            return;

        float newHeight = Mathf.MoveTowards(_collider.height, targetHeight, _crouchTransitionSpeed * Time.fixedDeltaTime);
        _collider.height = newHeight;

        Vector3 center = _standCenter;
        center.y -= (_standHeight - newHeight) * 0.5f;
        _collider.center = center;
    }

    private bool CanStandUp()
    {
        Vector3 worldCenter = transform.TransformPoint(_standCenter);
        Vector3 scale = transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float radius = _collider.radius * radiusScale - 0.02f;
        float halfHeight = Mathf.Max(radius, _standHeight * Mathf.Abs(scale.y) * 0.5f - 0.02f);
        float offset = halfHeight - radius;

        Vector3 up = transform.up;
        Vector3 top = worldCenter + up * offset;
        Vector3 bottom = worldCenter - up * offset;

        return !Physics.CheckCapsule(top, bottom, radius, _ceilingMask, QueryTriggerInteraction.Ignore);
    }

    private void UpdateCrouchVisuals()
    {
        float height = _collider.height;
        float lost = _standHeight - height;
        float ratio = height / _standHeight;
        float k = 1f - Mathf.Exp(-_visualSmoothing * Time.deltaTime);

        if (_eyes != null)
            _eyes.localPosition = Vector3.Lerp(_eyes.localPosition, _eyesStandPos + Vector3.down * lost, k);

        if (_holdPoint != null)
            _holdPoint.localPosition = Vector3.Lerp(_holdPoint.localPosition, _holdStandPos + Vector3.down * lost, k);

        if (_graphics != null)
        {
            Vector3 targetScale = new Vector3(_graphicsStandScale.x, _graphicsStandScale.y * ratio, _graphicsStandScale.z);
            Vector3 targetPos = _graphicsStandPos + Vector3.down * (lost * 0.5f);

            _graphics.localScale = Vector3.Lerp(_graphics.localScale, targetScale, k);
            _graphics.localPosition = Vector3.Lerp(_graphics.localPosition, targetPos, k);
        }
    }

    private void SpeedHandler()
    {
        if (_isCrouching)
            _currentSpeed = _crouchSpeed;
        else if (_input.IsSprinting)
            _currentSpeed = _sprintSpeed;
        else
            _currentSpeed = _moveSpeed;
    }

    // ---------- Movement ----------

    private void Movement(Vector2 moveVector)
    {
        bool wallRunning = _wallRun != null && _wallRun.IsWallRunning;
        bool grappling = _grapple != null && _grapple.IsGrappling;

        // Wall run and grapple own the velocity while active
        if (wallRunning || grappling)
        {
            if (grappling)
                _rb.useGravity = true;   // the grapple script expects normal gravity
            _jumpCutActive = false;
            return;
        }

        float dt = Time.fixedDeltaTime;

        Vector3 camForward = _playerLook.CameraForward.forward;
        Vector3 camRight = _playerLook.CameraForward.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 wishDir = camForward * moveVector.y + camRight * moveVector.x;
        if (wishDir.sqrMagnitude > 1f)
            wishDir.Normalize();
        bool hasInput = wishDir.sqrMagnitude > 0.01f;

        Vector3 vel = _rb.linearVelocity;

        bool wallJumpWindow = _wallRun != null && _wallRun.IsInExitWindow;
        bool stuck = _isGrounded && !wallJumpWindow && Vector3.Dot(vel, _groundNormal) <= _stickThreshold;

        if (stuck)
        {
            // Glued to the surface: gravity off, movement happens ALONG the slope
            _rb.useGravity = false;

            Vector3 n = _groundNormal;
            Vector3 wishSlope = Vector3.ProjectOnPlane(wishDir, n);
            if (wishSlope.sqrMagnitude > 0.0001f)
                wishSlope = wishSlope.normalized * wishDir.magnitude;

            Vector3 planar = Vector3.ProjectOnPlane(vel, n);
            planar = GroundMove(planar, wishSlope, hasInput, n, dt);
            vel = planar;
        }
        else
        {
            _rb.useGravity = true;

            Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
            horiz = AirMove(horiz, wishDir, hasInput, dt);
            vel = new Vector3(horiz.x, vel.y, horiz.z);

            if (vel.y < 0f)
                vel.y += Physics.gravity.y * (_fallMultiplier - 1f) * dt;
            else if (vel.y > 0f && _jumpCutActive && !_input.JumpAction.IsPressed())
                vel.y += Physics.gravity.y * (_lowJumpMultiplier - 1f) * dt;

            vel.y = Mathf.Max(vel.y, -_terminalFallSpeed);
        }

        if (vel.sqrMagnitude > _hardSpeedCap * _hardSpeedCap)
            vel = vel.normalized * _hardSpeedCap;

        _rb.linearVelocity = vel;
    }

    private Vector3 GroundMove(Vector3 planar, Vector3 wishSlope, bool hasInput, Vector3 normal, float dt)
    {
        if (_isSliding)
        {
            planar = Vector3.MoveTowards(planar, Vector3.zero, _slideFriction * dt);

            // Downhill speeds you up, uphill slows you down
            planar += Vector3.ProjectOnPlane(Physics.gravity, normal) * _slideSlopeGravity * dt;

            if (planar.magnitude <= _crouchSpeed)
                _isSliding = false;
            return planar;
        }

        float speed = planar.magnitude;
        float excess = speed - _currentSpeed;
        bool overspeed = excess > 0.1f;

        if (!hasInput)
        {
            float decel = overspeed
                ? Mathf.Max(_overspeedDeceleration, excess * _overspeedDecelFactor)
                : _groundDeceleration;
            return Vector3.MoveTowards(planar, Vector3.zero, decel * dt);
        }

        Vector3 target = wishSlope * _currentSpeed;

        if (overspeed)
        {
            float decel = Mathf.Max(_overspeedDeceleration, excess * _overspeedDecelFactor);
            return Vector3.MoveTowards(planar, target, decel * dt);
        }

        bool opposing = Vector3.Dot(planar, wishSlope) < 0f;
        float accel = opposing ? _groundTurnAcceleration : _groundAcceleration;
        return Vector3.MoveTowards(planar, target, accel * dt);
    }

    private Vector3 AirMove(Vector3 horiz, Vector3 wishDir, bool hasInput, float dt)
    {
        float speed = horiz.magnitude;

        if (hasInput)
        {
            if (speed < _airWishSpeed)
            {
                // Slow: accelerate freely toward the input, up to the wish speed
                horiz += wishDir * _airAcceleration * dt;
                if (horiz.magnitude > _airWishSpeed)
                    horiz = horiz.normalized * _airWishSpeed;
            }
            else
            {
                // Fast: STEER only. Direction changes, speed stays. This is what keeps it controllable.
                float maxTurn = _airTurnRate * Mathf.Deg2Rad * dt * wishDir.magnitude;
                Vector3 dir = Vector3.RotateTowards(horiz.normalized, wishDir.normalized, maxTurn, 0f);
                horiz = dir * speed;

                // Pushing against your motion brakes slightly
                float opposed = -Vector3.Dot(horiz.normalized, wishDir.normalized);
                if (opposed > 0.3f)
                    horiz = Vector3.MoveTowards(horiz, Vector3.zero, _airBrake * opposed * dt);
            }
        }

        if (_airDrag > 0f)
            horiz *= 1f - _airDrag * dt;

        // Soft cap: speed above the threshold decays smoothly instead of snowballing
        float s = horiz.magnitude;
        if (s > _softSpeedCap)
            horiz = horiz.normalized * Mathf.MoveTowards(s, _softSpeedCap, (s - _softSpeedCap) * _softCapDrag * dt);

        return horiz;
    }

    private void Jump(float jumpForce)
    {
        Vector3 v = _rb.linearVelocity;
        _rb.useGravity = true;
        _rb.linearVelocity = new Vector3(v.x, jumpForce, v.z);
        _jumpCutActive = true;
        _groundLockTimer = 0.1f;
    }
}