using UnityEngine;

[DefaultExecutionOrder(100)]
public class CameraSwingFX : MonoBehaviour
{
    [SerializeField] private GrappleGun _gun;
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _rollPivot;   // empty parent of the camera

    [Header("FOV")]
    [SerializeField] private float _speedFov = 18f;
    [SerializeField] private float _attachKick = 4f;
    [SerializeField] private float _releaseKick = 10f;
    [SerializeField] private float _minSpeed = 8f;
    [SerializeField] private float _maxSpeed = 45f;

    [Header("Tilt")]
    [SerializeField] private float _maxRoll = 9f;
    [SerializeField] private float _rollSmoothing = 8f;

    private Rigidbody _rb;
    private float _baseFov;
    private float _kick;
    private float _roll;

    private void Awake()
    {
        _rb = _gun.GetComponentInParent<Rigidbody>();
        _baseFov = _camera.fieldOfView;
        _gun.PhaseChanged += OnPhaseChanged;
    }

    private void OnDestroy()
    {
        if (_gun != null) _gun.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(GrappleGun.GrapplePhase prev, GrappleGun.GrapplePhase next)
    {
        if (next == GrappleGun.GrapplePhase.Grappling) _kick += _attachKick;
        else if (prev == GrappleGun.GrapplePhase.Grappling) _kick += _releaseKick;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 vel = _rb.linearVelocity;
        float speed = vel.magnitude;
        bool swinging = _gun.IsGrappling;

        _kick = Mathf.Lerp(_kick, 0f, 1f - Mathf.Exp(-6f * dt));

        float speedT = swinging ? Mathf.InverseLerp(_minSpeed, _maxSpeed, speed) : 0f;
        float targetFov = _baseFov + speedT * _speedFov + _kick;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, 1f - Mathf.Exp(-10f * dt));

        float lateral = Vector3.Dot(vel, _camera.transform.right);
        float targetRoll = swinging ? Mathf.Clamp(-lateral / 20f, -1f, 1f) * _maxRoll : 0f;
        _roll = Mathf.Lerp(_roll, targetRoll, 1f - Mathf.Exp(-_rollSmoothing * dt));

        if (_rollPivot != null)
            _rollPivot.localRotation = Quaternion.Euler(0f, 0f, _roll);
    }
    
}
