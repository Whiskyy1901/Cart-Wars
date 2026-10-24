using System;
using UnityEngine;

public class TriggerAnimationInteraction : InteractableBase
{
   private Animator _animator;
   [SerializeField] private string _triggerName;

   private void Start()
   {
      _animator = GetComponent<Animator>();
   }

   protected override void OnInteractLogic()
   {
      base.OnInteractLogic();
      _animator.SetTrigger(_triggerName);
   }
}
