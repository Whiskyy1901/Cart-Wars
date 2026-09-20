using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class Pullable : NetworkBehaviour
{
    public enum RagdollState : byte { Animated, Ragdoll, Recovering }

    [SerializeField] private NetworkTransformBase _hipsSync;
    [SerializeField] private NetworkTransformBase _rootSync;

    [SyncVar] private NetworkIdentity _puller;
    [SyncVar(hook = nameof(OnStateChanged))] private RagdollState _state;

    private SpringJoint _springJoint;
    private ActiveRagdoll _ragdoll;
    private Coroutine _serverRecovery;
    private RagdollState _applied = RagdollState.Animated;

    public Transform GrabPoint => _ragdoll.Hips.transform;
    public event Action<RagdollState> StateChanged;   // NPC AI/NavMeshAgent listens on the server

    private void Awake() => _ragdoll = GetComponentInChildren<ActiveRagdoll>();

    public override void OnStartClient()
    {
        if (_hipsSync != null) _hipsSync.enabled = false;
        ApplyState(_state);   // late joiners
    }

    [Server]
    public bool StartPull(float spring, float damper, NetworkIdentity puller)
    {
        if (_puller != null) return false;
        _puller = puller;

        if (_serverRecovery != null) { StopCoroutine(_serverRecovery); _serverRecovery = null; }

        Rigidbody hips = _ragdoll.Hips;
        _springJoint = hips.gameObject.AddComponent<SpringJoint>();
        _springJoint.autoConfigureConnectedAnchor = false;
        _springJoint.connectedBody = null;
        _springJoint.connectedAnchor = puller.transform.position;
        _springJoint.minDistance = 0f;
        _springJoint.maxDistance = Vector3.Distance(hips.position, puller.transform.position);
        _springJoint.spring = spring;
        _springJoint.damper = damper;

        SetState(RagdollState.Ragdoll);
        return true;
    }

    [Server]
    public void StopPull(NetworkIdentity requester)
    {
        if (_puller != requester) return;   // resolves races between two pullers
        ReleaseInternal();
    }

    [Server]
    private void ReleaseInternal()
    {
        if (_springJoint != null) Destroy(_springJoint);
        _puller = null;
        _serverRecovery = StartCoroutine(ServerRecovery());
    }

    [Server]
    private IEnumerator ServerRecovery()
    {
        yield return new WaitForSeconds(_ragdoll.RecoveryDelay);
        while (!_ragdoll.IsSettled) yield return null;

        SetState(RagdollState.Recovering);
        yield return new WaitForSeconds(_ragdoll.BlendDuration + 0.1f);
        SetState(RagdollState.Animated);
        _serverRecovery = null;
    }

    [Server]
    private void SetState(RagdollState s)
    {
        _state = s;
        ApplyState(s);   // dedicated server doesn't run SyncVar hooks, so apply explicitly
    }

    private void OnStateChanged(RagdollState oldS, RagdollState newS) => ApplyState(newS);

    // Idempotent: host runs both the explicit call and the hook
    private void ApplyState(RagdollState s)
    {
        if (s == _applied) return;
        _applied = s;

        if (_hipsSync != null) _hipsSync.enabled = s == RagdollState.Ragdoll;
        if (_rootSync != null && !isServer) _rootSync.enabled = s == RagdollState.Animated;

        if (s == RagdollState.Ragdoll) _ragdoll.EnableRagdoll(simulateHips: isServer);
        else if (s == RagdollState.Recovering) _ragdoll.BeginRecovery();

        StateChanged?.Invoke(s);
    }

    [ServerCallback]
    private void FixedUpdate()
    {
        if (_springJoint == null) return;
        if (_puller == null) { ReleaseInternal(); return; }   // puller disconnected or was destroyed
        _springJoint.connectedAnchor = _puller.transform.position;
    }
}