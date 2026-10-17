
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;

public class LevelSystem : GameSystem<LevelSystem>
{
    private const int MaxTeamSize = 3;

    private LevelData currentLevel;
    private readonly List<CharacterData> currentTeam = new();

    public LevelData CurrentLevel => currentLevel;

    public string CurrentLevelId =>
        currentLevel != null ? currentLevel.LevelId : string.Empty;

    public string CurrentLevelName =>
        currentLevel != null ? currentLevel.LevelName : string.Empty;

    public LevelPartData[] CurrentLevelParts =>
        currentLevel != null ? currentLevel.LevelParts : null;

    public RewardData[] CurrentLevelRewards =>
        currentLevel != null ? currentLevel.LevelRewards : null;

    public int CurrentEnergyCost =>
        currentLevel != null ? currentLevel.EnergyCost : 0;

    public IReadOnlyList<CharacterData> CurrentTeam => currentTeam;

    public int CurrentTeamSize => currentTeam.Count;

    public bool HasActiveSession =>
        currentLevel != null && currentTeam.Count > 0;

    public override IEnumerator Initialize()
    {
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    

    public bool PrepareLevel(
        LevelData level,
        IReadOnlyList<CharacterData> team)
    {
        if (level == null)
        {
            Debug.LogError("[LevelSystem] Cannot prepare a null level.");
            return false;
        }

        if (team == null || team.Count < 1 || team.Count > MaxTeamSize)
        {
            Debug.LogError(
                "[LevelSystem] The team must contain between 1 and 3 characters.");
            return false;
        }

        HashSet<string> characterIds = new();

        foreach (CharacterData character in team)
        {
            if (character == null)
            {
                Debug.LogError(
                    "[LevelSystem] The selected team contains a null character.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(character.CharacterId))
            {
                Debug.LogError(
                    "[LevelSystem] A selected character has an invalid ID.");
                return false;
            }

            if (character.CharacterPrefab == null)
            {
                Debug.LogError(
                    $"[LevelSystem] Character prefab is missing: {character.CharacterName}");
                return false;
            }

            if (!characterIds.Add(character.CharacterId))
            {
                Debug.LogError(
                    $"[LevelSystem] Duplicate character in team: {character.CharacterId}");
                return false;
            }
        }

        currentLevel = level;

        currentTeam.Clear();
        currentTeam.AddRange(team);

        Debug.Log(
            $"[LevelSystem] Prepared level '{currentLevel.LevelName}' " +
            $"with {currentTeam.Count} character(s).");

        return true;
    }

    public CharacterData GetTeamMember(int index)
    {
        if (index < 0 || index >= currentTeam.Count)
        {
            Debug.LogWarning(
                $"[LevelSystem] Team index is out of range: {index}");
            return null;
        }

        return currentTeam[index];
    }

    public void ClearSession()
    {
        currentLevel = null;
        currentTeam.Clear();

        Debug.Log("[LevelSystem] Level session cleared.");
    }
}
