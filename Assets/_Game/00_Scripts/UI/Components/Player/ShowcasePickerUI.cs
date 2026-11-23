using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Profile menu popup for choosing the character shown on the main menu.
/// Lists every character in the database; only owned ones can be picked.
/// </summary>
public class ShowcasePickerUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Options")]
    [SerializeField] private Transform optionContainer;
    [SerializeField] private ShowcaseOptionUI optionPrefab;

    [Header("Preview")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TextMeshProUGUI previewNameText;

    private readonly List<ShowcaseOptionUI> options = new List<ShowcaseOptionUI>();
    private PlayerSystem playerSystem;

    private void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (openButton != null)
            openButton.onClick.AddListener(Open);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
        {
            Debug.LogError("[ShowcasePickerUI] PlayerSystem is not available.", this);
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);

        Rebuild();
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void Rebuild()
    {
        ClearOptions();

        CharacterDatabase database = playerSystem.CharacterDatabase;
        CharacterData selected = playerSystem.ShowcaseCharacter;

        if (database != null && database.Characters != null &&
            optionContainer != null && optionPrefab != null)
        {
            // Owned characters first so the usable ones are on top.
            List<CharacterData> ordered = new List<CharacterData>();

            foreach (CharacterData character in database.Characters)
            {
                if (character != null && playerSystem.IsCharacterOwned(character))
                    ordered.Add(character);
            }

            foreach (CharacterData character in database.Characters)
            {
                if (character != null && !playerSystem.IsCharacterOwned(character))
                    ordered.Add(character);
            }

            foreach (CharacterData character in ordered)
            {
                ShowcaseOptionUI option = Instantiate(optionPrefab, optionContainer);
                option.Setup(
                    character,
                    playerSystem.IsCharacterOwned(character),
                    character == selected,
                    Pick);
                options.Add(option);
            }
        }

        RefreshPreview(selected);
    }

    private void Pick(CharacterData character)
    {
        if (playerSystem == null || !playerSystem.SetShowcaseCharacter(character))
            return;

        Rebuild();
    }

    private void RefreshPreview(CharacterData character)
    {
        if (previewImage != null)
        {
            previewImage.sprite = character != null ? character.CharacterPortrait : null;
            previewImage.enabled = previewImage.sprite != null;
        }

        if (previewNameText != null)
            previewNameText.text = character != null ? character.CharacterName : "No character owned";
    }

    private void ClearOptions()
    {
        foreach (ShowcaseOptionUI option in options)
        {
            if (option != null)
                Destroy(option.gameObject);
        }

        options.Clear();
    }
}
