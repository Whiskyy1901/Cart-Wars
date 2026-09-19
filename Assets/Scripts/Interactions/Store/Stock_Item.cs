using UnityEngine;

public class Stock_Item : MonoBehaviour
{
    [SerializeField] private string _itemName = "Test";
    [SerializeField] private float _itemPrice = 5.99f;
    [SerializeField] private int _maxStockability = 2;
    
    private void Assign(string name, float price, int stockability)
    {
        name = _itemName;
        price = _itemPrice;
        stockability = _maxStockability;
    }

    
}
