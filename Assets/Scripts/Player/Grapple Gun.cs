using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrappleGun : MonoBehaviour
{
    public enum GrapplePhase { Waiting, Flying, Grappling }

    public event Action<GrapplePhase, GrapplePhase> PhaseChanged;

    public GrapplePhase CurrentPhase { get; private set; }
    public Vector3 GrapplePoint { get; private set; }
    public bool IsGrappling => CurrentPhase == GrapplePhase.Grappling;

    [Header("References")]
    [SerializeField] private GrappleHook _hook;
    [SerializeField] private Transform _muzzle;

    [Header("Aim")]
    [SerializeField] private float _grappleRange = 60f;
    [SerializeField] private float _aimRadius = 0.3f;
    [SerializeField] private float _assistRadius = 2.5f;
    [SerializeField] private LayerMask _aimLayers;
    [SerializeField] private LayerMask _hookableLayers;

    [Header("Rope")]
    [SerializeField] private float _minRopeLength = 4f;
    [SerializeField] private float _maxRopeLength = 80f;
    [SerializeField] private float _reelInSpeed = 8f;
    [SerializeField] private float _reelOutSpeed = 10f;
    [SerializeField] private float _maxReelSpeedGain = 1.15f;

    [Header("Swing")]
    [SerializeField] private float _extraGravity = 0.8f;      // heavier = faster, punchier arcs
    [SerializeField] private float _pumpAcceleration = 8f;    // W: adds energy along your motion
    [SerializeField] private float _strafeAcceleration = 6f;  // A/D: steer
    [SerializeField] private float _maxSwingSpeed = 55f;
    [SerializeField] private float _airDrag = 0.05f;          // tiny, so energy is mostly conserved

    [Header("Attach")]
    [SerializeField] private float _attachHop = 4f;           // only when grounded
    [SerializeField] private float _attachForwardKick = 6f;   // sideways start so a standing swing gets going
    [SerializeField] private float _groundCheckDistance = 1.2f;

    [Header("Release")]
    [SerializeField] private float _releaseMinSpeed = 12f;
    [SerializeField] private float _releaseBoost = 0.2f;
    [SerializeField] private float _releaseUpBoost = 3f;

    [Header("Keys")]
    [SerializeField] private Key _reelInKey = Key.E;
    [SerializeField] private Key _reelOutKey = Key.Q;

    private PlayerInput _input;
    private PlayerLook _playerLook;
    private Rigidbody _rb;

    private float _ropeLength;

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInput>();
        _playerLook = GetComponentInParent<PlayerLook>();
        _rb = GetComponentInParent<Rigidbody>();

        _hook.Initialize(_muzzle);
        _hook.Attached += OnHookAttached;
        _hook.Returned += OnHookReturned;
    }

    private void OnEnable()
    {
        _hook.gameObject.SetActive(true);
    }

    private void Update()
    {
        var fire = _input.LeftShootAction;

        switch (CurrentPhase)
        {
            case GrapplePhase.Waiting:
                if (fire.WasPressedThisFrame())
                    FireHook();
                break;

            case GrapplePhase.Flying:
                if (fire.WasPressedThisFrame())
                    FireHook();
                else if (fire.WasReleasedThisFrame())
                    _hook.Retract();
                break;

            case GrapplePhase.Grappling:
                if (!fire.IsPressed())
                    Release();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!IsGrappling)
            return;

        float dt = Time.fixedDeltaTime;

        Vector3 toPoint = GrapplePoint - _rb.position;
        float distance = toPoint.magnitude;
        if (distance < 0.01f)
            return;
        Vector3 dir = toPoint / distance;

        var kb = Keyboard.current;
        bool reelIn = kb != null && kb[_reelInKey].isPressed;
        bool reelOut = kb != null && kb[_reelOutKey].isPressed;

        Vector3 vel = _rb.linearVelocity;

        // Rope length changes ONLY from the player's keys. No automatic pulling.
        if (reelIn && _ropeLength > _minRopeLength)
        {
            float old = _ropeLength;
            _ropeLength = Mathf.Max(_minRopeLength, old - _reelInSpeed * dt);

            // Conserve angular momentum so a shorter rope spins you faster
            float inward = Vector3.Dot(vel, dir);
            Vector3 tangent = vel - dir * inward;
            float ratio = Mathf.Min(old / _ropeLength, _maxReelSpeedGain);
            vel = dir * inward + tangent * ratio;
        }
        else if (reelOut)
        {
            _ropeLength = Mathf.Min(_maxRopeLength, _ropeLength + _reelOutSpeed * dt);
        }

        // Gravity does the swinging
        vel += Physics.gravity *  _extraGravity * dt;
        vel *= 1f - _airDrag * dt;

        // Pump and steer, only on the tangent plane so they never pull you toward the anchor
        if (kb != null)
        {
            Vector3 tangentVel = vel - dir * Vector3.Dot(vel, dir);
            Transform cam = _playerLook.CameraForward;

            if (kb.wKey.isPressed && tangentVel.sqrMagnitude > 1f)
                vel += tangentVel.normalized * _pumpAcceleration * dt;

            float strafe = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            if (strafe != 0f)
            {
                Vector3 side = Vector3.ProjectOnPlane(cam.right, dir).normalized;
                vel += side * strafe * _strafeAcceleration * dt;
            }
        }

        // Rope is a max-distance limit. Slack is fine, taut removes outward velocity only.
        if (distance > _ropeLength)
        {
            float outward = -Vector3.Dot(vel, dir);   // positive when moving away from the anchor
            if (outward > 0f)
                vel += dir * outward;
            _rb.position = GrapplePoint - dir * _ropeLength;
        }

        if (vel.sqrMagnitude > _maxSwingSpeed * _maxSwingSpeed)
            vel = vel.normalized * _maxSwingSpeed;

        _rb.linearVelocity = vel;
    }

    private void FireHook()
    {
        Transform cam = _playerLook.CameraForward;
        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;

        bool hit = Physics.SphereCast(origin, _aimRadius, direction, out RaycastHit info,
            _grappleRange, _aimLayers, QueryTriggerInteraction.Ignore);

        if (hit && info.distance <= 0f)
            hit = Physics.Raycast(origin, direction, out info, _grappleRange, _aimLayers,
                QueryTriggerInteraction.Ignore);

        if (!hit || !IsHookable(info.collider.gameObject.layer))
        {
            if (Physics.SphereCast(origin, _assistRadius, direction, out RaycastHit assist,
                    _grappleRange, _hookableLayers, QueryTriggerInteraction.Ignore) && assist.distance > 0f)
            {
                info = assist;
                hit = true;
            }
        }

        bool canAttach = hit && IsHookable(info.collider.gameObject.layer);
        Vector3 targetPoint = hit ? info.point : origin + direction * _grappleRange;
        Vector3 normal = hit ? info.normal : -direction;

        _hook.Launch(targetPoint, normal, canAttach);
        SetPhase(GrapplePhase.Flying);
    }

    private bool IsHookable(int layer) => (_hookableLayers.value & (1 << layer)) != 0;

    private void OnHookAttached(Vector3 point)
    {
        GrapplePoint = point;

        // Rope length is locked to your distance at attach. This IS the pendulum radius.
        float dist = Vector3.Distance(_rb.position, point);
        _ropeLength = Mathf.Clamp(dist, _minRopeLength, _maxRopeLength);

        Vector3 vel = _rb.linearVelocity;
        Vector3 toAnchor = (point - _rb.position).normalized;

        bool grounded = Physics.Raycast(_rb.position, Vector3.down, _groundCheckDistance, _aimLayers,
            QueryTriggerInteraction.Ignore);

        if (grounded)
        {
            // Hop off the ground and give a sideways start so a standing swing can begin.
            // The kick is perpendicular to the rope, so it never pulls you toward the anchor.
            vel.y = Mathf.Max(vel.y, _attachHop);

            Vector3 camFwd = _playerLook.CameraForward.forward;
            Vector3 kick = Vector3.ProjectOnPlane(camFwd, toAnchor);
            if (kick.sqrMagnitude > 0.001f)
                vel += kick.normalized * _attachForwardKick;
        }

        _rb.linearVelocity = vel;
        SetPhase(GrapplePhase.Grappling);
    }

    private void Release()
    {
        Vector3 vel = _rb.linearVelocity;
        float speed = vel.magnitude;

        if (speed > _releaseMinSpeed)
        {
            vel += vel.normalized * speed * _releaseBoost;
            vel.y += _releaseUpBoost;
            _rb.linearVelocity = vel;
        }

        _hook.Retract();
        SetPhase(GrapplePhase.Flying);
    }

    private void OnHookReturned() => SetPhase(GrapplePhase.Waiting);

    private void SetPhase(GrapplePhase next)
    {
        GrapplePhase previous = CurrentPhase;
        CurrentPhase = next;
        PhaseChanged?.Invoke(previous, next);
    }

    private void OnDisable()
    {
        if (_hook == null) return;

        _hook.ResetToIdle();
        _hook.gameObject.SetActive(false);

        if (CurrentPhase != GrapplePhase.Waiting)
            SetPhase(GrapplePhase.Waiting);
    }

    private void OnDestroy()
    {
        if (_hook != null) Destroy(_hook.gameObject);
    }
}