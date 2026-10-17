
using System;
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;
using UnityEngine;

public class PlayerSystem : GameSystem<PlayerSystem>
{
    private const string SaveFileName = "player";
    private const int BaseExpRequired = 100;
    private const double ExpMultiplier = 1.5;

    [Header("Player Data")]
    [SerializeField] private string playerName = "Player";
    [SerializeField, Min(0)] private int playerExp;

    [Header("Character Database")]
    [SerializeField] private CharacterDatabase characterDatabase;

    private readonly List<CharacterData> characterOwned = new();
    private readonly HashSet<string> completedLevelIds = new();

    private readonly Dictionary<string, CharacterProgression> characterProgressions = new();

    private bool isPlayerDataLoaded;

    public string PlayerName => playerName;
    public int PlayerExp => playerExp;
    public bool IsPlayerDataLoaded => isPlayerDataLoaded;

    public IReadOnlyList<CharacterData> CharacterOwned => characterOwned;
    public IReadOnlyCollection<string> CompletedLevelIds => completedLevelIds;

    public event Action<string> OnPlayerNameChanged;
    public event Action<int> OnPlayerExpChanged;
    public event Action<int> OnPlayerLevelChanged;

    public event Action OnPlayerDataReady;
    public event Action OnCharacterOwnedChanged;
    public event Action<string> OnCharacterProgressionChanged;

    public int PlayerLevel
    {
        get
        {
            int level = 1;
            long remaining = playerExp;
            long required = BaseExpRequired;

            while (remaining >= required && level < int.MaxValue)
            {
                remaining -= required;
                level++;
                required = GetNextRequiredExp(required);
            }

            return level;
        }
    }

    public int CurrentLevelExp
    {
        get
        {
            long remaining = playerExp;
            long required = BaseExpRequired;

            while (remaining >= required)
            {
                remaining -= required;
                required = GetNextRequiredExp(required);
            }

            return (int)remaining;
        }
    }

    public int RequiredLevelExp
    {
        get
        {
            long required = BaseExpRequired;

            for (int level = 1; level < PlayerLevel; level++)
                required = GetNextRequiredExp(required);

            return (int)required;
        }
    }

    public override IEnumerator Initialize()
    {
        isPlayerDataLoaded = false;

        while (InventorySystem.Instance == null ||
               !InventorySystem.Instance.IsDatabaseReady)
        {
            yield return null;
        }

        if (characterDatabase == null)
        {
            Debug.LogError(
                "[PlayerSystem] CharacterDatabase is not assigned.");
        }

        LoadPlayerData();

        isPlayerDataLoaded = true;

        OnPlayerNameChanged?.Invoke(playerName);
        OnPlayerExpChanged?.Invoke(playerExp);
        OnPlayerLevelChanged?.Invoke(PlayerLevel);

        OnPlayerDataReady?.Invoke();

        Debug.Log("[PlayerSystem] Player data loaded successfully.");
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    public override void PostInitialize()
    {
    }

    private static long GetNextRequiredExp(long currentRequiredExp)
    {
        double next = Math.Ceiling(currentRequiredExp * ExpMultiplier);

        if (next >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(1L, (long)next);
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return;

        newName = newName.Trim();

        if (playerName == newName)
            return;

        string previousName = playerName;
        playerName = newName;

        if (!SavePlayerData())
        {
            playerName = previousName;
            return;
        }

        OnPlayerNameChanged?.Invoke(playerName);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0 || !isPlayerDataLoaded)
            return;

        int previousLevel = PlayerLevel;

        playerExp = (int)Math.Min(
            (long)playerExp + amount,
            int.MaxValue);

        int currentLevel = PlayerLevel;

        if (!SavePlayerData())
            return;

        OnPlayerExpChanged?.Invoke(playerExp);

        if (currentLevel != previousLevel)
        {
            OnPlayerLevelChanged?.Invoke(currentLevel);

            Debug.Log(
                $"[PlayerSystem] Player leveled up: " +
                $"{previousLevel} -> {currentLevel}");
        }
    }

    public bool AddNewOwnedCharacter(CharacterData newCharacter)
    {
        if (!isPlayerDataLoaded ||
            !CanAddNewOwnedCharacter(newCharacter))
        {
            return false;
        }

        characterOwned.Add(newCharacter);

        if (!characterProgressions.ContainsKey(newCharacter.CharacterId))
        {
            characterProgressions.Add(
                newCharacter.CharacterId,
                new CharacterProgression());
        }

        if (!SavePlayerData())
        {
            characterOwned.Remove(newCharacter);
            characterProgressions.Remove(newCharacter.CharacterId);
            return false;
        }

        OnCharacterOwnedChanged?.Invoke();
        OnCharacterProgressionChanged?.Invoke(newCharacter.CharacterId);

        return true;
    }

    public bool CanAddNewOwnedCharacter(CharacterData character)
    {
        if (character == null ||
            string.IsNullOrWhiteSpace(character.CharacterId))
        {
            return false;
        }

        foreach (CharacterData owned in characterOwned)
        {
            if (owned != null &&
                owned.CharacterId == character.CharacterId)
            {
                return false;
            }
        }

        return true;
    }

    public CharacterProgression GetCharacterProgression(CharacterData character)
    {
        if (character == null)
            return null;

        return GetCharacterProgression(character.CharacterId);
    }

    public CharacterProgression GetCharacterProgression(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        if (!characterProgressions.TryGetValue(
                characterId, out CharacterProgression progression))
        {
            bool isOwned = characterOwned.Exists(
                character => character != null &&
                character.CharacterId == characterId);

            if (!isOwned)
                return null;

            progression = new CharacterProgression();
            characterProgressions.Add(characterId, progression);
        }

        return progression;
    }

    public bool AddCharacterExp(string characterId, int amount)
    {
        if (!isPlayerDataLoaded || amount <= 0)
            return false;

        CharacterProgression progression =
            GetCharacterProgression(characterId);

        if (progression == null)
            return false;

        progression.AddExp(amount);

        if (!SavePlayerData())
            return false;

        OnCharacterProgressionChanged?.Invoke(characterId);
        return true;
    }

    public bool AddCharacterAffection(string characterId, int amount)
    {
        if (!isPlayerDataLoaded || amount <= 0)
            return false;

        CharacterProgression progression =
            GetCharacterProgression(characterId);

        if (progression == null)
            return false;

        progression.AddAffection(amount);

        if (!SavePlayerData())
            return false;

        OnCharacterProgressionChanged?.Invoke(characterId);
        return true;
    }

    public bool IsLevelCompleted(string levelId)
    {
        return !string.IsNullOrWhiteSpace(levelId) &&
               completedLevelIds.Contains(levelId);
    }

    public void CompleteLevel(string levelId)
    {
        if (!isPlayerDataLoaded ||
            string.IsNullOrWhiteSpace(levelId))
        {
            return;
        }

        if (!completedLevelIds.Add(levelId))
            return;

        if (!SavePlayerData())
            completedLevelIds.Remove(levelId);
    }

    private bool SavePlayerData()
    {
        if (!isPlayerDataLoaded)
        {
            Debug.LogWarning(
                "[PlayerSystem] Cannot save before player data is loaded.");
            return false;
        }

        InventorySystem inventorySystem = InventorySystem.Instance;

        if (inventorySystem == null ||
            !inventorySystem.IsDatabaseReady)
        {
            Debug.LogWarning(
                "[PlayerSystem] Cannot save before the inventory database " +
                "is ready.");
            return false;
        }

        PlayerSaveData data = new PlayerSaveData
        {
            playerName = playerName,
            playerExp = playerExp,

            ownedCharacterIds = characterOwned
                .FindAll(character => character != null)
                .ConvertAll(character => character.CharacterId),

            completedLevelIds = new List<string>(completedLevelIds),

            characterProgressions = new List<CharacterProgressionSaveData>()
        };

        foreach (KeyValuePair<string, CharacterProgression> pair
                 in characterProgressions)
        {
            if (pair.Value == null)
                continue;

            data.characterProgressions.Add(
                new CharacterProgressionSaveData
                {
                    characterId = pair.Key,
                    totalExp = pair.Value.TotalExp,
                    affectionPoints = pair.Value.AffectionPoints
                });
        }

        inventorySystem.WriteToSaveData(data);
        Save.To(data, SaveFileName);

        return true;
    }

    private void LoadPlayerData()
    {
        PlayerSaveData data = Save.From(
            SaveFileName,
            new PlayerSaveData());

        if (data == null)
            data = new PlayerSaveData();

        playerName = string.IsNullOrWhiteSpace(data.playerName)
            ? "Player"
            : data.playerName.Trim();

        playerExp = Mathf.Max(0, data.playerExp);

        characterOwned.Clear();
        completedLevelIds.Clear();
        characterProgressions.Clear();

        if (data.ownedCharacterIds != null)
        {
            foreach (string id in data.ownedCharacterIds)
            {
                CharacterData character = FindCharacterById(id);

                if (character == null)
                {
                    Debug.LogWarning(
                        $"[PlayerSystem] Character ID '{id}' " +
                        "could not be resolved.");
                    continue;
                }

                if (characterOwned.Exists(
                    owned => owned.CharacterId == character.CharacterId))
                {
                    continue;
                }

                characterOwned.Add(character);

                characterProgressions[character.CharacterId] =
                    new CharacterProgression();
            }
        }

        if (data.characterProgressions != null)
        {
            foreach (CharacterProgressionSaveData saved in
                     data.characterProgressions)
            {
                if (saved == null ||
                    string.IsNullOrWhiteSpace(saved.characterId))
                {
                    continue;
                }

                bool isOwned = characterOwned.Exists(
                    character => character.CharacterId == saved.characterId);

                if (!isOwned)
                    continue;

                characterProgressions[saved.characterId] =
                    new CharacterProgression(
                        saved.totalExp,
                        saved.affectionPoints);
            }
        }

        if (data.completedLevelIds != null)
        {
            foreach (string id in data.completedLevelIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    completedLevelIds.Add(id);
            }
        }

        InventorySystem.Instance.LoadFromSaveData(data);
    }

    private CharacterData FindCharacterById(string id)
    {
        if (characterDatabase == null)
            return null;

        return characterDatabase.FindById(id);
    }
}
