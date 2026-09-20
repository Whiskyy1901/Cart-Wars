using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

    private Animator _animator;
    private Rigidbody[] _boneRigidBodies;
    private Rigidbody _hipsRigidbody;
    private Coroutine _recoveryCoroutine;

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
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
            _recoveryCoroutine = null;
        }

        SetRagdollState(true);

        if (!simulateHips && _hipsRigidbody != null)
            _hipsRigidbody.isKinematic = true;
    }

    public void BeginRecovery()
    {
        if (_recoveryCoroutine != null)
            StopCoroutine(_recoveryCoroutine);

        _recoveryCoroutine = StartCoroutine(BlendBackCoroutine());
    }

    private IEnumerator BlendBackCoroutine()
    {
        RealignRootToHips();

        List<BoneTransform> startPose = CaptureBoneTransforms();

        foreach (Rigidbody rb in _boneRigidBodies)
            rb.isKinematic = true;

        // Sample the animator's pose to blend towards
        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.Update(0f);
        }

        List<BoneTransform> targetPose = CaptureBoneTransforms();

        // Hold the animator off while we drive the bones ourselves
        if (_animator != null)
            _animator.enabled = false;

        // Restore the ragdoll pose before the first blended frame
        for (int i = 0; i < startPose.Count; i++)
        {
            startPose[i].transform.localPosition = startPose[i].localPosition;
            startPose[i].transform.localRotation = startPose[i].localRotation;
        }

        float elapsed = 0f;
        while (elapsed < _blendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _blendDuration);

            for (int i = 0; i < startPose.Count; i++)
            {
                startPose[i].transform.localPosition = Vector3.Lerp(
                    startPose[i].localPosition,
                    targetPose[i].localPosition,
                    t);

                startPose[i].transform.localRotation = Quaternion.Slerp(
                    startPose[i].localRotation,
                    targetPose[i].localRotation,
                    t);
            }

            yield return null;
        }

        if (_animator != null)
            _animator.enabled = true;

        isRagdoll = false;
        _recoveryCoroutine = null;
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

        Vector3 hipsForward = _hipsTransform.forward;
        hipsForward.y = 0f;

        if (hipsForward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(hipsForward.normalized, Vector3.up);

        transform.position = targetRootPosition;

        // Moving the root drags the hips along, so put them back
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
}