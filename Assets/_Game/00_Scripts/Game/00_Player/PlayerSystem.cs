using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

public class PlayerSystem : GameSystem<PlayerSystem>
{
    private const string SaveFileName = "player";

    private string playerName = "Player";
    private int playerExp;

    private readonly List<CharacterData> characterOwned = new();

    public string PlayerName => playerName;
    public int PlayerExp => playerExp;
    public IReadOnlyList<CharacterData> CharacterOwned => characterOwned;

    public override IEnumerator Initialize()
    {
        LoadPlayerData();
        yield return null;
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return;

        playerName = newName;
        SavePlayerData();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        playerExp += amount;
        SavePlayerData();
    }

    public bool AddNewOwnedCharacter(CharacterData newCharacter)
    {
        if (newCharacter == null)
            return false;

        foreach (CharacterData owned in characterOwned)
        {
            if (owned.CharacterId == newCharacter.CharacterId)
                return false;
        }

        characterOwned.Add(newCharacter);
        SavePlayerData();
        return true;
    }

    public bool CanAddNewOwnedCharacter(CharacterData character)
    {
        if (character == null)
            return false;

        if (string.IsNullOrWhiteSpace(character.CharacterId))
            return false;

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

    private void SavePlayerData()
    {
        PlayerSaveData data = new PlayerSaveData
        {
            playerName = playerName,
            playerExp = playerExp,
            ownedCharacterIds = characterOwned
                .ConvertAll(character => character.CharacterId)
        };

        Save.To(data, SaveFileName);
    }

    private void LoadPlayerData()
    {
        if (!Save.Exists(SaveFileName))
        {
            SavePlayerData();
            return;
        }

        PlayerSaveData data = Save.From<PlayerSaveData>(
            SaveFileName,
            new PlayerSaveData()
        );

        if (data == null)
            return;

        playerName = string.IsNullOrWhiteSpace(data.playerName)
            ? "Player"
            : data.playerName;

        playerExp = Mathf.Max(0, data.playerExp);
        characterOwned.Clear();

        if (data.ownedCharacterIds == null)
            return;

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

    private CharacterData FindCharacterById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        // Implementasikan lookup dari CharacterDatabase atau registry
        // karakter milik game. Jangan mengandalkan pencarian nama.
        return null;
    }

    private readonly HashSet<string> completedLevelIds = new();

    public bool IsLevelCompleted(string levelId)
    {
        return !string.IsNullOrEmpty(levelId)
            && completedLevelIds.Contains(levelId);
    }

    public void CompleteLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId))
            return;

        if (completedLevelIds.Add(levelId))
        {
            SavePlayerData();
        }
    }

    public IReadOnlyCollection<string> CompletedLevelIds
        => completedLevelIds;

    private void LoadCompletedLevels(List<string> savedIds)
    {
        completedLevelIds.Clear();

        if (savedIds == null)
            return;

        foreach (string id in savedIds)
        {
            if (!string.IsNullOrEmpty(id))
                completedLevelIds.Add(id);
        }
    }

    private void SaveCompletedLevels(PlayerSaveData data)
    {
        data.completedLevelIds = new List<string>(completedLevelIds);
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    public override void PostInitialize()
    {
    }
}