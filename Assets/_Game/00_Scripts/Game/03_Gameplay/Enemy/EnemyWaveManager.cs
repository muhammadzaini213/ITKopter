
using System.Collections;
using UnityEngine;
using System;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform enemyContainer;

    [Header("Wave Settings")]
    [SerializeField, Min(0f)] private float delayBetweenWaves = 1f;
    [SerializeField, Min(0f)] private float delayBetweenParts = 1f;

    private LevelSystem levelSystem;
    private Coroutine waveRoutine;
    private EnemyCollection currentCollection;

    public bool IsRunning { get; private set; }
    public int CurrentPartIndex { get; private set; } = -1;
    public int CurrentWaveIndex { get; private set; } = -1;

    public event Action OnLevelWon;
    public event Action<int, int> OnWaveStarted;
    public event Action<int> OnPartStarted;

    void Awake()
    {
        levelSystem = LevelSystem.Instance;
    }

    public void StartLevelWaves()
    {
        if (IsRunning)
        {
            Debug.LogWarning(
                "[EnemyWaveManager] Wave sequence is already running.",
                this);
            return;
        }

        if (levelSystem == null)
        {
            Debug.LogError(
                "[EnemyWaveManager] LevelSystem is not assigned.",
                this);
            return;
        }

        if (enemyContainer == null)
        {
            Debug.LogError(
                "[EnemyWaveManager] Enemy Container is not assigned.",
                this);
            return;
        }

        if (levelSystem.CurrentLevelParts == null ||
            levelSystem.CurrentLevelParts.Length == 0)
        {
            Debug.LogError(
                "[EnemyWaveManager] No level parts are configured.",
                this);
            return;
        }

        waveRoutine = StartCoroutine(RunLevelWaves());
    }

    private IEnumerator RunLevelWaves()
    {
        IsRunning = true;

        LevelPartData[] parts = levelSystem.CurrentLevelParts;

        for (int partIndex = 0; partIndex < parts.Length; partIndex++)
        {
            LevelPartData part = parts[partIndex];

            if (part == null)
            {
                Debug.LogError(
                    $"[EnemyWaveManager] Level part at index {partIndex} is null.",
                    this);
                continue;
            }

            CurrentPartIndex = partIndex;
            CurrentWaveIndex = -1;

            OnPartStarted?.Invoke(partIndex);

            Debug.Log(
                $"[EnemyWaveManager] Starting part: {part.PartName}",
                this);

            WaveData[] waves = part.EnemyWaves;

            if (waves == null || waves.Length == 0)
            {
                Debug.LogWarning(
                    $"[EnemyWaveManager] Part '{part.PartName}' has no waves.",
                    this);
                continue;
            }

            for (int waveIndex = 0; waveIndex < waves.Length; waveIndex++)
            {
                WaveData wave = waves[waveIndex];

                if (wave == null)
                {
                    Debug.LogError(
                        $"[EnemyWaveManager] Wave at index {waveIndex} in part '{part.PartName}' is null.",
                        this);
                    continue;
                }

                if (wave.EnemyCollectionPrefab == null)
                {
                    Debug.LogError(
                        $"[EnemyWaveManager] Wave '{wave.WaveName}' has no enemy collection prefab.",
                        this);
                    continue;
                }

                CurrentWaveIndex = waveIndex;

                GameObject collectionObject = Instantiate(
                    wave.EnemyCollectionPrefab,
                    enemyContainer
                );

                currentCollection =
                    collectionObject.GetComponent<EnemyCollection>();

                if (currentCollection == null)
                {
                    Debug.LogError(
                        $"[EnemyWaveManager] Prefab for wave '{wave.WaveName}' has no EnemyCollection component.",
                        collectionObject);

                    Destroy(collectionObject);
                    continue;
                }

                OnWaveStarted?.Invoke(partIndex, waveIndex);

                Debug.Log(
                    $"[EnemyWaveManager] Started wave '{wave.WaveName}' in part '{part.PartName}'.",
                    this);

                // Wait until every enemy in this collection is defeated.
                yield return new WaitUntil(
                    () => currentCollection == null ||
                          currentCollection.IsCleared
                );

                if (currentCollection != null)
                {
                    Destroy(currentCollection.gameObject);
                    currentCollection = null;
                }

                if (delayBetweenWaves > 0f)
                    yield return new WaitForSeconds(delayBetweenWaves);
            }

            if (delayBetweenParts > 0f && partIndex < parts.Length - 1)
                yield return new WaitForSeconds(delayBetweenParts);
        }

        IsRunning = false;
        waveRoutine = null;

        Debug.Log("[EnemyWaveManager] All level waves completed.", this);

        OnLevelWon?.Invoke();
    }

    public void StopLevelWaves()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        if (currentCollection != null)
        {
            Destroy(currentCollection.gameObject);
            currentCollection = null;
        }

        IsRunning = false;
        CurrentPartIndex = -1;
        CurrentWaveIndex = -1;
    }

    private void OnDisable()
    {
        StopLevelWaves();
    }
}
