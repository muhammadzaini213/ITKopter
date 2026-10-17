
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Slafurry.Systems.Scene;

public class TeamSelectionPopupUI : MonoBehaviour
{
    private const int MaxTeamSize = 3;

    [Header("Character List")]
    [SerializeField] private Transform characterListRoot;
    [SerializeField] private TeamCharacterItemUI characterItemPrefab;

    [Header("Selected Team")]
    [SerializeField] private TMP_Text selectedCountText;

    [Header("Feedback")]
    [SerializeField] private TMP_Text messageText;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;

    [Header("Scene")]
    [SerializeField] private SceneReference gameplayScene;

    private readonly List<CharacterData> selectedTeam = new();
    private readonly List<TeamCharacterItemUI> characterItems = new();

    private LevelData selectedLevel;
    private Action<LevelData, IReadOnlyList<CharacterData>> onLevelStart;

    private bool isStartingLevel;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(HandleStartClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        RefreshSelectionUI();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(HandleStartClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    public void Show(
        LevelData level,
        Action<LevelData, IReadOnlyList<CharacterData>> startCallback)
    {
        if (isStartingLevel)
            return;

        if (level == null)
        {
            Debug.LogWarning("[TeamSelectionPopupUI] Cannot open popup without a level.");
            return;
        }

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded)
        {
            Debug.LogWarning("[TeamSelectionPopupUI] Player data is not ready.");
            return;
        }

        if (!player.IsLevelUnlocked(level))
        {
            Debug.LogWarning("[TeamSelectionPopupUI] This level is locked.");
            return;
        }

        selectedLevel = level;
        onLevelStart = startCallback;

        selectedTeam.Clear();
        LoadSavedTeam(player);

        gameObject.SetActive(true);

        SetMessage(string.Empty);
        RefreshCharacterList();
        RefreshSelectionUI();
    }

    public void Hide()
    {
        if (isStartingLevel)
            return;

        gameObject.SetActive(false);
    }

    private void LoadSavedTeam(PlayerSystem player)
    {
        List<CharacterData> savedTeam = player.GetLastTeamCharacters();

        if (savedTeam == null)
            return;

        foreach (CharacterData character in savedTeam)
        {
            if (character == null)
                continue;

            if (!IsOwnedByPlayer(player, character))
                continue;

            if (IsSelected(character))
                continue;

            if (selectedTeam.Count >= MaxTeamSize)
                break;

            selectedTeam.Add(character);
        }
    }

    private void RefreshCharacterList()
    {
        ClearCharacterItems();

        if (characterListRoot == null || characterItemPrefab == null)
        {
            Debug.LogWarning(
                "[TeamSelectionPopupUI] Character list references are missing.");

            SetMessage("Character list is not configured.");
            return;
        }

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded)
            return;

        foreach (CharacterData character in player.CharacterOwned)
        {
            if (character == null)
                continue;

            CharacterProgression progression =
                player.GetCharacterProgression(character);

            TeamCharacterItemUI item = Instantiate(
                characterItemPrefab,
                characterListRoot);

            characterItems.Add(item);

            item.Setup(
                character,
                progression,
                IsSelected(character),
                HandleCharacterSelected);
        }
    }

    private void ClearCharacterItems()
    {
        foreach (TeamCharacterItemUI item in characterItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        characterItems.Clear();
    }

    private void HandleCharacterSelected(CharacterData character)
    {
        if (isStartingLevel || character == null)
            return;

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded)
        {
            SetMessage("Player data is not ready.");
            return;
        }

        if (!IsOwnedByPlayer(player, character))
        {
            SetMessage("You do not own this character.");
            return;
        }

        int index = selectedTeam.FindIndex(
            item => item != null &&
                    item.CharacterId == character.CharacterId);

        if (index >= 0)
        {
            selectedTeam.RemoveAt(index);
        }
        else
        {
            if (selectedTeam.Count >= MaxTeamSize)
            {
                SetMessage("You can select up to 3 characters.");
                return;
            }

            selectedTeam.Add(character);
        }

        SetMessage(string.Empty);
        RefreshCharacterList();
        RefreshSelectionUI();
    }

    private bool IsSelected(CharacterData character)
    {
        if (character == null)
            return false;

        return selectedTeam.Exists(
            item => item != null &&
                    item.CharacterId == character.CharacterId);
    }

    private bool IsOwnedByPlayer(
        PlayerSystem player,
        CharacterData character)
    {
        if (player == null || character == null)
            return false;

        foreach (CharacterData ownedCharacter in player.CharacterOwned)
        {
            if (ownedCharacter != null &&
                ownedCharacter.CharacterId == character.CharacterId)
            {
                return true;
            }
        }

        return false;
    }

    private bool ValidateSelectedTeam(PlayerSystem player)
    {
        if (selectedLevel == null)
        {
            SetMessage("No level selected.");
            return false;
        }

        if (player == null || !player.IsPlayerDataLoaded)
        {
            SetMessage("Player data is not ready.");
            return false;
        }

        if (!player.IsLevelUnlocked(selectedLevel))
        {
            SetMessage("This level is locked.");
            return false;
        }

        if (selectedTeam.Count < 1 || selectedTeam.Count > MaxTeamSize)
        {
            SetMessage("Select between 1 and 3 characters.");
            return false;
        }

        HashSet<string> characterIds = new();

        foreach (CharacterData character in selectedTeam)
        {
            if (character == null)
            {
                SetMessage("The selected team contains an invalid character.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(character.CharacterId))
            {
                SetMessage("A selected character has an invalid ID.");
                return false;
            }

            if (!IsOwnedByPlayer(player, character))
            {
                SetMessage("The selected team contains a character you do not own.");
                return false;
            }

            if (!characterIds.Add(character.CharacterId))
            {
                SetMessage("The selected team contains duplicate characters.");
                return false;
            }

            if (character.CharacterPrefab == null)
            {
                SetMessage(
                    $"Character prefab is missing for {character.CharacterName}.");
                return false;
            }
        }

        return true;
    }

    private void RefreshSelectionUI()
    {
        if (selectedCountText != null)
        {
            selectedCountText.text =
                $"Team: {selectedTeam.Count}/{MaxTeamSize}";
        }

        if (startButton != null)
        {
            startButton.interactable =
                !isStartingLevel &&
                selectedLevel != null &&
                selectedTeam.Count >= 1 &&
                selectedTeam.Count <= MaxTeamSize;
        }

        if (closeButton != null)
            closeButton.interactable = !isStartingLevel;
    }

    private void HandleStartClicked()
    {
        if (isStartingLevel)
            return;

        PlayerSystem player = PlayerSystem.Instance;
        EnergySystem energySystem = EnergySystem.Instance;

        if (!ValidateSelectedTeam(player))
            return;

        if (energySystem == null)
        {
            SetMessage("Energy system is unavailable.");
            return;
        }

        if (gameplayScene == null)
        {
            SetMessage("Gameplay scene is not configured.");
            Debug.LogError(
                "[TeamSelectionPopupUI] Gameplay SceneReference is missing.");
            return;
        }

        if (onLevelStart == null)
        {
            SetMessage("Level start callback is not configured.");
            return;
        }

        int energyCost = selectedLevel.EnergyCost;

        if (energyCost < 0)
        {
            SetMessage("Invalid level energy cost.");
            return;
        }

        if (energySystem.Energy < energyCost)
        {
            SetMessage("Not enough energy.");
            return;
        }

        List<CharacterData> teamToStart = new(selectedTeam);
        List<CharacterData> previousTeam = player.GetLastTeamCharacters();

        if (!player.SaveLastTeam(teamToStart))
        {
            SetMessage("Failed to save the selected team.");
            return;
        }

        if (energyCost > 0 && !energySystem.SpendEnergy(energyCost))
        {
            if (!player.SaveLastTeam(previousTeam))
            {
                Debug.LogError(
                    "[TeamSelectionPopupUI] Failed to restore the previous team.");
            }

            SetMessage("Failed to spend energy.");
            return;
        }

        LevelData levelToStart = selectedLevel;
        Action<LevelData, IReadOnlyList<CharacterData>> callback = onLevelStart;

        isStartingLevel = true;
        RefreshSelectionUI();
        SetMessage(string.Empty);

        // Close the popup before requesting the scene transition.
        gameObject.SetActive(false);

        try
        {
            // Notify the caller with the selected level and team.
            callback.Invoke(levelToStart, teamToStart);

            // Load the configured gameplay scene.
            SceneSystem.Load(gameplayScene);
            LevelSystem.Instance.PrepareLevel(levelToStart, teamToStart);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            isStartingLevel = false;

            if (energyCost > 0)
            {
                Debug.LogWarning(
                    "[TeamSelectionPopupUI] Level start failed after energy was spent.");
            }

            if (gameObject != null)
            {
                gameObject.SetActive(true);
                SetMessage("Failed to start the level. Please try again.");
                RefreshSelectionUI();
            }
        }
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
