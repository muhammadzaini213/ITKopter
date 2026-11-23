using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Profile menu summary: showcase (main menu) character on the player card, plus account stats
/// (units owned, collection %, stages cleared, item types, total items).
/// Name / level / EXP on the card are handled by PlayerComponentUI.
/// </summary>
public class ProfileSummaryUI : MonoBehaviour
{
    [Header("Player Card")]
    [SerializeField] private Image leaderPortrait;
    [SerializeField] private TextMeshProUGUI leaderNameText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI unitsOwnedText;
    [SerializeField] private TextMeshProUGUI collectionText;
    [SerializeField] private TextMeshProUGUI stagesClearedText;
    [SerializeField] private TextMeshProUGUI itemTypesText;
    [SerializeField] private TextMeshProUGUI totalItemsText;

    private PlayerSystem playerSystem;

    private void Start()
    {
        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
        {
            Debug.LogError("[ProfileSummaryUI] PlayerSystem is not available.", this);
            return;
        }

        playerSystem.OnPlayerDataReady += Refresh;
        playerSystem.OnCharacterOwnedChanged += Refresh;
        playerSystem.OnLevelProgressChanged += Refresh;
        playerSystem.OnShowcaseCharacterChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (playerSystem == null)
            return;

        playerSystem.OnPlayerDataReady -= Refresh;
        playerSystem.OnCharacterOwnedChanged -= Refresh;
        playerSystem.OnLevelProgressChanged -= Refresh;
        playerSystem.OnShowcaseCharacterChanged -= Refresh;
    }

    public void Refresh()
    {
        if (playerSystem == null)
            return;

        RefreshLeader();

        int owned = playerSystem.CharacterOwned.Count;
        CharacterDatabase database = playerSystem.CharacterDatabase;
        int total = database != null && database.Characters != null
            ? database.Characters.Length
            : 0;

        SetText(unitsOwnedText, total > 0 ? $"{owned} / {total}" : owned.ToString());
        SetText(collectionText, total > 0
            ? $"{Mathf.RoundToInt(owned * 100f / total)}%"
            : "-");
        SetText(stagesClearedText, playerSystem.CompletedLevelIds.Count.ToString());

        int itemTypes = 0;
        int totalItems = 0;

        InventorySystem inventory = InventorySystem.Instance;

        if (inventory != null)
        {
            List<InventoryItemSaveData> items = inventory.GetInventoryItems();

            foreach (InventoryItemSaveData item in items)
            {
                if (item == null || item.amount <= 0)
                    continue;

                itemTypes++;
                totalItems += item.amount;
            }
        }

        SetText(itemTypesText, itemTypes.ToString());
        SetText(totalItemsText, totalItems.ToString("N0"));
    }

    private void RefreshLeader()
    {
        // The card shows the same character the main menu does.
        CharacterData partner = playerSystem.ShowcaseCharacter;

        if (leaderPortrait != null)
        {
            leaderPortrait.sprite = partner != null ? partner.CharacterPortrait : null;
            leaderPortrait.enabled = leaderPortrait.sprite != null;
        }

        SetText(leaderNameText, partner != null ? $"Partner: {partner.CharacterName}" : "No partner");
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
