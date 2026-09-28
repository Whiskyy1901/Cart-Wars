using System;
using UnityEngine;

public class PlayerHealth : HealthBase
{
    [SerializeField] private float _stunDuration = 2f;
    [SerializeField] private float _recoveryHeal = 5f;
    private PlayerInput _input;
    private Rigidbody _rb;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();
    }

    protected override void DeathSequence()
    {
        base.DeathSequence();
        //disable input
        _input._takingInput = false;
        Quaternion initialRotation = transform.rotation;
        _rb.freezeRotation = false;
        //move cam to third person view         
        Awaitable.WaitForSecondsAsync(_stunDuration); // wait for stunDuration
        //reset everything
        Quaternion finalRotation = transform.rotation;
        _rb.rotation = Quaternion.Lerp(initialRotation, finalRotation, 1);
        _input._takingInput = true;
        _rb.freezeRotation = true;
        //Heal player
        Heal(_recoveryHeal);
    }
}
