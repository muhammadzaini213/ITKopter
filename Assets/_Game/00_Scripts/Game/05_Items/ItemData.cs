
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : RewardData
{
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private string desc;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public string Description => desc;
}