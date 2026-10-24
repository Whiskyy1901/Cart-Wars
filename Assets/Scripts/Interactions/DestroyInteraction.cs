using UnityEngine;

public class DestroyInteraction : InteractableBase
{
    protected override void OnInteractLogic()
    {
        base.OnInteractLogic();
        Destroy(this.gameObject);
    }
}
