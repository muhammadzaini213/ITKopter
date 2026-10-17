
using System.Collections.Generic;
using UnityEngine;

public class CharacterMenuUI : MonoBehaviour
{
    [Header("Character List")]
    [SerializeField] private Transform listContentRoot;
    [SerializeField] private CharacterListItemUI listItemPrefab;

    [Header("Character Details")]
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private CharacterDetailPanelUI detailUI;

    private readonly List<CharacterListItemUI> spawnedItems = new();
    private readonly List<CharacterData> characters = new();

    private PlayerSystem playerSystem;
    private int selectedIndex = -1;
    private bool isSubscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        if (listContentRoot == null ||
            listItemPrefab == null ||
            detailUI == null)
        {
            Debug.LogError(
                "[CharacterMenuUI] Required UI references are missing.");
            return;
        }

        detailUI.SetNavigationCallbacks(
            SelectPreviousCharacter,
            SelectNextCharacter);

        TrySubscribe();
        RefreshFromPlayer();
    }

    private void TrySubscribe()
    {
        if (isSubscribed)
            return;

        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
            return;

        playerSystem.OnPlayerDataReady += HandlePlayerDataReady;
        playerSystem.OnCharacterOwnedChanged += HandleCharacterOwnedChanged;
        playerSystem.OnCharacterProgressionChanged += HandleProgressionChanged;

        isSubscribed = true;

        if (playerSystem.IsPlayerDataLoaded)
            RefreshFromPlayer();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
            return;

        if (playerSystem != null)
        {
            playerSystem.OnPlayerDataReady -= HandlePlayerDataReady;
            playerSystem.OnCharacterOwnedChanged -= HandleCharacterOwnedChanged;
            playerSystem.OnCharacterProgressionChanged -= HandleProgressionChanged;
        }

        isSubscribed = false;
        playerSystem = null;
    }

    private void HandlePlayerDataReady()
    {
        RefreshFromPlayer();
    }

    private void HandleCharacterOwnedChanged()
    {
        RefreshFromPlayer();
    }

    private void HandleProgressionChanged(string characterId)
    {
        RefreshCharacterList();

        if (selectedIndex >= 0 && selectedIndex < characters.Count &&
            characters[selectedIndex].CharacterId == characterId)
        {
            ShowSelectedCharacter();
        }
    }

    private void RefreshFromPlayer()
    {
        if (playerSystem == null)
            playerSystem = PlayerSystem.Instance;

        if (playerSystem == null || !playerSystem.IsPlayerDataLoaded)
            return;

        string previouslySelectedId = null;

        if (selectedIndex >= 0 && selectedIndex < characters.Count)
            previouslySelectedId = characters[selectedIndex].CharacterId;

        characters.Clear();

        foreach (CharacterData character in playerSystem.CharacterOwned)
        {
            if (character != null)
                characters.Add(character);
        }

        selectedIndex = -1;

        if (!string.IsNullOrEmpty(previouslySelectedId))
        {
            selectedIndex = characters.FindIndex(
                character => character.CharacterId == previouslySelectedId);
        }

        if (selectedIndex < 0 && characters.Count > 0)
            selectedIndex = 0;

        RefreshCharacterList();

        if (detailPanel != null && detailPanel.activeSelf &&
            selectedIndex >= 0)
        {
            ShowSelectedCharacter();
        }
        else if (characters.Count == 0)
        {
            ShowListPanel();
        }
    }

    private void RefreshCharacterList()
    {
        ClearCharacterList();

        if (playerSystem == null)
            return;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData character = characters[i];

            CharacterProgression progression =
                playerSystem.GetCharacterProgression(character);

            if (progression == null)
                continue;

            CharacterListItemUI item = Instantiate(
                listItemPrefab,
                listContentRoot);

            int index = i;

            item.Setup(
                character,
                progression,
                () => OpenCharacterDetails(index));

            spawnedItems.Add(item);
        }
    }

    public void OpenCharacterDetails(int index)
    {
        if (index < 0 || index >= characters.Count)
            return;

        selectedIndex = index;
        ShowSelectedCharacter();

        if (listPanel != null)
            listPanel.SetActive(false);

        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    private void ShowSelectedCharacter()
    {
        if (playerSystem == null ||
            selectedIndex < 0 ||
            selectedIndex >= characters.Count)
        {
            return;
        }

        CharacterData character = characters[selectedIndex];

        detailUI.ShowCharacter(
            character,
            playerSystem.GetCharacterProgression(character),
            characters.Count > 1);
    }

    public void SelectNextCharacter()
    {
        NavigateCharacter(1);
    }

    public void SelectPreviousCharacter()
    {
        NavigateCharacter(-1);
    }

    private void NavigateCharacter(int direction)
    {
        if (characters.Count <= 1 || selectedIndex < 0)
            return;

        selectedIndex = (selectedIndex + direction + characters.Count)
            % characters.Count;

        ShowSelectedCharacter();
    }

    public void AddExpToCharacter(string characterId, int amount)
    {
        if (playerSystem != null)
            playerSystem.AddCharacterExp(characterId, amount);
    }

    public void AddAffectionToCharacter(string characterId, int amount)
    {
        if (playerSystem != null)
            playerSystem.AddCharacterAffection(characterId, amount);
    }

    public void ShowListPanel()
    {
        if (listPanel != null)
            listPanel.SetActive(true);

        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    private void ClearCharacterList()
    {
        foreach (CharacterListItemUI item in spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        ClearCharacterList();
    }
}
