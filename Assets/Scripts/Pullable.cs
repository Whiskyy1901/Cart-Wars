using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Pullable : NetworkBehaviour
{
    public enum RagdollState : byte { Animated, Ragdoll, Recovering }
    [SerializeField] private NetworkTransformBase _hipsSync;
    [SerializeField] private NetworkTransformBase _rootSync;

    [SyncVar] private NetworkIdentity[] _puller;
    [SyncVar] private List<NetworkIdentity> _pullerList = new List<NetworkIdentity>();
    [SyncVar(hook = nameof(OnStateChanged))] private RagdollState _state;

    private List<SpringJoint> _springJointList = new List<SpringJoint>();
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
        if (_pullerList.Contains(puller)) return false;

        _pullerList.Add(puller);

        if (_serverRecovery != null) { StopCoroutine(_serverRecovery); _serverRecovery = null; }

        Rigidbody hips = _ragdoll.Hips;
        SpringJoint springJoint = hips.gameObject.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.connectedBody = null;
        springJoint.connectedAnchor = puller.transform.position;
        springJoint.minDistance = 0f;
        springJoint.maxDistance = Vector3.Distance(hips.position, puller.transform.position);
        springJoint.spring = spring;
        springJoint.damper = damper;

        _springJointList.Add(springJoint); // stays index-aligned with _pullerList

        SetState(RagdollState.Ragdoll);
        return true;      
    }

    [Server]
    public void StopPull(NetworkIdentity requester)
    {
        int index = _pullerList.IndexOf(requester);
        if (index < 0) return; // requester wasn't actually pulling

        ReleaseInternal(index);
    }

    [Server]
    private void ReleaseInternal(int index)
    {
        if (_springJointList[index] != null) Destroy(_springJointList[index]);

        _springJointList.RemoveAt(index);
        _pullerList.RemoveAt(index);

        if (_pullerList.Count == 0 && _serverRecovery == null)
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
        if (_pullerList == null) return;

        for (int i = _pullerList.Count - 1; i >= 0; i--)
        {
            if (_pullerList[i] == null) { ReleaseInternal(i); continue; } // that puller disconnected/was destroyed
            _springJointList[i].connectedAnchor = _pullerList[i].transform.position;
        }
    }
}