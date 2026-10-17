
using System.Collections.Generic;
using UnityEngine;

public class GameplayInitializer : MonoBehaviour
{
    private const int MaxTeamSize = 3;

    [Header("Character Setup")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Transform[] characterSpawnPoints = new Transform[3];

    [Header("Initialization")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool activateFirstCharacterOnly = true;

    [Header("Enemy Waves")]
    [SerializeField] private EnemyWaveManager enemyWaveManager;

    private readonly List<GameObject> characterInstances = new();
    private readonly List<Character> initializedCharacters = new();

    public LevelData CurrentLevel { get; private set; }

    public IReadOnlyList<LevelPartData> CurrentLevelParts =>
        CurrentLevel != null
            ? CurrentLevel.LevelParts
            : System.Array.Empty<LevelPartData>();

    public IReadOnlyList<RewardData> CurrentLevelRewards =>
        CurrentLevel != null
            ? CurrentLevel.LevelRewards
            : System.Array.Empty<RewardData>();

    public IReadOnlyList<GameObject> CharacterInstances =>
        characterInstances;

    public IReadOnlyList<Character> InitializedCharacters =>
        initializedCharacters;

    public bool IsInitialized { get; private set; }

    private void Start()
    {
        if (initializeOnStart)
            InitializeGameplay();
    }

    public bool InitializeGameplay()
    {
        if (IsInitialized)
        {
            Debug.LogWarning(
                "[GameplayInitializer] Gameplay is already initialized.",
                this);
            return false;
        }

        LevelSystem levelSystem = LevelSystem.Instance;

        if (levelSystem == null)
        {
            Debug.LogError(
                "[GameplayInitializer] LevelSystem instance was not found.",
                this);
            return false;
        }

        if (!levelSystem.HasActiveSession)
        {
            Debug.LogError(
                "[GameplayInitializer] No active level session was found.",
                this);
            return false;
        }

        if (enemyWaveManager == null)
        {
            Debug.LogError(
                "[GameplayInitializer] EnemyWaveManager is not assigned.",
                this);
            return false;
        }

        LevelData level = levelSystem.CurrentLevel;
        IReadOnlyList<CharacterData> team = levelSystem.CurrentTeam;

        if (level == null)
        {
            Debug.LogError(
                "[GameplayInitializer] Current level data is missing.",
                this);
            return false;
        }

        if (team == null || team.Count < 1 || team.Count > MaxTeamSize)
        {
            Debug.LogError(
                "[GameplayInitializer] Team must contain between 1 and 3 characters.",
                this);
            return false;
        }

        if (characterRoot == null)
        {
            Debug.LogError(
                "[GameplayInitializer] Character Root is not assigned.",
                this);
            return false;
        }

        if (characterSpawnPoints == null ||
            characterSpawnPoints.Length < team.Count)
        {
            Debug.LogError(
                "[GameplayInitializer] Not enough character spawn points are assigned.",
                this);
            return false;
        }

        // Validate all selected characters before spawning any of them.
        for (int i = 0; i < team.Count; i++)
        {
            CharacterData data = team[i];

            if (data == null || data.CharacterPrefab == null)
            {
                Debug.LogError(
                    $"[GameplayInitializer] Character data or prefab is missing at team index {i}.",
                    this);
                return false;
            }

            if (characterSpawnPoints[i] == null)
            {
                Debug.LogError(
                    $"[GameplayInitializer] Spawn point {i} is not assigned.",
                    this);
                return false;
            }

            if (data.CharacterPrefab.GetComponent<Character>() == null)
            {
                Debug.LogError(
                    $"[GameplayInitializer] Prefab '{data.CharacterPrefab.name}' " +
                    "does not have a Character component on its root.",
                    this);
                return false;
            }
        }

        CurrentLevel = level;

        for (int i = 0; i < team.Count; i++)
        {
            CharacterData data = team[i];
            Transform spawnPoint = characterSpawnPoints[i];

            GameObject instance = Instantiate(
                data.CharacterPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                characterRoot);

            Character character = instance.GetComponent<Character>();

            // Apply the selected CharacterData to this instance.
            character.Initialize(data);

            if (activateFirstCharacterOnly)
                instance.SetActive(i == 0);

            characterInstances.Add(instance);
            initializedCharacters.Add(character);
        }

        IsInitialized = true;

        Debug.Log(
            $"[GameplayInitializer] Initialized level '{CurrentLevel.LevelName}' " +
            $"with {initializedCharacters.Count} character(s).",
            this);

        // Start enemy waves only after all characters have been initialized.
        enemyWaveManager.StartLevelWaves();

        return true;
    }

    public GameObject GetCharacterInstance(int index)
    {
        if (index < 0 || index >= characterInstances.Count)
            return null;

        return characterInstances[index];
    }

    public Character GetCharacter(int index)
    {
        if (index < 0 || index >= initializedCharacters.Count)
            return null;

        return initializedCharacters[index];
    }

    public void ClearCharacters()
    {
        foreach (GameObject instance in characterInstances)
        {
            if (instance != null)
                Destroy(instance);
        }

        characterInstances.Clear();
        initializedCharacters.Clear();

        CurrentLevel = null;
        IsInitialized = false;
    }
}
