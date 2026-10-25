using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeRenderer : MonoBehaviour
{
    private LineRenderer _line;
    private GrappleGun _gun;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _gun = GetComponentInParent<GrappleGun>();

        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.enabled = false;
    }

    private void LateUpdate()
    {
        if (_gun.CurrentPhase == GrappleGun.GrapplePhase.Waiting)
        {
            if (_line.enabled)
                _line.enabled = false;
            return;
        }

        if (!_line.enabled)
            _line.enabled = true;

        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, _gun.HookTransform.position);
    }
}