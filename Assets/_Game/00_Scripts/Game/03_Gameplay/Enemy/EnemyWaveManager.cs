
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Slider waveProgressSlider;

    [Header("Wave Settings")]
    [SerializeField, Min(0f)] private float delayBetweenWaves = 1f;
    [SerializeField, Min(0f)] private float delayBetweenParts = 1f;

    private LevelSystem levelSystem;
    private Coroutine waveRoutine;
    private EnemyCollection currentCollection;

    private int totalWaves;
    private int completedWaves;

    public bool IsRunning { get; private set; }
    public int CurrentPartIndex { get; private set; } = -1;
    public int CurrentWaveIndex { get; private set; } = -1;

    public int TotalWaves => totalWaves;
    public int CompletedWaves => completedWaves;

    public float WaveProgress =>
        totalWaves > 0
            ? Mathf.Clamp01((float)completedWaves / totalWaves)
            : 0f;

    public event Action OnLevelWon;
    public event Action<int, int> OnWaveStarted;
    public event Action<int> OnPartStarted;

    private void Awake()
    {
        levelSystem = LevelSystem.Instance;
        ResetProgressSlider();
    }

    public void StartLevelWaves()
    {
        if (IsRunning || waveRoutine != null)
        {
            Debug.LogWarning(
                "[EnemyWaveManager] Wave sequence is already running.",
                this);
            return;
        }

        if (levelSystem == null)
            levelSystem = LevelSystem.Instance;

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

        LevelPartData[] parts = levelSystem.CurrentLevelParts;

        if (parts == null || parts.Length == 0)
        {
            Debug.LogError(
                "[EnemyWaveManager] No level parts are configured.",
                this);
            return;
        }

        totalWaves = CountValidWaves(parts);
        completedWaves = 0;

        RefreshProgressSlider();

        if (totalWaves == 0)
        {
            Debug.LogWarning(
                "[EnemyWaveManager] No valid waves were found.",
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

                if (wave == null || wave.EnemyCollectionPrefab == null)
                {
                    Debug.LogWarning(
                        $"[EnemyWaveManager] Skipping invalid wave at index {waveIndex} in part '{part.PartName}'.",
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

                yield return new WaitUntil(
                    () => currentCollection == null ||
                          currentCollection.IsCleared
                );

                if (currentCollection != null)
                {
                    Destroy(currentCollection.gameObject);
                    currentCollection = null;
                }

                completedWaves = Mathf.Min(
                    completedWaves + 1,
                    totalWaves
                );

                RefreshProgressSlider();

                Debug.Log(
                    $"[EnemyWaveManager] Wave progress: {completedWaves}/{totalWaves} ({WaveProgress:P0}).",
                    this);

                if (delayBetweenWaves > 0f)
                    yield return new WaitForSeconds(delayBetweenWaves);
            }

            if (delayBetweenParts > 0f && partIndex < parts.Length - 1)
                yield return new WaitForSeconds(delayBetweenParts);
        }

        IsRunning = false;
        waveRoutine = null;

        // Mark the level as complete after the entire sequence finishes.
        completedWaves = totalWaves;
        RefreshProgressSlider();

        Debug.Log(
            "[EnemyWaveManager] All level waves completed.",
            this);

        OnLevelWon?.Invoke();
    }

    private int CountValidWaves(LevelPartData[] parts)
    {
        int count = 0;

        foreach (LevelPartData part in parts)
        {
            if (part == null || part.EnemyWaves == null)
                continue;

            foreach (WaveData wave in part.EnemyWaves)
            {
                if (wave != null && wave.EnemyCollectionPrefab != null)
                    count++;
            }
        }

        return count;
    }

    private void ResetProgressSlider()
    {
        if (waveProgressSlider == null)
            return;

        waveProgressSlider.minValue = 0f;
        waveProgressSlider.maxValue = 1f;
        waveProgressSlider.value = 0f;
    }

    private void RefreshProgressSlider()
    {
        if (waveProgressSlider != null)
        {
            waveProgressSlider.value = WaveProgress;
        }
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
