
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

public class PlayerSystem : GameSystem<PlayerSystem>
{
    private const string SaveFileName = "player";
    private const int BaseExpRequired = 100;
    private const double ExpMultiplier = 1.5;

    [Header("Player Data")]
    [SerializeField] private string playerName = "Player";
    [SerializeField, Min(0)] private int playerExp = 0;

    private readonly List<CharacterData> characterOwned = new();
    private readonly HashSet<string> completedLevelIds = new();

    private bool isPlayerDataLoaded;

    public string PlayerName => playerName;
    public int PlayerExp => playerExp;
    public bool IsPlayerDataLoaded => isPlayerDataLoaded;

    public int PlayerLevel
    {
        get
        {
            int level = 1;
            long remainingExp = playerExp;
            long requiredExp = BaseExpRequired;

            while (remainingExp >= requiredExp)
            {
                remainingExp -= requiredExp;
                level++;

                requiredExp = GetNextRequiredExp(requiredExp);
            }

            return level;
        }
    }

    public int CurrentLevelExp
    {
        get
        {
            long remainingExp = playerExp;
            long requiredExp = BaseExpRequired;

            while (remainingExp >= requiredExp)
            {
                remainingExp -= requiredExp;
                requiredExp = GetNextRequiredExp(requiredExp);
            }

            return (int)remainingExp;
        }
    }

    public int RequiredLevelExp
    {
        get
        {
            long requiredExp = BaseExpRequired;

            for (int level = 1; level < PlayerLevel; level++)
                requiredExp = GetNextRequiredExp(requiredExp);

            return (int)requiredExp;
        }
    }

    public IReadOnlyList<CharacterData> CharacterOwned => characterOwned;

    public IReadOnlyCollection<string> CompletedLevelIds =>
        completedLevelIds;

    public event Action<string> OnPlayerNameChanged;
    public event Action<int> OnPlayerExpChanged;
    public event Action<int> OnPlayerLevelChanged;

    public override IEnumerator Initialize()
    {
        isPlayerDataLoaded = false;

        // Wait until the inventory system exists and its item database is ready.
        while (InventorySystem.Instance == null ||
               !InventorySystem.Instance.IsDatabaseReady)
        {
            yield return null;
        }

        LoadPlayerData();

        isPlayerDataLoaded = true;

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
        double nextExp = Math.Ceiling(
            currentRequiredExp * ExpMultiplier
        );

        if (nextExp >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(1L, (long)nextExp);
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return;

        newName = newName.Trim();

        if (playerName == newName)
            return;

        playerName = newName;

        if (!SavePlayerData())
            return;

        OnPlayerNameChanged?.Invoke(playerName);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0 || !isPlayerDataLoaded)
            return;

        int previousLevel = PlayerLevel;

        playerExp = (int)Math.Min(
            (long)playerExp + amount,
            int.MaxValue
        );

        int currentLevel = PlayerLevel;

        if (!SavePlayerData())
            return;

        OnPlayerExpChanged?.Invoke(playerExp);

        if (currentLevel != previousLevel)
        {
            OnPlayerLevelChanged?.Invoke(currentLevel);

            Debug.Log(
                $"Player leveled up! Level {previousLevel} -> {currentLevel}"
            );
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

        if (!SavePlayerData())
        {
            characterOwned.Remove(newCharacter);
            return false;
        }

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

        if (completedLevelIds.Add(levelId))
        {
            if (!SavePlayerData())
                completedLevelIds.Remove(levelId);
        }
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

        if (inventorySystem == null || !inventorySystem.IsDatabaseReady)
        {
            Debug.LogWarning(
                "[PlayerSystem] Cannot save before the inventory database " +
                "is ready. Existing save data was preserved.");
            return false;
        }

        PlayerSaveData data = new PlayerSaveData
        {
            playerName = playerName,
            playerExp = playerExp,

            ownedCharacterIds = characterOwned
                .FindAll(character => character != null)
                .ConvertAll(character => character.CharacterId),

            completedLevelIds = new List<string>(completedLevelIds)
        };

        inventorySystem.WriteToSaveData(data);

        Save.To(data, SaveFileName);
        return true;
    }

    private void LoadPlayerData()
    {
        PlayerSaveData data = Save.From(
            SaveFileName,
            new PlayerSaveData()
        );

        if (data == null)
            data = new PlayerSaveData();

        playerName = string.IsNullOrWhiteSpace(data.playerName)
            ? "Player"
            : data.playerName.Trim();

        playerExp = Mathf.Max(0, data.playerExp);

        characterOwned.Clear();
        completedLevelIds.Clear();

        if (data.ownedCharacterIds != null)
        {
            foreach (string id in data.ownedCharacterIds)
            {
                CharacterData character = FindCharacterById(id);

                if (character != null &&
                    !characterOwned.Exists(
                        owned => owned.CharacterId == character.CharacterId))
                {
                    characterOwned.Add(character);
                }
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

        OnPlayerNameChanged?.Invoke(playerName);
        OnPlayerExpChanged?.Invoke(playerExp);
        OnPlayerLevelChanged?.Invoke(PlayerLevel);
    }

    private CharacterData FindCharacterById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // TODO: Resolve the character through the character database.
        // Return the CharacterData asset that matches this ID.
        return null;
    }
}
