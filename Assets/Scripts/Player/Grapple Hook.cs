using System;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrappleHook : MonoBehaviour
{
    private enum HookState { Idle, Outbound, Stuck, Returning }

    public event Action<Vector3> Attached;
    public event Action Returned;

    [SerializeField] private float _launchSpeed = 80f;
    [SerializeField] private float _retractionTime = 0.25f;

    private LineRenderer _line;
    private Transform _muzzle;
    private HookState _state;

    private Vector3 _targetPoint;
    private Vector3 _surfaceNormal;
    private bool _attaches;

    private Vector3 _returnStart;
    private float _returnTimer;

    public void Initialize(Transform muzzle)
    {
        _muzzle = muzzle;

        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        //_line.

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        transform.SetParent(null);
    }

    public void Launch(Vector3 targetPoint, Vector3 surfaceNormal, bool attaches)
    {
        _targetPoint = targetPoint;
        _surfaceNormal = surfaceNormal;
        _attaches = attaches;

        Vector3 toTarget = targetPoint - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toTarget);

        _state = HookState.Outbound;
    }

    public void Retract()
    {
        if (_state != HookState.Outbound && _state != HookState.Stuck)
            return;

        _returnStart = transform.position;
        _returnTimer = 0f;
        _state = HookState.Returning;
    }

    public void ResetToIdle()
    {
        _state = HookState.Idle;
    }

    private void Update()
    {
        switch (_state)
        {
            case HookState.Outbound:
                transform.position = Vector3.MoveTowards(transform.position, _targetPoint, _launchSpeed * Time.deltaTime);

                if ((transform.position - _targetPoint).sqrMagnitude <= 0.0001f)
                    OnReachedTarget();
                break;

            case HookState.Returning:
            {
                _returnTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_returnTimer / Mathf.Max(0.01f, _retractionTime));
                transform.position = Vector3.Lerp(_returnStart, _muzzle.position, t);

                if (t >= 1f)
                {
                    _state = HookState.Idle;
                    Returned?.Invoke();
                }
                break;
            }
        }
    }

    private void LateUpdate()
    {
        _line.enabled = _state != HookState.Idle;
    
        if (_state == HookState.Idle)
        {
            transform.SetPositionAndRotation(_muzzle.position, _muzzle.rotation);
            return;
        }
    
        _line.SetPosition(0, _muzzle.position);
        _line.SetPosition(1, transform.position);
    }

    private void OnReachedTarget()
    {
        if (_attaches)
        {
            transform.rotation = Quaternion.LookRotation(-_surfaceNormal);
            _state = HookState.Stuck;
            Attached?.Invoke(transform.position);
        }
        else
        {
            Retract();
        }
    }
}