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

    [Header("Hierarchy / Role / Rarity")]
    [SerializeField] private Image typeIcon;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private Image roleIcon;
    [SerializeField] private TMP_Text roleText;
    [Tooltip("Large rarity letters (UR / SR / R / N), tinted by rarity.")]
    [SerializeField] private TMP_Text rarityBadgeText;
    [SerializeField] private TMP_Text rarityText;
    [Tooltip("Hierarchy icons indexed by CharacterType (Abstract, Simple, Special, Primordial, Anomaly).")]
    [SerializeField] private Sprite[] typeSprites = new Sprite[5];
    [Tooltip("Role icons indexed by CharacterRole.")]
    [SerializeField] private Sprite[] roleSprites = new Sprite[7];

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

    [Header("EX Skill Info")]
    [Tooltip("Opens the EX skill description. Empty = cloned from the Upgrade button and placed at the panel's top-right.")]
    [SerializeField] private Button skillInfoButton;
    [SerializeField] private SFXReference skillInfoClickSfx;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private Action previousCallback;
    private Action nextCallback;
    private Action upgradeCallback;

    private CharacterData shownCharacter;
    private int shownLevel = 1;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(HandlePrevious);

        if (nextButton != null)
            nextButton.onClick.AddListener(HandleNext);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(HandleUpgrade);

        SetupSkillInfoButton();
    }

    private void SetupSkillInfoButton()
    {
        if (skillInfoButton == null && upgradeButton != null)
        {
            // Inside the stats card (the Upgrade button's parent), not the
            // full-screen container this component sits on.
            Transform card = upgradeButton.transform.parent != null
                ? upgradeButton.transform.parent
                : transform;

            skillInfoButton = InfoPopupUI.CloneButton(
                upgradeButton, card, "SkillInfoButton", "EX Skill", null);

            if (skillInfoButton != null)
            {
                var rect = (RectTransform)skillInfoButton.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(118f, 42f);
                rect.anchoredPosition = new Vector2(-14f, -14f);

                foreach (var label in skillInfoButton.GetComponentsInChildren<TMP_Text>(true))
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 10f;
                    label.fontSizeMax = 22f;
                }
            }
        }

        if (skillInfoButton != null)
            skillInfoButton.onClick.AddListener(OpenSkillInfo);
    }

    /// <summary>Shows the EX skill of the character on display, with numbers at its current level.</summary>
    public void OpenSkillInfo()
    {
        if (shownCharacter == null)
            return;

        ExSkill skill = shownCharacter.CharacterPrefab != null
            ? shownCharacter.CharacterPrefab.GetComponentInChildren<ExSkill>(true)
            : null;

        float attack = shownCharacter.GetAttackAtLevel(shownLevel);
        float defense = shownCharacter.GetDefenseAtLevel(shownLevel);

        string title = skill != null && !string.IsNullOrEmpty(skill.SkillName)
            ? skill.SkillName.ToUpperInvariant()
            : "EX SKILL";

        string body =
            $"<color=#19F0FF>{shownCharacter.CharacterName}</color>  Lv. {shownLevel}   " +
            $"<color=#8FA9B5>ATK {Mathf.RoundToInt(attack)}  DEF {Mathf.RoundToInt(defense)}  " +
            $"Cooldown {shownCharacter.ExSkillCooldown:0}s</color>\n\n" +
            (skill != null ? skill.Describe(attack, defense) : "This unit has no EX skill.") +
            "\n\n<color=#8FA9B5>Numbers in (~) are at the current level; % values scale with upgrades.</color>";

        InfoPopupUI.Show(title, body, new Vector2(1100f, 720f), skillInfoClickSfx);
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(HandlePrevious);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(HandleNext);

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveListener(HandleUpgrade);

        if (skillInfoButton != null)
            skillInfoButton.onClick.RemoveListener(OpenSkillInfo);
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

        shownCharacter = character;
        shownLevel = level;

        if (iconImage != null)
        {
            iconImage.sprite = character.CharacterPortrait;
            iconImage.enabled = character.CharacterPortrait != null;
        }

        if (nameText != null)
            nameText.text = character.CharacterName;

        if (levelText != null)
            levelText.text = $"Lv. {level}";

        ShowTags(character);

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

    private void ShowTags(CharacterData character)
    {
        SetIcon(typeIcon, GetSprite(typeSprites, (int)character.CharacterType));
        SetIcon(roleIcon, GetSprite(roleSprites, (int)character.CharacterRole));

        if (typeText != null)
            typeText.text = character.CharacterType.ToString().ToUpperInvariant();

        if (roleText != null)
            roleText.text = character.CharacterRole.ToString().ToUpperInvariant();

        Color rarityColor = RarityStyle.GetColor(character.Rarity);

        if (rarityBadgeText != null)
        {
            rarityBadgeText.text = character.Rarity.ToString();
            rarityBadgeText.color = rarityColor;
        }

        if (rarityText != null)
        {
            rarityText.text = RarityStyle.GetFullName(character.Rarity);
            rarityText.color = rarityColor;
        }
    }

    private static Sprite GetSprite(Sprite[] sprites, int index)
    {
        return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = sprite != null;
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
