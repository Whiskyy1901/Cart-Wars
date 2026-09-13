using UnityEngine;

[CreateAssetMenu(fileName = "PickableInputData", menuName = "InteractionSystem/PickableInputData", order = 0)]
public class PickableInputData : ScriptableObject
{
    private bool _pickClicked;
    private bool _pickHold;
    private bool _pickReleased;
    public bool isPicked => _pickClicked;

    public bool PickClicked
    {
        get => _pickClicked;
        set => _pickClicked = value;
    }
    public bool PickHold
    {
        get => _pickHold;
        set => _pickHold = value;
    }
    public bool PickReleased
    {
        get => _pickReleased;
        set => _pickReleased = value;
    }
}