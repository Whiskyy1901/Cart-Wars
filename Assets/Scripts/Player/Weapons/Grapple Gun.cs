using Mirror;
using UnityEngine;

public class GrappleGun : NetworkBehaviour
{
    private PlayerInput _input;
    private PlayerLook _playerLook;
    private LineRenderer _line;
    private Rigidbody _playerRb;
    private SpringJoint _springJoint;
    private NetworkIdentity _selfIdentity;

    [Header("Gun Settings")]
    [SerializeField] private float _grappleRange = 20f;
    [SerializeField] private float _grappleRayRadius = 0.5f;
    [SerializeField] private float _grappleCooldown = 2f;
    [SerializeField] private Transform _shootPos;
    [SerializeField] [Tooltip("No Network Identity")] private GameObject _hookObject;
    [SerializeField] private LayerMask _hookAttachLayers;

    [Header("Grapple Settings")]
    [SerializeField] private float _spring = 400f;
    [SerializeField] private float _damper = 20f;
    [SerializeField] private float _massScale = 1f;
    [SerializeField] private float _reelSpeed = 5f;
    [SerializeField] private float _minReelDistance = 1f;
    private bool _isGrappling;

    [Header("Pull Settings")]
    [SerializeField] private LayerMask _pullableLayer;
    [SerializeField] private float _pullSpring = 400f;
    [SerializeField] private float _pullDamper = 20f;
    private bool _isPulling;

    // ---- Synced state (remote players read these) ----
    [SyncVar] private Vector3 _syncAnchor;
    [SyncVar] private Vector3 _syncNormal;
    [SyncVar] private bool _syncIsPull;
    [SyncVar] private NetworkIdentity _syncTarget;
    [SyncVar] private Vector3 _syncTargetOffset;   // attach point in the target's GrabPoint local space
    [SyncVar] private Vector3 _syncTargetNormal;   // hit normal in the target's GrabPoint local space
    // Keep this LAST: its hook fires after the values above have been applied.
    [SyncVar(hook = nameof(OnSyncIsGrapplingChanged))] private bool _syncIsGrappling;

    // ---- Local-only state (local player) ----
    private float _timeFromLastShot;
    private Vector3 _localAnchor;
    private Pullable _localPullTarget;
    private Vector3 _localTargetOffset;
    private Vector3 _localTargetNormal;
    private bool _localActive;

    // ---- Remote-only cache ----
    private Pullable _remoteTarget;

    private GameObject _hookVisual;

    public bool IsGrappling => isLocalPlayer ? _localActive : _syncIsGrappling;
    public bool MoveDisableGrapple => _isGrappling;

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInput>();
        _playerLook = GetComponentInParent<PlayerLook>();
        _playerRb = GetComponentInParent<Rigidbody>();
        _line = GetComponent<LineRenderer>();
        _selfIdentity = GetComponentInParent<NetworkIdentity>();
        _line.positionCount = 0;
    }

    private void Update()
    {
        if (isLocalPlayer)
            HandleLocalInput();
    }

    // Rope and hook are drawn after everything has moved this frame
    private void LateUpdate()
    {
        UpdateRope();
    }

    // -------------------------------------------------------------------------
    // Local input
    // -------------------------------------------------------------------------

    private void HandleLocalInput()
    {
        _timeFromLastShot += Time.deltaTime;

        if (_input.ShootAction.WasPressedThisFrame() && _timeFromLastShot >= _grappleCooldown && _springJoint == null)
        {
            Ray ray = new Ray(_playerLook.Camera.transform.position, _playerLook.CameraForward.forward);
            _timeFromLastShot = 0;

            Pullable pullable = null;
            RaycastHit pullHit = default;
            if (Physics.SphereCast(ray, _grappleRayRadius, out pullHit, _grappleRange, _pullableLayer))
                pullable = pullHit.collider.GetComponentInParent<Pullable>();

            if (pullable != null)
                StartPull(pullable, pullHit.point, pullHit.normal);
            else if (Physics.SphereCast(ray, _grappleRayRadius, out RaycastHit hit, _grappleRange, _hookAttachLayers))
                StartGrapple(hit.point, hit.normal);
        }

        if (_input.ShootAction.WasReleasedThisFrame())
        {
            if (_isGrappling) StopGrapple();
            else if (_isPulling) StopPull();
        }

        // Target despawned or was destroyed mid-pull
        if (_isPulling && _localPullTarget == null)
            StopPull();

        if (_springJoint != null && _springJoint.maxDistance > _minReelDistance)
            _springJoint.maxDistance -= _reelSpeed * Time.deltaTime;
    }

    // -------------------------------------------------------------------------
    // Static grapple (swing)
    // -------------------------------------------------------------------------

    private void StartGrapple(Vector3 point, Vector3 normal)
    {
        _localActive = true;
        _isGrappling = true;
        _localAnchor = point;
        CreateJoint(point);
        ShowHook(point, normal);

        CmdStartGrapple(point, normal);
    }

    private void StopGrapple()
    {
        _localActive = false;
        _isGrappling = false;
        DestroyJoint();
        HideHook();
        CmdStopGrapple();
    }

    private void CreateJoint(Vector3 anchor)
    {
        _springJoint = _playerRb.gameObject.AddComponent<SpringJoint>();
        _springJoint.autoConfigureConnectedAnchor = false;
        _springJoint.connectedBody = null;
        _springJoint.connectedAnchor = anchor;

        _springJoint.minDistance = 0f;
        _springJoint.maxDistance = Vector3.Distance(_playerRb.position, anchor);
        _springJoint.spring = _spring;
        _springJoint.damper = _damper;
        _springJoint.massScale = _massScale;
    }

    private void DestroyJoint()
    {
        if (_springJoint != null)
            Destroy(_springJoint);
    }

    [Command]
    private void CmdStartGrapple(Vector3 point, Vector3 normal)
    {
        if (_syncIsGrappling) return; // ignore duplicate requests

        _syncAnchor = point;
        _syncNormal = normal;
        _syncIsPull = false;
        _syncIsGrappling = true;      // set last, this triggers the hook on clients
    }

    [Command]
    private void CmdStopGrapple()
    {
        _syncIsGrappling = false;
    }

    // -------------------------------------------------------------------------
    // Pull
    // -------------------------------------------------------------------------

    private void StartPull(Pullable target, Vector3 point, Vector3 normal)
    {
        _localActive = true;
        _isPulling = true;
        _localPullTarget = target;

        Transform grab = target.GrabPoint;
        _localTargetOffset = grab.InverseTransformPoint(point);
        _localTargetNormal = grab.InverseTransformDirection(normal);

        CmdStartPull(target.netIdentity, _localTargetOffset, _localTargetNormal);
    }

    private void StopPull()
    {
        _localActive = false;
        _isPulling = false;
        _localPullTarget = null;
        HideHook();
        CmdStopPull();
    }

    [Command]
    private void CmdStartPull(NetworkIdentity targetIdentity, Vector3 localOffset, Vector3 localNormal)
    {
        if (!_syncIsGrappling &&
            targetIdentity != null &&
            targetIdentity.TryGetComponent(out Pullable pullable) &&
            pullable.StartPull(_pullSpring, _pullDamper, _selfIdentity))
        {
            _syncTarget = targetIdentity;
            _syncTargetOffset = localOffset;
            _syncTargetNormal = localNormal;
            _syncIsPull = true;
            _syncIsGrappling = true;  // last
            return;
        }

        TargetPullRejected();
    }

    [Command]
    private void CmdStopPull()
    {
        ReleaseServerPull();
    }

    [Server]
    private void ReleaseServerPull()
    {
        if (_syncTarget != null && _syncTarget.TryGetComponent(out Pullable pullable))
            pullable.StopPull(_selfIdentity);

        _syncTarget = null;
        _syncIsPull = false;
        _syncIsGrappling = false;
    }

    [TargetRpc]
    private void TargetPullRejected()
    {
        _localActive = false;
        _isPulling = false;
        _localPullTarget = null;
        HideHook();
    }

    // -------------------------------------------------------------------------
    // Network lifecycle and sync hooks
    // -------------------------------------------------------------------------

    // Puller disconnected or was destroyed mid-pull: free the NPC
    public override void OnStopServer()
    {
        ReleaseServerPull();
    }

    private void OnSyncIsGrapplingChanged(bool oldValue, bool newValue)
    {
        // The local player drives their own visuals
        if (isLocalPlayer) return;

        _remoteTarget = null;

        if (newValue)
        {
            // Pull hooks are placed every frame in UpdateRope
            if (!_syncIsPull) ShowHook(_syncAnchor, _syncNormal);
        }
        else
        {
            HideHook();
        }
    }

    // Covers late joiners in case the hook doesn't fire for the initial state
    public override void OnStartClient()
    {
        if (!isLocalPlayer && _syncIsGrappling && !_syncIsPull)
            ShowHook(_syncAnchor, _syncNormal);
    }

    public override void OnStopClient()
    {
        HideHook();
        DestroyJoint();
        _line.positionCount = 0;
    }

    // -------------------------------------------------------------------------
    // Visuals
    // -------------------------------------------------------------------------

    private void ShowHook(Vector3 pos, Vector3 normal)
    {
        Quaternion rot = normal.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        if (_hookVisual == null)
            _hookVisual = Instantiate(_hookObject, pos, rot);
        else
            _hookVisual.transform.SetPositionAndRotation(pos, rot);
    }

    private void HideHook()
    {
        if (_hookVisual != null)
            Destroy(_hookVisual);
    }

    private Pullable ResolveRemoteTarget()
    {
        if (_syncTarget == null) return null;   // not spawned on this client yet

        if (_remoteTarget == null || _remoteTarget.netIdentity != _syncTarget)
            _remoteTarget = _syncTarget.GetComponent<Pullable>();

        return _remoteTarget;
    }

    // World-space attach point and normal on the pulled target, recomputed every frame
    private bool TryGetPullAnchor(out Vector3 pos, out Vector3 normal)
    {
        Pullable target = isLocalPlayer ? _localPullTarget : ResolveRemoteTarget();
        if (target == null)
        {
            pos = default;
            normal = Vector3.forward;
            return false;
        }

        Transform grab = target.GrabPoint;
        pos = grab.TransformPoint(isLocalPlayer ? _localTargetOffset : _syncTargetOffset);
        normal = grab.TransformDirection(isLocalPlayer ? _localTargetNormal : _syncTargetNormal);
        return true;
    }

    private void UpdateRope()
    {
        bool active = isLocalPlayer ? _localActive : _syncIsGrappling;

        if (!active)
        {
            if (_line.positionCount != 0) _line.positionCount = 0;
            return;
        }

        bool isPull = isLocalPlayer ? _isPulling : _syncIsPull;
        Vector3 anchor;

        if (isPull)
        {
            if (!TryGetPullAnchor(out anchor, out Vector3 normal))
            {
                if (_line.positionCount != 0) _line.positionCount = 0;
                return;
            }

            ShowHook(anchor, normal);   // creates the hook once, then moves it every frame
        }
        else
        {
            anchor = isLocalPlayer ? _localAnchor : _syncAnchor;
        }

        _line.positionCount = 2;
        _line.SetPosition(0, _shootPos.position);
        _line.SetPosition(1, anchor);
    }
}