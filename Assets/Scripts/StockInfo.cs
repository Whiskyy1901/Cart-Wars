using System;
using UnityEngine;

public class StockInfo : MonoBehaviour
{
    [SerializeField] private string _stockName;
    [SerializeField] private float _stockPrice;
    [SerializeField] private Sprite _stockImage;
    [SerializeField] private int _maxStockability = 5; //How many times can u use to stock
    private int _currentStockability;

    public int currentStockability => _currentStockability;
    public string stockName => _stockName;
    public float stockPrice => _stockPrice;
    public Sprite stockImage => _stockImage;
    
    private void Awake()
    {
        _currentStockability = _maxStockability;
    }

    public void Stock()
    {
        _currentStockability -= 1;
        if (_currentStockability <= 0)
        {
            Destroy(gameObject);
        }
    }
}
