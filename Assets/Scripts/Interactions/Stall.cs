using UnityEngine;

public class Stall : InteractableBase
{
    [Header("Stock Information")]
    [Tooltip("Just for text display")]
    [SerializeField] private string _itemName;
    [SerializeField] private int _itemID;
    private int _currentQty = 0;
    private int _decidedQty;

    private OrderHandler _handler;

    public string ItemName => _itemName;
    public int ItemID => _itemID;
    public int CurrentQty => _currentQty;
    public int DecidedQty => _decidedQty;
    public bool IsCorrect => _currentQty == _decidedQty;

    public void Initialization(int qty)
    {
        _decidedQty = qty;
        _currentQty = 0;
    }

    private void Start() => _handler = FindAnyObjectByType<OrderHandler>();

    protected override void OnInteractLogic()
    {
        base.OnInteractLogic();
        _currentQty++;
        if (_handler != null)
            _handler.UpdateOrderText();
    }
}