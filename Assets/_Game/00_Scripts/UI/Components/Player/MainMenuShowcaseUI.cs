using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's chosen showcase character (set in the Profile menu) on the main menu.
/// </summary>
[RequireComponent(typeof(Image))]
public class MainMenuShowcaseUI : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [Tooltip("Frame drawn over the character (like the Character detail panel). Hidden with the image.")]
    [SerializeField] private GameObject border;
    [Tooltip("Profile bar icon: shows the showcase character's Icon (the _1 face sprite).")]
    [SerializeField] private Image profileIcon;

    private PlayerSystem playerSystem;

    private void Reset()
    {
        characterImage = GetComponent<Image>();
    }

    private void Start()
    {
        if (characterImage == null)
            characterImage = GetComponent<Image>();

        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
        {
            Debug.LogError("[MainMenuShowcaseUI] PlayerSystem is not available.", this);
            return;
        }

        playerSystem.OnShowcaseCharacterChanged += Refresh;
        playerSystem.OnCharacterOwnedChanged += Refresh;
        playerSystem.OnPlayerDataReady += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (playerSystem == null)
            return;

        playerSystem.OnShowcaseCharacterChanged -= Refresh;
        playerSystem.OnCharacterOwnedChanged -= Refresh;
        playerSystem.OnPlayerDataReady -= Refresh;
    }

    private void Refresh()
    {
        if (characterImage == null || playerSystem == null)
            return;

        CharacterData character = playerSystem.ShowcaseCharacter;

        characterImage.sprite = character != null ? character.CharacterPortrait : null;

        // The scene placeholder is fully transparent and Sliced; show the art
        // opaque and as Simple so preserveAspect keeps the card proportions.
        characterImage.type = Image.Type.Simple;
        characterImage.preserveAspect = true;
        characterImage.color = Color.white;
        // Hide the placeholder when the player owns nobody yet.
        characterImage.enabled = characterImage.sprite != null;

        if (border != null)
            border.SetActive(characterImage.enabled);

        if (profileIcon != null)
        {
            profileIcon.sprite = character != null ? character.Icon : null;
            profileIcon.preserveAspect = true;
            profileIcon.enabled = profileIcon.sprite != null;
        }
    }
}
