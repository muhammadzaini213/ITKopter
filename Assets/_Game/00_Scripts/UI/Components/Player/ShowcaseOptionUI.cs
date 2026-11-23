using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One character card in the main menu character picker.
/// Unowned characters are shown locked and can't be picked.
/// </summary>
public class ShowcaseOptionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button button;
    [SerializeField] private Image frame;
    [SerializeField] private Image portrait;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject selectedBadge;

    [Header("Colors")]
    [SerializeField] private Color normalFrameColor = new Color(0.55f, 0.95f, 1f, 1f);
    [SerializeField] private Color selectedFrameColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color lockedPortraitColor = new Color(0.25f, 0.25f, 0.3f, 1f);

    private CharacterData character;
    private Action<CharacterData> onPicked;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Setup(
        CharacterData data,
        bool isOwned,
        bool isSelected,
        Action<CharacterData> pickedCallback)
    {
        character = data;
        onPicked = pickedCallback;

        if (portrait != null)
        {
            portrait.sprite = data != null ? data.CharacterPortrait : null;
            portrait.enabled = portrait.sprite != null;
            portrait.color = isOwned ? Color.white : lockedPortraitColor;
        }

        if (nameText != null)
            nameText.text = data != null ? data.CharacterName : string.Empty;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!isOwned);

        if (selectedBadge != null)
            selectedBadge.SetActive(isOwned && isSelected);

        if (frame != null)
            frame.color = isSelected ? selectedFrameColor : normalFrameColor;

        if (button != null)
            button.interactable = isOwned;
    }

    private void HandleClick()
    {
        if (character != null)
            onPicked?.Invoke(character);
    }
}
