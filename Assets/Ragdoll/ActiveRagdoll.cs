using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActiveRagdoll : MonoBehaviour
{
    [Header("Recovery Settings")]
    [SerializeField] private Transform _hipsTransform;
    [SerializeField] private float _recoveryDelay = 0.5f;
    [SerializeField] private float _blendDuration = 0.4f;
    [SerializeField] private float _settledVelocityThreshold = 0.1f;

    private Animator _animator;
    private Rigidbody[] _boneRigidBodies;
    private Rigidbody _hipsRigidbody;
    private Coroutine _recoveryCoroutine;

    public bool isRagdoll { get; private set; }

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
        {
            _hipsTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        if (_hipsTransform != null)
        {
            _hipsRigidbody = _hipsTransform.GetComponent<Rigidbody>();
        }

        SetRagdollState(false);
    }

    public void EnableRagdoll()
    {
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
            _recoveryCoroutine = null;
        }

        SetRagdollState(true);
    }

    public void DisableRagdollWithDelay()
    {
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
        }

        _recoveryCoroutine = StartCoroutine(RecoveryCoroutine());
    }

    private IEnumerator RecoveryCoroutine()
    {
        yield return new WaitForSeconds(_recoveryDelay);

        if (_hipsRigidbody != null)
        {
            while (_hipsRigidbody.linearVelocity.sqrMagnitude > _settledVelocityThreshold * _settledVelocityThreshold)
            {
                yield return null;
            }
        }

        RealignRootToHips();

        List<BoneTransform> startPose = CaptureBoneTransforms();

        foreach (Rigidbody rb in _boneRigidBodies)
        {
            rb.isKinematic = true;
        }

        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.Update(0f);
        }

        List<BoneTransform> targetPose = CaptureBoneTransforms();

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
                    t
                );
                startPose[i].transform.localRotation = Quaternion.Slerp(
                    startPose[i].localRotation,
                    targetPose[i].localRotation,
                    t
                );
            }

            if (_animator != null) _animator.enabled = false;
            yield return null;
        }

        if (_animator != null) _animator.enabled = true;
        isRagdoll = false;
        _recoveryCoroutine = null;
    }

    private List<BoneTransform> CaptureBoneTransforms()
    {
        List<BoneTransform> pose = new List<BoneTransform>();
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
        if (Physics.Raycast(targetHipsPosition, Vector3.down, out RaycastHit hit, 10f))
        {
            targetRootPosition.y = hit.point.y;
        }

        Vector3 hipsForward = _hipsTransform.forward;
        hipsForward.y = 0f;

        if (hipsForward.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(hipsForward.normalized, Vector3.up);
        }

        transform.position = targetRootPosition;

        _hipsTransform.position = targetHipsPosition;
        _hipsTransform.rotation = targetHipsRotation;
    }

    private void SetRagdollState(bool state)
    {
        isRagdoll = state;

        //toggle animator
        if (_animator != null)
        {
            _animator.enabled = !state;
        }

        //toggle rigidbody kinetic states
        foreach (Rigidbody rb in _boneRigidBodies)
        {
            rb.isKinematic = !state;
        }
    }
}