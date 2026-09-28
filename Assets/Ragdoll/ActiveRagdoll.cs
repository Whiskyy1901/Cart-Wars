using UnityEngine;

using System.Collections;

using System.Collections.Generic;

using System.Linq;
using System;





public class ActiveRagdoll : MonoBehaviour

{

    [Header("Recovery Settings")]

    [SerializeField] private Transform _hipsTransform;

    [SerializeField] private float _recoveryDelay = 0.5f;

    [SerializeField] private float _blendDuration = 0.4f;

    [SerializeField] private float _settledVelocityThreshold = 0.1f;



    [Header("Ground Check")]

    [SerializeField] private LayerMask _groundMask = ~0;

    [SerializeField] private float _groundCheckDistance = 10f;



    [SerializeField] private string _getUpBackState = "GetUp_Back";

    [SerializeField] private string _getUpFrontState = "GetUp_Front";
    
    public event Action OnRagdollStart;
    public event Action OnRecoveryComplete;



    private Animator _animator;

    private Rigidbody[] _boneRigidBodies;

    private Rigidbody _hipsRigidbody;

    private Coroutine _recoveryCoroutine;

    private Coroutine _autoRecoverMonitorCoroutine;

    private bool _autoRecover = true;

    private int _recoveryToken = 0;



    public Rigidbody Hips => _hipsRigidbody;

    public bool isRagdoll { get; private set; }



    public float RecoveryDelay => _recoveryDelay;

    public float BlendDuration => _blendDuration;

    public bool IsSettled => _hipsRigidbody == null || _hipsRigidbody.linearVelocity.sqrMagnitude <= _settledVelocityThreshold * _settledVelocityThreshold;



    private struct BoneTransform

    {

        public Transform transform;

        public Vector3 localPosition;

        public Quaternion localRotation;

    }



    private void Awake()

    {

        _animator = GetComponent<Animator>();

        _boneRigidBodies = GetComponentsInChildren<Rigidbody>();



        // Auto-find Hips if not assigned

        if (_hipsTransform == null && _animator != null)

            _hipsTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);



        if (_hipsTransform != null)

            _hipsRigidbody = _hipsTransform.GetComponent<Rigidbody>();



        SetRagdollState(false);

    }

   

    public void EnableRagdoll(bool simulateHips)

    {

        if(_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
            _recoveryCoroutine = null;

        }

        _recoveryToken ++ ;
        SetRagdollState(true);

        if(!simulateHips && _hipsRigidbody != null)
        {
            _hipsRigidbody.isKinematic = true;
        }

        OnRagdollStart?.Invoke();

    }

    private int _activeRecoveryToken;

    public void BeginRecovery()

    {

        if (_recoveryCoroutine != null)

            StopCoroutine(_recoveryCoroutine);


        _activeRecoveryToken = _recoveryToken;
        _recoveryCoroutine = StartCoroutine(BlendBackCoroutine());

    }



    private IEnumerator BlendBackCoroutine()

    {

        bool _isFacingUp = _hipsTransform != null && _hipsTransform.forward.y > 0f;

        string _getUpState = _isFacingUp ? _getUpBackState : _getUpFrontState;



        RealignRootToHips();



        List<BoneTransform> startPose = CaptureBoneTransforms();



        foreach(Rigidbody rb in _boneRigidBodies)

        {

            rb.isKinematic = true;

       

        }



        if(_animator != null)

        {

            _animator.enabled = true;

            _animator.Play(_getUpState, 0 , 0f);

            _animator.Update(0f);

        }



        List<BoneTransform> targetPose = CaptureBoneTransforms();



        if(_animator != null)

        {

            _animator.enabled = false;



        }



        for(int i = 0 ; i < startPose.Count; i ++)

        {

            startPose[i].transform.localPosition = startPose[i].localPosition;

            startPose[i].transform.localRotation = startPose[i].localRotation;



        }



        float elapsed = 0f;

        float t = 0f;

        while(elapsed < _blendDuration)

        {

            elapsed += Time.deltaTime;

            t = Mathf.SmoothStep(0f, 1f, elapsed/BlendDuration);



            for(int i = 0; i < startPose.Count; i++)

            {

                startPose[i].transform.localPosition = Vector3.Lerp(startPose[i].localPosition, targetPose[i].localPosition, t);



                startPose[i].transform.localRotation = Quaternion.Slerp(startPose[i].localRotation, targetPose[i].localRotation, t);



            }



            yield return null;

        }



        if(_animator != null)

        {

            _animator.enabled = true;

        }



        isRagdoll = false;

        _recoveryCoroutine = null;

       



       



    }

    public void AnimEvent_RecoveryComplete()
    {
        if(_activeRecoveryToken != _recoveryToken) return;

        OnRecoveryComplete?.Invoke();
    }



    private List<BoneTransform> CaptureBoneTransforms()

    {

        List<BoneTransform> pose = new List<BoneTransform>(_boneRigidBodies.Length);

        foreach (Rigidbody rb in _boneRigidBodies)

        {

            pose.Add(new BoneTransform

            {

                transform = rb.transform,

                localPosition = rb.transform.localPosition,

                localRotation = rb.transform.localRotation

            });

        }

        return pose;

    }



    private void RealignRootToHips()

    {

        if (_hipsTransform == null) return;



        Vector3 targetHipsPosition = _hipsTransform.position;

        Quaternion targetHipsRotation = _hipsTransform.rotation;



        Vector3 targetRootPosition = targetHipsPosition;

        if (TryGetGroundHeight(targetHipsPosition, out float groundY))

            targetRootPosition.y = groundY;



        // Detect if body is facing up (belly facing sky)

        bool isFacingUp = _hipsTransform.forward.y > 0f;



        // Hips.up represents the spine direction (pelvis to head) on standard rigs.

        // This vector lies flat along the ground whether face-up or face-down.

        Vector3 spineDirection = _hipsTransform.up;

        spineDirection.y = 0f;



        if (spineDirection.sqrMagnitude > 0.001f)

        {

            spineDirection.Normalize();



            // Face-up animations generally get up facing towards the feet (-spineDirection).

            // Face-down animations generally get up facing towards the head (+spineDirection).

            Vector3 facingDirection = isFacingUp ? -spineDirection : spineDirection;



            transform.rotation = Quaternion.LookRotation(facingDirection, Vector3.up);

        }



        transform.position = targetRootPosition;



        // Moving the root moves all child bones; restore hips back to physical ragdoll pose

        _hipsTransform.position = targetHipsPosition;

        _hipsTransform.rotation = targetHipsRotation;

    }



    // Ignores this ragdoll's own colliders so a limb under the hips can't be mistaken for ground

    private bool TryGetGroundHeight(Vector3 from, out float groundY)

    {

        RaycastHit[] hits = Physics.RaycastAll(

            from, Vector3.down, _groundCheckDistance, _groundMask, QueryTriggerInteraction.Ignore);



        float best = float.MaxValue;

        bool found = false;



        foreach (RaycastHit hit in hits.OrderBy(h => h.distance))

        {

            if (hit.collider.transform.IsChildOf(transform)) continue;

            best = hit.point.y;

            found = true;

            break;

        }



        groundY = best;

        return found;

    }



    private void SetRagdollState(bool state)

    {

        isRagdoll = state;



        if (_animator != null)

            _animator.enabled = !state;



        foreach (Rigidbody rb in _boneRigidBodies)

            rb.isKinematic = !state;

    }



    public void ApplyImpact(Vector3 force, Vector3 hitPoint, Transform hitBone = null)

    {

        if(!isRagdoll)

        {

            EnableRagdoll(true);

        }

        else if(_autoRecover)

        {

           

            if (_autoRecoverMonitorCoroutine != null)

                StopCoroutine(_autoRecoverMonitorCoroutine);



            _autoRecoverMonitorCoroutine = StartCoroutine(AutoRecoveryMonitorCoroutine());

        }

        Rigidbody targetRb =(hitBone != null) ? hitBone.GetComponent<Rigidbody>() : _hipsRigidbody;

        if(targetRb == null) targetRb = _hipsRigidbody;

        if(targetRb != null)

        {

            targetRb.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);

        }



        if(_hipsRigidbody != null && targetRb != _hipsRigidbody)

        {

            _hipsRigidbody.AddForce(force * 0.5f, ForceMode.Impulse);

        }

    }

    private IEnumerator AutoRecoveryMonitorCoroutine()

    {

        yield return new WaitForSeconds(_recoveryDelay);



        while (!IsSettled)

        {

            yield return null;

        }



        BeginRecovery();

    }

}