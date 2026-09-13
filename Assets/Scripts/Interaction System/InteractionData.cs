using UnityEngine;

[CreateAssetMenu(fileName = "Interaction Data", menuName = "InteractionSystem/InteractionData")]
public class InteractionData : ScriptableObject
{
    private IInteractable[] _interactables;
    


    public IInteractable[] InteractableBases
    {
        get => _interactables;
        set => _interactables = value;
    }

    public void Interact()
    {
        foreach (var interactable in _interactables)
        {
            interactable.OnInteract();
        }
        ResetData();
    }

    public bool IsSameInteractable(IInteractable[] _newInteractables)
    {
        if (_interactables == null || _newInteractables == null)
            return false;

        if (_interactables.Length != _newInteractables.Length)
            return false;

        for (int i = 0; i < _interactables.Length; i++)
        {
            if (_interactables[i] != _newInteractables[i])
                return false;
        }

        return true;
    }

    public bool IsEmpty() => _interactables == null || _interactables.Length == 0;

    public bool AllInteractable()
    {
        foreach (var interactable in _interactables)
        {
            if (!interactable.IsInteractable)
                return false;
        }
        return true;
    }

    public float MaxHoldDuration()
    {
        float _max = 0f;
        foreach (var interactable in _interactables)
        {
            if (interactable.HoldDuration > _max)
                _max = interactable.HoldDuration;
        }
        return _max;
    }

    public bool AnyHoldInteract()
    {
        foreach (var interactable in _interactables)
        {
            if (interactable.HoldInteract)
                return true;
        }
        return false;
    }

    public void ResetData() => _interactables = null;
}