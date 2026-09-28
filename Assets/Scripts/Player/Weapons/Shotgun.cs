using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class Shotgun : NetworkBehaviour
{
    private PlayerInput _input;
    private PlayerLook _playerLook;
    private NetworkIdentity _self;
    
    [Header("Shotgun Settings")]
    [SerializeField] private int _pelletCount = 8;
    [SerializeField] private int _pelletDamage = 8;
    [SerializeField] private float _spreadAngle = 5f;
    [SerializeField] private float _range = 20f;
    [SerializeField] private float _cooldown = 1f;
    private float _timeFromLastShot;
    [SerializeField] private float _knockbackForce = 40f;
    [SerializeField] private float _upwardModifier = 0.2f;
    
    [SerializeField] private LayerMask _hitMask = ~0;
    [SerializeField] private Transform _shootPos;
    
    [SerializeField] private AnimationCurve _damageFalloff;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private LineRenderer _tracerPrefab;
    [SerializeField] private GameObject _impactEffectPrefab;
    [SerializeField] private float _tracerDuration= 0.04f;

    [SerializeField] private CameraShake _cameraShake;
    [SerializeField] private float _shakeTrauma = 0.6f;

    private List<HitData> _hitDataList = new List<HitData>();
    private struct HitData
    {
        public NetworkIdentity Identity;
        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Direction;
        public float Distance;
    }

    private void Awake()
    {
        _input = GetComponentInParent<PlayerInput>();
        _playerLook = GetComponentInParent<PlayerLook>();
        _self = transform.root.GetComponent<NetworkIdentity>();

    }
    private void Update()
    {
        if(!isLocalPlayer)
            return;
        
        _timeFromLastShot += Time.deltaTime;
        if(_input.RightShootAction.WasPressedThisFrame() && _timeFromLastShot >= _cooldown)
        {
            Fire();
            _timeFromLastShot = 0;
        }
    }
    
    public void Fire()
    {
        if(_muzzleFlash != null)
            _muzzleFlash.Play();

        if(_cameraShake != null)
        {
            _cameraShake.AddTrauma(_shakeTrauma);
        }
        
        for (int i = 0; i < _pelletCount; i++)
        {
            Vector3 direction = GetSpreadDirection(_playerLook.CameraForward.forward, _spreadAngle);

            if (Physics.Raycast(_playerLook.Camera.transform.position, direction, out RaycastHit hit, _range, _hitMask))
            {
                if (hit.collider.transform.root.TryGetComponent(out NetworkIdentity identity) && identity != _self)
                {
                    HitData data = new HitData
                    {
                        Identity = hit.collider.transform.root.GetComponentInChildren<NetworkIdentity>(),
                        Point = hit.point,
                        Normal = hit.normal,
                        Direction = hit.collider.transform.position - this.gameObject.transform.position,
                        Distance = Vector3.Distance(this.gameObject.transform.root.position,
                            hit.collider.gameObject.transform.position)
                    };
                    _hitDataList.Add(data);
                }
            }
        }
        
        CmdHit(_hitDataList.ToArray());
        _hitDataList.Clear();
    }

    private Vector3 GetSpreadDirection(Vector3 forward, float maxAngle)
    {
        Vector2 randomCircle = Random.insideUnitCircle * maxAngle;

        Quaternion spreadRotation = Quaternion.Euler(randomCircle.x, randomCircle.y, 0f);
        return spreadRotation * forward;
    }

    [Command]
    void CmdHit(HitData[] hitDataList)
    {
        // Network transform has server authority so that positions of shot objects can be synced across all clients.
        // Passing struct because server does not have access to hit.transform and thus cannot access components, therefore we give Network Identity.
        foreach (var hitData in hitDataList)
        {
            float distanceFactor = hitData.Distance / _range;
            float finalDamage = _pelletDamage * _damageFalloff.Evaluate(distanceFactor);
            
            if (hitData.Identity.TryGetComponent<HealthBase>(out var target))
                 target.Damage(finalDamage);
            
            Vector3 targetDirection = hitData.Direction;
                
            Vector3 blastDirection = (targetDirection + (Vector3.up * _upwardModifier)).normalized;
            Vector3 impulseForce = blastDirection * _knockbackForce;

            ActiveRagdoll ragdoll = hitData.Identity.GetComponentInChildren<ActiveRagdoll>();
            if (ragdoll != null)
            {
                // Using ClientRpc to send this information to all clients so that they can handle their on ragdoll simulation.
                RpcApplyRagdollImpact(hitData.Identity, impulseForce, hitData.Point);
            }
            else if (hitData.Identity.GetComponent<Rigidbody>() != null && !hitData.Identity.transform.gameObject.CompareTag("Player"))
            {
                hitData.Identity.GetComponent<Rigidbody>().AddForceAtPosition(impulseForce, hitData.Point, ForceMode.Impulse);
            }
            else if (hitData.Identity.GetComponent<Rigidbody>() != null && hitData.Identity.transform.gameObject.CompareTag("Player"))
            {
                TargetApplyPlayerForce(hitData.Identity, impulseForce);
            }
        }   
        
        _hitDataList.Clear();
    }
    [TargetRpc]
    void TargetApplyPlayerForce(NetworkIdentity targetIdentity, Vector3 impulseForce)
    {
        // add logic so that you cannot shoot yourself
        if (targetIdentity == null)
            return;
        Rigidbody rb = targetIdentity.GetComponent<Rigidbody>();
        rb.AddForce(impulseForce, ForceMode.Impulse);
    }
    
    [ClientRpc]
    void RpcApplyRagdollImpact(NetworkIdentity targetIdentity, Vector3 impulseForce, Vector3 hitPoint)
    {
        if (targetIdentity == null)
            return;

        ActiveRagdoll ragdoll = targetIdentity.GetComponentInChildren<ActiveRagdoll>();
        if (ragdoll != null)
        {
            ragdoll.ApplyImpact(impulseForce, hitPoint, targetIdentity.transform);
        }
    }
    
    private IEnumerator SpawnTracer(Vector3 start, Vector3 end)
    {
        LineRenderer tracer = Instantiate(_tracerPrefab, start, Quaternion.identity);
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);

        yield return new WaitForSeconds(_tracerDuration);

        Destroy(tracer.gameObject);
    }

}