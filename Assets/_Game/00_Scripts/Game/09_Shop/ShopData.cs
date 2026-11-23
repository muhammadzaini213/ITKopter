using UnityEngine;

[CreateAssetMenu(fileName = "ShopData", menuName = "Scriptable Objects/ShopData")]
public class ShopData : ScriptableObject
{
    [SerializeField] private string shopName;
    [SerializeField] private ShopItemEntry[] itemsForSale;

    [Header("Conversion (fill this instead of Items For Sale for a conversion shop)")]
    [SerializeField] private ConversionRule[] conversionRules;

    public string ShopName => shopName;
    public ShopItemEntry[] ItemsForSale => itemsForSale;
    public ConversionRule[] ConversionRules => conversionRules;

    public bool IsConversionShop =>
        conversionRules != null && conversionRules.Length > 0;
}
