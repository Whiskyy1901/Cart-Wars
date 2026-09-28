using System.Security.Cryptography;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(ActiveRagdoll))]

public class NPCPatroll : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waypointTolerance = 0.8f;
    [SerializeField] private float _waitTimeWaypoint = 1f;
    [SerializeField] private float _navMeshSampleRadius = 3f;

    private NavMeshAgent _agent;
    private ActiveRagdoll _ragdoll;
    private Animator _animator;

    private int _currentWaypointIndex = 0;
    private float _waitTimer = 0f;
    private bool _isWaiting = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _ragdoll = GetComponent<ActiveRagdoll>();
        _animator = GetComponent<Animator>();

    }

    private void Start()
    {
        if(_waypoints != null && _waypoints.Length >0)
        {
            MoveToCurrentWaypoint();
        }
    }

    private void OnEnable()
    {
        _ragdoll.OnRagdollStart += HandleRagdollStart;
        _ragdoll.OnRecoveryComplete += HandleRecoveryComplete;
    }

     private void OnDisable()
    {
        _ragdoll.OnRagdollStart -= HandleRagdollStart;
        _ragdoll.OnRecoveryComplete -= HandleRecoveryComplete;
    }
    private void MoveToCurrentWaypoint()
    {
        if(_waypoints == null || _waypoints.Length == 0 )
            return;

        if(_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.SetDestination(_waypoints[_currentWaypointIndex].position);
        }
    }

    private void AdvanceToNextWaypoint()
    {
        if(_waypoints == null || _waypoints.Length == 0)
        return;

        _currentWaypointIndex = (_currentWaypointIndex +1) % _waypoints.Length;
        MoveToCurrentWaypoint();
    }

    private void Update()
    {
        if(_ragdoll.isRagdoll || !_agent.enabled || !_agent.isOnNavMesh)
        return;

        if(_animator != null)
        {
            float speed = _agent.velocity.magnitude;
            _animator.SetFloat("Speed", speed);

        }

        if(!_agent.pathPending && _agent.remainingDistance <= _waypointTolerance)
        {
            if(!_isWaiting)
            {
                _isWaiting = true;
                _waitTimer = _waitTimeWaypoint;

            }
            else
            {
                _waitTimer -= Time.deltaTime;
                if(_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    AdvanceToNextWaypoint();
                }
            }
        }
    }

    //event Handlers

    private void HandleRagdollStart()
    {
        _isWaiting = false;
        if(_agent.enabled)
        {
            _agent.enabled = false; //stop it from fighting the ragdoll
        }

    }

    private void HandleRecoveryComplete()
    {
        if(NavMesh.SamplePosition(transform.position, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            _agent.enabled  = true;
            _agent.Warp(hit.position);
            MoveToCurrentWaypoint();

        }

        else
        {
            Debug.LogWarning($"{name}: couldn't find NavMeshnear RecoveryPoint, Patrol NOT resumed", this);
        }
    }

}


