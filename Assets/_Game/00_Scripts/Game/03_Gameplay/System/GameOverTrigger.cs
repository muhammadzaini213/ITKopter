
using UnityEngine;
using UnityEngine.Events;

public class GameOverTrigger : MonoBehaviour
{
    [Header("Character Life")]
    [SerializeField]
    private CharacterLifeController lifeController;

    [Header("Game Over UI")]
    [SerializeField]
    private GameObject gameOverPanel;

    [Header("Game Over Settings")]
    [SerializeField]
    private bool pauseGameOnGameOver = true;

    [SerializeField]
    private bool showPanelOnGameOver = true;

    [Header("Events")]
    [SerializeField]
    private UnityEvent onGameOver;

    public bool IsGameOver { get; private set; }

    private bool isSubscribed;

    private void Awake()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (lifeController == null)
        {
            lifeController =
                FindFirstObjectByType<CharacterLifeController>();
        }

        if (lifeController == null)
        {
            Debug.LogError(
                "[GameOverTrigger] CharacterLifeController was not found.",
                this);

            enabled = false;
            return;
        }

        if (lifeController.IsInitialized)
            SubscribeToCharacterLife();
    }

    private void Update()
    {
        if (IsGameOver || lifeController == null)
            return;

        if (!lifeController.IsInitialized)
            return;

        if (!isSubscribed)
            SubscribeToCharacterLife();

        CheckGameOver();
    }

    private void SubscribeToCharacterLife()
    {
        if (isSubscribed || lifeController == null)
            return;

        lifeController.CharacterDefeated +=
            HandleCharacterDefeated;

        isSubscribed = true;

        CheckGameOver();
    }

    private void HandleCharacterDefeated(int characterIndex)
    {
        if (IsGameOver)
            return;

        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (IsGameOver ||
            lifeController == null ||
            !lifeController.IsInitialized)
        {
            return;
        }

        if (lifeController.AllCharactersDefeated)
            TriggerGameOver();
    }

    public void TriggerGameOver()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;

        Debug.Log(
            "[GameOverTrigger] Game Over.",
            this);

        if (showPanelOnGameOver && gameOverPanel != null)
            gameOverPanel.SetActive(true);

        onGameOver?.Invoke();

        if (pauseGameOnGameOver)
            Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        if (isSubscribed && lifeController != null)
        {
            lifeController.CharacterDefeated -=
                HandleCharacterDefeated;
        }
    }
}
