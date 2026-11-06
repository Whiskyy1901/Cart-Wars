using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class OrderHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _orderListText;
    [SerializeField] private Stall[] _stalls;
    [SerializeField] private int _maxQtyPerItem;

    [Header("Text Colours")]
    [SerializeField] private Color _correctColor = Color.green;
    [SerializeField] private Color _incorrectColor = Color.red;

    private List<Stall> _initialList = new List<Stall>();
    private List<Stall> _orderList = new List<Stall>();

    private void Start()
    {
        CreateOrder(2);
    }

    private void CreateOrder(int itemQty)
    {
        _initialList = new List<Stall>(_stalls);
        _orderList.Clear();

        for (int i = 0; i < itemQty; i++)
        {
            int j = Random.Range(0, _initialList.Count);
            _orderList.Add(_initialList[j]);
            _initialList.RemoveAt(j);
        }

        foreach (Stall stall in _orderList)
            stall.Initialization(Random.Range(1, _maxQtyPerItem + 1));

        UpdateOrderText();
    }

    public void UpdateOrderText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Current Order");

        foreach (Stall stall in _orderList)
        {
            Color c = stall.IsCorrect ? _correctColor : _incorrectColor;
            string hex = ColorUtility.ToHtmlStringRGB(c);
            sb.AppendLine($"<color=#{hex}>{stall.ItemName}: {stall.CurrentQty}/{stall.DecidedQty}</color>");
        }

        _orderListText.text = sb.ToString();
    }

    public bool IsOrderComplete()
    {
        foreach (var orderItem in _orderList)
            if (!orderItem.IsCorrect)
                return false;

        return true;
    }
}