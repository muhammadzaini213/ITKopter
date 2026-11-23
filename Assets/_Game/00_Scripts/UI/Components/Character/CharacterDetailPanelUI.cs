using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterDetailPanelUI : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    [Header("Stats (current -> next level)")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text defenseText;

    [Header("Upgrade")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image dustIcon;
    [SerializeField] private TMP_Text dustCostText;
    [SerializeField] private Image engramIcon;
    [SerializeField] private TMP_Text engramCostText;
    [SerializeField] private TMP_Text coinCostText;
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color notEnoughColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Affection")]
    [SerializeField] private TMP_Text affectionLevelText;
    [SerializeField] private Slider affectionSlider;
    [SerializeField] private TMP_Text affectionText;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private Action previousCallback;
    private Action nextCallback;
    private Action upgradeCallback;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(HandlePrevious);

        if (nextButton != null)
            nextButton.onClick.AddListener(HandleNext);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(HandleUpgrade);
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(HandlePrevious);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(HandleNext);

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(HandleUpgrade);
    }

    public void SetNavigationCallbacks(
        Action previousCallback,
        Action nextCallback)
    {
        this.previousCallback = previousCallback;
        this.nextCallback = nextCallback;
    }

    public void SetUpgradeCallback(Action callback)
    {
        upgradeCallback = callback;
    }

    public void ShowCharacter(
        CharacterData character,
        CharacterProgression progression,
        bool canNavigate,
        CharacterUpgradeCost upgradeCost,
        bool canUpgrade)
    {
        if (character == null || progression == null)
            return;

        int level = progression.Level;

        if (iconImage != null)
        {
            iconImage.sprite = character.CharacterPortrait;
            iconImage.enabled = character.CharacterPortrait != null;
        }

        if (nameText != null)
            nameText.text = character.CharacterName;

        if (levelText != null)
            levelText.text = $"Lv. {level}";

        // Stats now and after the next upgrade.
        SetStat(healthText, "HP", character.GetHealthAtLevel(level), character.GetHealthAtLevel(level + 1));
        SetStat(attackText, "ATK", character.GetAttackAtLevel(level), character.GetAttackAtLevel(level + 1));
        SetStat(defenseText, "DEF", character.GetDefenseAtLevel(level), character.GetDefenseAtLevel(level + 1));

        ShowUpgradeCost(upgradeCost);

        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade;

        // Affection level and points.
        if (affectionLevelText != null)
            affectionLevelText.text = $"Lv. {progression.AffectionLevel}";

        if (affectionSlider != null)
        {
            affectionSlider.minValue = 0;
            affectionSlider.maxValue = Mathf.Max(1, progression.RequiredAffection);
            affectionSlider.value = progression.CurrentAffection;
        }

        if (affectionText != null)
        {
            affectionText.text =
                $"{progression.CurrentAffection} / {progression.RequiredAffection}";
        }

        if (previousButton != null)
            previousButton.interactable = canNavigate;

        if (nextButton != null)
            nextButton.interactable = canNavigate;
    }

    private void ShowUpgradeCost(CharacterUpgradeCost cost)
    {
        InventorySystem inventory = InventorySystem.Instance;
        CurrencySystem currency = CurrencySystem.Instance;

        SetMaterial(dustIcon, dustCostText, cost.Dust, cost.DustAmount, inventory);
        SetMaterial(engramIcon, engramCostText, cost.Engram, cost.EngramAmount, inventory);

        if (coinCostText != null)
        {
            int owned = currency != null ? currency.Coins : 0;
            coinCostText.text = $"{FormatAmount(owned)}/{FormatAmount(cost.Coins)}";
            coinCostText.color = owned >= cost.Coins ? enoughColor : notEnoughColor;
        }
    }

    private void SetMaterial(Image icon, TMP_Text text, ItemData item, int required, InventorySystem inventory)
    {
        if (icon != null)
        {
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = icon.sprite != null;
        }

        if (text == null)
            return;

        int owned = inventory != null && item != null ? inventory.GetItemAmount(item) : 0;
        text.text = $"{FormatAmount(owned)}/{FormatAmount(required)}";
        text.color = owned >= required ? enoughColor : notEnoughColor;
    }

    private static void SetStat(TMP_Text text, string label, float current, float next)
    {
        if (text != null)
            text.text = $"{label}  {Mathf.RoundToInt(current)} > {Mathf.RoundToInt(next)}";
    }

    private static string FormatAmount(int value)
    {
        if (value >= 1000000)
            return $"{value / 1000000f:0.#}M";

        if (value >= 10000)
            return $"{value / 1000f:0.#}K";

        return value.ToString();
    }

    private void HandlePrevious()
    {
        previousCallback?.Invoke();
    }

    private void HandleNext()
    {
        nextCallback?.Invoke();
    }

    private void HandleUpgrade()
    {
        upgradeCallback?.Invoke();
    }
}
