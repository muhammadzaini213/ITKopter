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

    [Header("EXP")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TMP_Text expText;

    [Header("Affection")]
    [SerializeField] private TMP_Text affectionLevelText;
    [SerializeField] private Slider affectionSlider;
    [SerializeField] private TMP_Text affectionText;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private Action previousCallback;
    private Action nextCallback;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(HandlePrevious);

        if (nextButton != null)
            nextButton.onClick.AddListener(HandleNext);
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(HandlePrevious);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(HandleNext);
    }

    public void SetNavigationCallbacks(
        Action previousCallback,
        Action nextCallback)
    {
        this.previousCallback = previousCallback;
        this.nextCallback = nextCallback;
    }

    public void ShowCharacter(
        CharacterData character,
        CharacterProgression progression,
        bool canNavigate)
    {
        if (character == null || progression == null)
            return;

        if (iconImage != null)
        {
            iconImage.sprite = character.Icon;
            iconImage.enabled = character.Icon != null;
        }

        if (nameText != null)
            nameText.text = character.CharacterName;

        // Character level and EXP.
        if (levelText != null)
            levelText.text = $"Lv. {progression.Level}";

        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = Mathf.Max(1, progression.RequiredLevelExp);
            expSlider.value = progression.CurrentLevelExp;
        }

        if (expText != null)
        {
            expText.text =
                $"{progression.CurrentLevelExp} / {progression.RequiredLevelExp}";
        }

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

    private void HandlePrevious()
    {
        previousCallback?.Invoke();
    }

    private void HandleNext()
    {
        nextCallback?.Invoke();
    }
}