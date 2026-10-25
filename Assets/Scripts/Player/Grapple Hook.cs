using UnityEngine;

public class GrappleHook : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float _launchSpeed = 80f;

    private GrappleGun _gun;

    private Vector3 _targetPoint;
    private Vector3 _surfaceNormal;
    private bool _attaches;
    private bool _isLaunched;

    public void Initialize(GrappleGun gun)
    {
        _gun = gun;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (TryGetComponent(out Collider col))
            col.enabled = false;
    }

    public void Launch(Vector3 targetPoint, Vector3 surfaceNormal, bool attaches)
    {
        _targetPoint = targetPoint;
        _surfaceNormal = surfaceNormal;
        _attaches = attaches;

        transform.SetParent(null);

        Vector3 toTarget = targetPoint - transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toTarget);

        _isLaunched = true;
    }

    public void Cancel()
    {
        if (_isLaunched)
            EndLaunch(false);
    }

    public void ResetToRest(Transform parent, Vector3 localPos, Quaternion localRot)
    {
        _isLaunched = false;

        transform.SetParent(parent);
        transform.localPosition = localPos;
        transform.localRotation = localRot;
    }

    private void Update()
    {
        if (!_isLaunched)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _targetPoint, _launchSpeed * Time.deltaTime);

        if ((transform.position - _targetPoint).sqrMagnitude <= 0.0001f)
        {
            if (_attaches)
                transform.rotation = Quaternion.LookRotation(-_surfaceNormal);

            EndLaunch(_attaches);
        }
    }

    private void EndLaunch(bool success)
    {
        _isLaunched = false;
        _gun.OnLaunchFinished(success, transform.position);
    }
}