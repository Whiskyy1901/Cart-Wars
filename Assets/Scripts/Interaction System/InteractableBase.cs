using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
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
        OnInteractLogic();
    }
    
    protected virtual void OnInteractLogic() { }
}