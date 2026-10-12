
using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "Scriptable Objects/ShopData")]
public class ShopData : ScriptableObject
{
    [SerializeField] private string shopName;
    [SerializeField] private ShopItemEntry[] itemsForSale;

    public string ShopName => shopName;
    public ShopItemEntry[] ItemsForSale => itemsForSale;
}