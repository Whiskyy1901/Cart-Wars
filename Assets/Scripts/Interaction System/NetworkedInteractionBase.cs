using Mirror;
using UnityEngine;

public abstract class NetworkedInteractableBase : NetworkBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private bool holdInteract = true;
    [SerializeField] private bool multipleUse = false;
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private string tooltip = "Interact";
    [SerializeField] private string secondarytooltip = "";

    public float HoldDuration => holdDuration;
    public bool HoldInteract => holdInteract;
    public bool MultipleUse => multipleUse;
    public bool IsInteractable => isInteractable;
    public string Tooltip => tooltip;
    public string SecondaryTooltip => secondarytooltip;

    public void OnInteract()
    {
        OnInteractLocal();  //For client side logic like camerashake and playing animations etc
        CmdInteract();
    }
    
    [Command(requiresAuthority = false)]
    private void CmdInteract()
    {
        OnInteractServer();
    }

    // Subclasses override this. Runs server only.
    protected virtual void OnInteractServer() { }
    
    protected virtual void OnInteractLocal() { }

}
