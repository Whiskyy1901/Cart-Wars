using UnityEngine;

public class DestroyInteraction : NetworkedInteractableBase
{
    protected override void OnInteractServer()
    {
        base.OnInteractServer();
        Destroy(this.gameObject);
    }
}