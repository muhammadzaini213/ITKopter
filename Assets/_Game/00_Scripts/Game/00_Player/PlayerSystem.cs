
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

    public string PlayerName => playerName;

    // Total EXP accumulated by the player.
    public int PlayerExp => playerExp;

    // Calculate the player's level from their total EXP.
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

    // EXP accumulated within the current level.
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

    // EXP required to reach the next level.
    public int RequiredLevelExp
    {
        get
        {
            long requiredExp = BaseExpRequired;

            for (int level = 1; level < PlayerLevel; level++)
            {
                requiredExp = GetNextRequiredExp(requiredExp);
            }

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
        LoadPlayerData();
        yield return null;
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

        // Prevent overflow when the required EXP becomes very large.
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

        SavePlayerData();
        OnPlayerNameChanged?.Invoke(playerName);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        int previousLevel = PlayerLevel;

        playerExp = (int)Math.Min(
            (long)playerExp + amount,
            int.MaxValue
        );

        int currentLevel = PlayerLevel;

        SavePlayerData();

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
        if (!CanAddNewOwnedCharacter(newCharacter))
            return false;

        characterOwned.Add(newCharacter);
        SavePlayerData();

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
        if (string.IsNullOrWhiteSpace(levelId))
            return;

        if (completedLevelIds.Add(levelId))
            SavePlayerData();
    }

    private void SavePlayerData()
    {
        PlayerSaveData data = new PlayerSaveData
        {
            playerName = playerName,
            playerExp = playerExp,

            ownedCharacterIds = characterOwned
                .FindAll(character => character != null)
                .ConvertAll(character => character.CharacterId),

            completedLevelIds = new List<string>(completedLevelIds)
        };

        Save.To(data, SaveFileName);
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

        // Notify listeners after player data has been loaded.
        OnPlayerNameChanged?.Invoke(playerName);
        OnPlayerExpChanged?.Invoke(playerExp);
        OnPlayerLevelChanged?.Invoke(PlayerLevel);
    }

    private CharacterData FindCharacterById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // TODO: Connect this method to the character database or registry.
        // Owned characters cannot be restored until this lookup is implemented.
        return null;
    }
}
