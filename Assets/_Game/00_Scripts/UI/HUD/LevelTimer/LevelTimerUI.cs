
using TMPro;
using UnityEngine;

public class LevelTimerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameplayInitializer gameplayInitializer;

    private float remainingTime;
    private bool isRunning;
    private bool hasInitialized;

    public float RemainingTime => remainingTime;
    public bool IsRunning => isRunning;

    private void Start()
    {
        if (timerText == null)
        {
            Debug.LogError(
                "[LevelTimerUI] Timer Text is not assigned.",
                this);
            enabled = false;
            return;
        }

        if (gameplayInitializer == null)
        {
            gameplayInitializer =
                FindFirstObjectByType<GameplayInitializer>();
        }

        if (gameplayInitializer == null)
        {
            Debug.LogError(
                "[LevelTimerUI] GameplayInitializer was not found.",
                this);
            enabled = false;
            return;
        }

        InitializeTimer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime);

        RefreshTimerText();

        if (remainingTime <= 0f)
        {
            isRunning = false;

            Debug.Log(
                "[LevelTimerUI] Level time limit reached.",
                this);
        }
    }

    private void InitializeTimer()
    {
        LevelData currentLevel = gameplayInitializer.CurrentLevel;

        if (currentLevel == null)
        {
            Debug.LogError(
                "[LevelTimerUI] Current level is not initialized yet.",
                this);
            return;
        }

        remainingTime = currentLevel.TimeLimitSeconds;
        hasInitialized = true;

        RefreshTimerText();
        isRunning = true;
    }

    private void RefreshTimerText()
    {
        if (timerText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void StartTimer()
    {
        if (!hasInitialized)
        {
            InitializeTimer();
            return;
        }

        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        if (gameplayInitializer == null ||
            gameplayInitializer.CurrentLevel == null)
        {
            return;
        }

        remainingTime =
            gameplayInitializer.CurrentLevel.TimeLimitSeconds;

        RefreshTimerText();
        isRunning = true;
        hasInitialized = true;
    }
}
