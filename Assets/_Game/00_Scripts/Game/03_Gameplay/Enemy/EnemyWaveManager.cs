
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Slider waveProgressSlider;

    [SerializeField, Tooltip("Title above the wave bar (\"Wave 2/3\", \"Boss: Regu - Phase 2\"). " +
                             "Empty = the text next to the wave slider that starts with \"Wave\".")]
    private TMP_Text waveTitleText;

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
        FindWaveTitle();
    }

    // ---- wave title ---------------------------------------------------

    private EnemyBoss currentBoss;
    private EnemyShooter currentBossShooter;
    private bool shownPhase2;
    private Coroutine titlePunch;

    private void FindWaveTitle()
    {
        if (waveTitleText != null || waveProgressSlider == null)
            return;

        // The scene's static "Wave 1: ..." label sits beside the slider.
        Transform scope = waveProgressSlider.transform.parent;

        for (int depth = 0; depth < 2 && scope != null && waveTitleText == null; depth++)
        {
            foreach (TMP_Text text in scope.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text != null && !text.transform.IsChildOf(waveProgressSlider.transform) &&
                    text.text != null && text.text.TrimStart().StartsWith("Wave", StringComparison.OrdinalIgnoreCase))
                {
                    waveTitleText = text;
                    break;
                }
            }

            scope = scope.parent;
        }
    }

    private void SetWaveTitle(string value)
    {
        if (waveTitleText == null || waveTitleText.text == value)
            return;

        waveTitleText.text = value;

        if (titlePunch != null)
            StopCoroutine(titlePunch);

        titlePunch = StartCoroutine(PunchTitle());
    }

    private IEnumerator PunchTitle()
    {
        Transform target = waveTitleText.transform;
        float t = 0f;

        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.3f;
            target.localScale = Vector3.one * (k < 0.35f ? Mathf.Lerp(1f, 1.18f, k / 0.35f) : Mathf.Lerp(1.18f, 1f, (k - 0.35f) / 0.65f));
            yield return null;
        }

        target.localScale = Vector3.one;
        titlePunch = null;
    }

    private void ShowWaveTitle(EnemyCollection collection)
    {
        currentBoss = collection != null ? collection.GetComponentInChildren<EnemyBoss>(true) : null;
        currentBossShooter = currentBoss != null ? currentBoss.GetComponent<EnemyShooter>() : null;
        shownPhase2 = false;

        if (currentBoss != null)
        {
            SetWaveTitle($"Boss: {currentBoss.DisplayName}");
            return;
        }

        int number = Mathf.Clamp(completedWaves + 1, 1, Mathf.Max(1, totalWaves));
        SetWaveTitle(totalWaves > 0 ? $"Wave {number}/{totalWaves}" : $"Wave {number}");
    }

    private void Update()
    {
        // Boss crossed its HP threshold → Phase 2.
        if (!shownPhase2 && currentBoss != null && currentBossShooter != null && currentBossShooter.IsPhase2)
        {
            shownPhase2 = true;
            SetWaveTitle($"Boss: {currentBoss.DisplayName} - Phase 2");
        }
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

                ShowWaveTitle(currentCollection);
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

        currentBoss = null;
        SetWaveTitle("Cleared!");

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
