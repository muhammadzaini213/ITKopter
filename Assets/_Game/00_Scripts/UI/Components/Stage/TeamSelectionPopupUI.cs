
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private readonly List<CharacterData> selectedTeam = new();

    private LevelData selectedLevel;
    private Action<LevelData, IReadOnlyList<CharacterData>> onLevelStart;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(HandleStartClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
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
        PlayerSystem player = PlayerSystem.Instance;

        if (level == null)
            return;

        if (player == null || !player.IsPlayerDataLoaded)
        {
            Debug.LogWarning(
                "[TeamSelectionPopupUI] Player data is not ready.");
            return;
        }

        if (!player.IsLevelUnlocked(level))
        {
            Debug.LogWarning(
                "[TeamSelectionPopupUI] This level is locked.");
            return;
        }

        selectedLevel = level;
        onLevelStart = startCallback;

        selectedTeam.Clear();
        selectedTeam.AddRange(player.GetLastTeamCharacters());

        gameObject.SetActive(true);
        SetMessage(string.Empty);

        RefreshCharacterList();
        RefreshSelectionUI();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void RefreshCharacterList()
    {
        if (characterListRoot == null || characterItemPrefab == null)
        {
            Debug.LogWarning(
                "[TeamSelectionPopupUI] Character list references are missing.");
            return;
        }

        foreach (Transform child in characterListRoot)
            Destroy(child.gameObject);

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null)
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

            item.Setup(
                character,
                progression,
                IsSelected(character),
                HandleCharacterSelected);
        }
    }

    private void HandleCharacterSelected(CharacterData character)
    {
        if (character == null)
            return;

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
        return selectedTeam.Exists(
            item => item != null &&
                    item.CharacterId == character.CharacterId);
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
                selectedLevel != null &&
                selectedTeam.Count > 0 &&
                selectedTeam.Count <= MaxTeamSize;
        }
    }

    private void HandleStartClicked()
    {
        PlayerSystem player = PlayerSystem.Instance;
        EnergySystem energySystem = EnergySystem.Instance;

        if (selectedLevel == null)
        {
            SetMessage("No level selected.");
            return;
        }

        if (player == null || !player.IsPlayerDataLoaded)
        {
            SetMessage("Player data is not ready.");
            return;
        }

        if (energySystem == null)
        {
            SetMessage("Energy system is unavailable.");
            return;
        }

        if (!player.IsLevelUnlocked(selectedLevel))
        {
            SetMessage("This level is locked.");
            return;
        }

        if (selectedTeam.Count == 0 ||
            selectedTeam.Count > MaxTeamSize)
        {
            SetMessage("Select between 1 and 3 characters.");
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

        int currentEnergy = energySystem.Energy;

        if (currentEnergy < energyCost)
        {
            SetMessage("Not enough energy.");
            return;
        }

        List<CharacterData> teamToStart = new(selectedTeam);
        List<CharacterData> previousTeam = player.GetLastTeamCharacters();

        // Save the team before starting, so the latest selection persists.
        if (!player.SaveLastTeam(teamToStart))
        {
            SetMessage("Failed to save the selected team.");
            return;
        }

        // Free levels do not require spending energy.
        if (energyCost > 0 && !energySystem.SpendEnergy(energyCost))
        {
            player.SaveLastTeam(previousTeam);
            SetMessage("Failed to spend energy.");
            return;
        }

        LevelData levelToStart = selectedLevel;
        Action<LevelData, IReadOnlyList<CharacterData>> callback =
            onLevelStart;

        Hide();

        // The caller decides how to enter gameplay.
        callback.Invoke(levelToStart, teamToStart);
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
