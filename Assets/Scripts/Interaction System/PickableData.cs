using UnityEngine;

[CreateAssetMenu(fileName = "PickableData", menuName = "InteractionSystem/PickableData")]
public class PickableData : ScriptableObject
{
    private IPickable _pickable;
    public IPickable PickableItem
    {
        get => _pickable;
        set => _pickable = value;
    }

    public bool IsEmpty()
    {
        if (_pickable != null)
            return false;
        return true;
    }

    public bool IsSamePickable(Pickable pickable)
    {
        if((Pickable)_pickable == pickable)
            return true;
        return false;
    }

    public void Pick()
    {
        _pickable.OnPickUp();
    }

    public void Hold()
    {
        _pickable.OnHold();
    }

    public void Release()
    {
        _pickable.OnRelease();
        ResetData();

    }

    public void ResetData()
    {
        _pickable = null;
    }

}
