using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a banner's drop rates: rarity rates, featured/pity notes, and every reward
/// with its icon and per-pull chance (from GachaSystem.GetRewardRates).
/// </summary>
public class GachaRatesPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button closeButton;

    [Header("Summary")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text noteText;

    [Header("List")]
    [SerializeField] private Transform listRoot;
    [Tooltip("Inactive row with children Icon (Image), Name, Rate (TMP).")]
    [SerializeField] private GameObject rowTemplate;
    [Tooltip("Inactive header row with a TMP text, one per rarity.")]
    [SerializeField] private GameObject headerTemplate;
    [SerializeField] private Color featuredColor = new Color(1f, 0.85f, 0.2f, 1f);

    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (rowTemplate != null)
            rowTemplate.SetActive(false);

        if (headerTemplate != null)
            headerTemplate.SetActive(false);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open(GachaData gacha)
    {
        if (gacha == null || GachaSystem.Instance == null)
            return;

        if (titleText != null)
            titleText.text = $"{gacha.GachaName} Rates";

        if (summaryText != null)
        {
            summaryText.text =
                $"UR {gacha.URRate:0.##}%   SR {gacha.SRRate:0.##}%   " +
                $"R {gacha.RRate:0.##}%   N {gacha.NRate:0.##}%";
        }

        if (noteText != null)
        {
            string featured = gacha.FeaturedReward != null
                ? $"Featured: {DisplayName(gacha.FeaturedReward)} (rate-up on UR). "
                : string.Empty;
            noteText.text = $"{featured}Guaranteed {gacha.PityRarity} within {gacha.PityThreshold} pulls.";
        }

        BuildList(GachaSystem.Instance.GetRewardRates(gacha));

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void BuildList(List<GachaRateEntry> rates)
    {
        foreach (GameObject go in spawned)
        {
            if (go != null)
                Destroy(go);
        }

        spawned.Clear();

        if (listRoot == null || rowTemplate == null)
            return;

        RewardRarity? currentRarity = null;

        foreach (GachaRateEntry rate in rates)
        {
            if (headerTemplate != null && currentRarity != rate.Rarity)
            {
                currentRarity = rate.Rarity;

                GameObject header = Instantiate(headerTemplate, listRoot);
                header.SetActive(true);

                TMP_Text headerText = header.GetComponentInChildren<TMP_Text>(true);

                if (headerText != null)
                    headerText.text = rate.Rarity.ToString();

                spawned.Add(header);
            }

            GameObject row = Instantiate(rowTemplate, listRoot);
            row.SetActive(true);

            Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text nameText = row.transform.Find("Name")?.GetComponent<TMP_Text>();
            TMP_Text rateText = row.transform.Find("Rate")?.GetComponent<TMP_Text>();

            if (icon != null)
            {
                icon.sprite = rate.Reward != null ? rate.Reward.Icon : null;
                icon.preserveAspect = true;
                icon.enabled = icon.sprite != null;
            }

            if (nameText != null)
            {
                string label = DisplayName(rate.Reward) + FormatAmount(rate.MinAmount, rate.MaxAmount);
                nameText.text = rate.IsFeatured
                    ? $"{label} (Featured)"
                    : label;
                nameText.color = rate.IsFeatured ? featuredColor : Color.white;
            }

            if (rateText != null)
                rateText.text = FormatPercent(rate.Percent);

            spawned.Add(row);
        }
    }

    private static string FormatAmount(int min, int max)
    {
        if (max <= 1)
            return string.Empty;

        return min == max ? $" x{min}" : $" x{min}-{max}";
    }

    private static string FormatPercent(float percent)
    {
        if (percent <= 0f)
            return "0%";

        if (percent < 0.01f)
            return "<0.01%";

        return $"{percent:0.##}%";
    }

    private static string DisplayName(RewardData reward)
    {
        switch (reward)
        {
            case CharacterData character:
                return character.CharacterName;
            case ItemData item:
                return item.ItemName;
            case CurrencyRewardData currency:
                return currency.CurrencyType.ToString();
            case null:
                return "?";
            default:
                return reward.name;
        }
    }
}
