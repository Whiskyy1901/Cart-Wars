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

    public bool IsSamePickable(Pickable _pickable)
    {
        if(this._pickable == _pickable)
            return true;
        return false;
    }

    public void Pick()
    {
        _pickable.CmdOnPickUp();
    }

    public void Hold()
    {
        _pickable.OnHold();
    }

    public void Release()
    {
        _pickable.CmdOnRelease();
        ResetData();

    }

    public void ResetData()
    {
        _pickable = null;
    }

}
