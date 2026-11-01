using System;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    public enum GrapplePhase { Waiting, Flying, Grappling}

    public event Action<GrapplePhase, GrapplePhase> PhaseChanged;

    public GrapplePhase CurrentPhase { get; private set; }
    public Vector3 GrapplePoint { get; private set; }

    public bool IsGrappling => CurrentPhase == GrapplePhase.Grappling;

    [Header("References")]
    [SerializeField] private GrappleHook _hook;
    [SerializeField] private Transform _muzzle;

    [Header("Aim Settings")]
    [SerializeField] private float _grappleRange = 40f;
    [SerializeField] private float _aimRadius = 0.3f;
    [SerializeField] private LayerMask _aimLayers;
    [SerializeField] private LayerMask _hookableLayers;

    [Header("Rope Settings")]
    [SerializeField] private float _reelInAcceleration = 20f;
    [SerializeField] private float _minRopeLength = 1.5f;

    private PlayerInput _input;
    private PlayerLook _playerLook;
    private Rigidbody _rb;

    private float _ropeLength;
    private float _reelInSpeed;

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
                if (fire.WasReleasedThisFrame())
                    _hook.Retract();
                break;

            case GrapplePhase.Grappling:
                if (!fire.IsPressed())
                    BeginRetract();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (!IsGrappling)
            return;

        if (_ropeLength > _minRopeLength)
        {
            _reelInSpeed += _reelInAcceleration * Time.fixedDeltaTime;
            _ropeLength = Mathf.Max(_minRopeLength, _ropeLength - _reelInSpeed * Time.fixedDeltaTime);
        }
        else
        {
            _reelInSpeed = 0f;
        }

        Vector3 toPoint = GrapplePoint - _rb.position;
        float distance = toPoint.magnitude;
        if (distance <= _ropeLength)
            return;

        Vector3 direction = toPoint / distance;
        float inwardSpeed = Vector3.Dot(_rb.linearVelocity, direction);

        // Cancel outward velocity, keep inward, and never move slower than the reel
        _rb.linearVelocity += direction * (Mathf.Max(inwardSpeed, _reelInSpeed) - inwardSpeed);
        _rb.position = GrapplePoint - direction * _ropeLength;
    }

    private void FireHook()
    {
        Transform cam = _playerLook.CameraForward;
        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;

        bool hitSomething = Physics.SphereCast(origin, _aimRadius, direction, out RaycastHit hit,
            _grappleRange, _aimLayers, QueryTriggerInteraction.Ignore);

        // A sphere that starts inside a collider reports distance 0 and no usable point
        if (hitSomething && hit.distance <= 0f)
        {
            hitSomething = Physics.Raycast(origin, direction, out hit,
                _grappleRange, _aimLayers, QueryTriggerInteraction.Ignore);
        }

        bool canAttach = hitSomething && IsHookable(hit.collider.gameObject.layer);
        Vector3 targetPoint = hitSomething ? hit.point : origin + direction * _grappleRange;
        Vector3 surfaceNormal = hitSomething ? hit.normal : -direction;

        _hook.Launch(targetPoint, surfaceNormal, canAttach);
        SetPhase(GrapplePhase.Flying);
    }

    private bool IsHookable(int layer)
    {
        return (_hookableLayers.value & (1 << layer)) != 0;
    }

    private void OnHookAttached(Vector3 point)
    {
        GrapplePoint = point;
        _ropeLength = Mathf.Max(_minRopeLength, Vector3.Distance(_rb.position, point));
        _reelInSpeed = 0f;
        SetPhase(GrapplePhase.Grappling);
    }

    private void OnHookReturned()
    {
        SetPhase(GrapplePhase.Waiting);
    }

    private void BeginRetract()
    {
        _hook.Retract();
        SetPhase(GrapplePhase.Flying);
    }

    private void SetPhase(GrapplePhase next)
    {
        GrapplePhase previous = CurrentPhase;
        CurrentPhase = next;
        PhaseChanged?.Invoke(previous, next);
    }

    private void OnDisable()
    {
        if (_hook == null)
            return;

        _hook.ResetToIdle();
        _hook.gameObject.SetActive(false);

        if (CurrentPhase != GrapplePhase.Waiting)
            SetPhase(GrapplePhase.Waiting);
    }

    private void OnDestroy()
    {
        if (_hook != null)
            Destroy(_hook.gameObject);
    }
}