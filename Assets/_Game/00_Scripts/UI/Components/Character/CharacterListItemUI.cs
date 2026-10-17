
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterListItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button detailsButton;

    private Action onDetailsClicked;

    private void Awake()
    {
        if (detailsButton != null)
            detailsButton.onClick.AddListener(HandleDetailsClicked);
    }

    private void OnDestroy()
    {
        if (detailsButton != null)
            detailsButton.onClick.RemoveListener(HandleDetailsClicked);
    }

    public void Setup(
        CharacterData data,
        CharacterProgression progression,
        Action detailsCallback)
    {
        if (data == null || progression == null)
            return;

        onDetailsClicked = detailsCallback;

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }

        if (nameText != null)
            nameText.text = data.CharacterName;

        if (levelText != null)
            levelText.text = $"Lv. {progression.Level}";
    }

    private void HandleDetailsClicked()
    {
        onDetailsClicked?.Invoke();
    }
}
