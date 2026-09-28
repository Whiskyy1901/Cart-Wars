using System;
using UnityEngine;

public class RagdollHealth : HealthBase
{
    private ActiveRagdoll _ragdoll;

    private void Awake()
    {
        _ragdoll = GetComponentInChildren<ActiveRagdoll>();
    }

    protected override void DeathSequence()
    {
        base.DeathSequence();
        //_ragdoll._autoRecover = false;
        _ragdoll.EnableRagdoll(true);
    }
}
