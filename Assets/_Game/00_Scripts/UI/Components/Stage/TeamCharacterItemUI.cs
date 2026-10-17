
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamCharacterItemUI : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text levelText;

    [Header("Selection")]
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectedIndicator;

    private CharacterData characterData;
    private Action<CharacterData> onSelected;

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandleClick);
    }

    public void Setup(
        CharacterData character,
        CharacterProgression progression,
        bool selected,
        Action<CharacterData> callback)
    {
        characterData = character;
        onSelected = callback;

        if (characterData == null)
            return;

        if (iconImage != null)
        {
            iconImage.sprite = characterData.Icon;
            iconImage.enabled = characterData.Icon != null;
        }

        if (characterNameText != null)
            characterNameText.text = characterData.CharacterName;

        if (levelText != null)
        {
            levelText.text = progression != null
                ? $"Lv. {progression.Level}"
                : "Lv. 1";
        }

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(selected);
    }

    private void HandleClick()
    {
        if (characterData != null)
            onSelected?.Invoke(characterData);
    }
}
