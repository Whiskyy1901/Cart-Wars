using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pickable : NetworkBehaviour, IPickable
{
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private Rigidbody _rb;

    public Rigidbody Rigidbody
    {
        get => _rb;
        set => _rb = value;
    }

    // Synced to all clients.
    [SyncVar(hook = nameof(OnPickedChanged))]
    private bool picked;
    
    [SyncVar(hook = nameof(OnHolderChanged))]
    private NetworkIdentity holder;

    private Collider _collider;
    private NetworkTransformBase _netTransform; // Includes both reliable and unreliable

    private void Start()
    {
        _collider = GetComponent<Collider>();
        _netTransform = GetComponent<NetworkTransformBase>();
    }

    public void OnHold() { }

    public void CmdOnPickUp()
    {
        NetworkIdentity localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        CmdRequestPickUp(localPlayer);
    }

    public void CmdOnRelease()
    {
        NetworkIdentity localPlayer = NetworkClient.localPlayer;
        if (localPlayer == null) return;

        CmdRequestRelease(localPlayer);
    }

    //networked requests

    [Command(requiresAuthority = false)]
    private void CmdRequestPickUp(NetworkIdentity requester)
    {
        if (picked) return;

        picked = true;
        holder = requester;
        _rb.isKinematic = true;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestRelease(NetworkIdentity requester)
    {
        if (!picked) return;

        // Only the current holder may release it
        if (holder != requester) return;

        picked = false;
        holder = null;
        _rb.isKinematic = false;
    }

    // Hooks on server and every client

    private void OnPickedChanged(bool oldValue, bool newValue)
    {
        _rb.isKinematic = newValue;
        
        if (_collider != null)
            _collider.enabled = !newValue;
        if (_netTransform != null)
            _netTransform.enabled = !newValue;
    }

    private void OnHolderChanged(NetworkIdentity oldHolder, NetworkIdentity newHolder)
    {
        if (newHolder != null)
        {
            Transform socket = newHolder.GetComponent<PickableController>()?.HoldPoint;
            transform.SetParent(socket, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            transform.SetParent(null, worldPositionStays: true);
        }
    }
}