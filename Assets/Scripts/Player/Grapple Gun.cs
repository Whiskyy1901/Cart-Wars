using System;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    public enum GrapplePhase
    {
        Waiting,
        Launching,
        Grappling,
        Retracting
    }

    public event Action<GrapplePhase, GrapplePhase> PhaseChanged;

    public GrapplePhase CurrentPhase { get; private set; }
    public Vector3 GrapplePoint { get; private set; }

    public bool IsGrappling => CurrentPhase == GrapplePhase.Grappling;
    public Transform HookTransform => _hook.transform;

    [Header("References")]
    [SerializeField] private GrappleHook _hook;

    [Header("Aim Settings")]
    [SerializeField] private float _grappleRange = 40f;
    [SerializeField] private float _aimRadius = 0.3f;
    [Tooltip("Everything the aim cast can hit. Exclude the player's own layer.")]
    [SerializeField] private LayerMask _aimLayers;
    [Tooltip("Subset of surfaces the hook is allowed to attach to.")]
    [SerializeField] private LayerMask _hookableLayers;

    [Header("Rope Settings")]
    [SerializeField] private float _reelInAcceleration = 20f;
    [SerializeField] private float _minRopeLength = 1.5f;
    [SerializeField] private float _retractionTime = 0.25f;

    private PlayerInput _input;
    private PlayerLook _playerLook;
    private Rigidbody _playerRb;

    private Vector3 _hookRestLocalPos;
    private Quaternion _hookRestLocalRot;

    private float _ropeLength;
    private bool _isRopeInTension;
    private bool _isReelingIn;
    private float _reelInSpeed;
    private float _retractTimer;

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInput>();
        _playerLook = GetComponentInParent<PlayerLook>();
        _playerRb = GetComponentInParent<Rigidbody>();

        _hookRestLocalPos = _hook.transform.localPosition;
        _hookRestLocalRot = _hook.transform.localRotation;

        _hook.Initialize(this);
        CurrentPhase = GrapplePhase.Waiting;
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

            case GrapplePhase.Launching:
                if (fire.WasReleasedThisFrame())
                    _hook.Cancel();
                break;

            case GrapplePhase.Grappling:
                if (!fire.IsPressed())
                    BeginRetract();
                break;

            case GrapplePhase.Retracting:
                UpdateRetraction();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (CurrentPhase != GrapplePhase.Grappling)
            return;

        Vector3 toPoint = GrapplePoint - _playerRb.position;
        Vector3 direction = toPoint.normalized;
        _isRopeInTension = toPoint.sqrMagnitude > _ropeLength * _ropeLength;

        ApplyGrappleForces(direction);

        if (_isRopeInTension && Vector3.Dot(_playerRb.linearVelocity, toPoint) <= 0f)
            TugPlayer(direction);

        if (_isReelingIn && _ropeLength > _minRopeLength)
        {
            _reelInSpeed += _reelInAcceleration * Time.fixedDeltaTime;
            _ropeLength -= _reelInSpeed * Time.fixedDeltaTime;
        }
        else
        {
            _isReelingIn = false;
            _ropeLength = _minRopeLength;
        }
    }

    private void FireHook()
    {
        Transform cam = _playerLook.CameraForward;
        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;

        bool hitSomething = Physics.SphereCast(origin, _aimRadius, direction, out RaycastHit hit,
            _grappleRange, _aimLayers, QueryTriggerInteraction.Ignore);

        // SphereCast gives no valid point if it starts inside a collider
        if (hitSomething && hit.distance <= 0f)
        {
            hitSomething = Physics.Raycast(origin, direction, out hit,
                _grappleRange, _aimLayers, QueryTriggerInteraction.Ignore);
        }

        bool canAttach = hitSomething && IsHookable(hit.collider.gameObject.layer);
        Vector3 targetPoint = hitSomething ? hit.point : origin + direction * _grappleRange;
        Vector3 surfaceNormal = hitSomething ? hit.normal : -direction;

        _hook.Launch(targetPoint, surfaceNormal, canAttach);
        SetPhase(GrapplePhase.Launching);
    }

    private bool IsHookable(int layer)
    {
        return (_hookableLayers.value & (1 << layer)) != 0;
    }

    public void OnLaunchFinished(bool success, Vector3 point)
    {
        if (CurrentPhase != GrapplePhase.Launching)
            return;

        GrapplePoint = point;

        if (success)
            StartGrapple();
        else
            BeginRetract();
    }

    private void StartGrapple()
    {
        _ropeLength = Mathf.Max(_minRopeLength, Vector3.Distance(_playerRb.position, GrapplePoint));
        _isReelingIn = true;
        _reelInSpeed = 0f;
        _isRopeInTension = false;

        SetPhase(GrapplePhase.Grappling);
    }

    private void ApplyGrappleForces(Vector3 direction)
    {
        if (!_isRopeInTension)
            return;

        float theta = Vector3.Angle(direction, Vector3.up) * Mathf.Deg2Rad;
        float centripetal = _playerRb.linearVelocity.sqrMagnitude / _ropeLength;
        float tension = Mathf.Max(0f, centripetal + Physics.gravity.magnitude * Mathf.Cos(theta));

        _playerRb.AddForce(direction * tension, ForceMode.Acceleration);

        if (_isReelingIn)
            _playerRb.AddForce(direction * _reelInAcceleration, ForceMode.Acceleration);
    }

    private void TugPlayer(Vector3 direction)
    {
        _playerRb.linearVelocity = Vector3.ProjectOnPlane(_playerRb.linearVelocity, direction);
        _playerRb.position = GrapplePoint - direction * _ropeLength;
    }

    private void BeginRetract()
    {
        _retractTimer = 0f;
        _isRopeInTension = false;
        SetPhase(GrapplePhase.Retracting);
    }

    private void UpdateRetraction()
    {
        _retractTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_retractTimer / Mathf.Max(0.01f, _retractionTime));

        Vector3 restWorldPos = transform.TransformPoint(_hookRestLocalPos);
        _hook.transform.position = Vector3.Lerp(GrapplePoint, restWorldPos, t);

        if (t >= 1f)
            ResetGun();
    }

    private void ResetGun()
    {
        _hook.ResetToRest(transform, _hookRestLocalPos, _hookRestLocalRot);
        _retractTimer = 0f;
        SetPhase(GrapplePhase.Waiting);
    }

    private void SetPhase(GrapplePhase next)
    {
        GrapplePhase previous = CurrentPhase;
        CurrentPhase = next;
        PhaseChanged?.Invoke(previous, next);
    }

    private void OnDisable()
    {
        if (_hook != null && CurrentPhase != GrapplePhase.Waiting)
            ResetGun();
    }

    private void OnDestroy()
    {
        // Hook is unparented while flying/stuck, destroy it with the player
        if (_hook != null && _hook.transform.parent != transform)
            Destroy(_hook.gameObject);
    }
}