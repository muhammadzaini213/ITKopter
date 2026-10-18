using UnityEngine;
using UnityEngine.UI;
using Slafurry.Systems.Pause;
using Slafurry.Systems.Scene;

public class GameplayPauseMenu : MonoBehaviour
{
    private const string PauseKey = "GameplayMenu";

    [Header("References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backToMenuButton;

    [Header("Scene References")]
    [SerializeField] private SceneReference gameplayScene;
    [SerializeField] private SceneReference mainMenuScene;

    private bool isMenuOpen;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(OpenPauseMenu);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(BackToMenu);
    }

    public void OpenPauseMenu()
    {
        if (isMenuOpen)
            return;

        isMenuOpen = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Pause.On(PauseKey);
    }

    public void ResumeGame()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        isMenuOpen = false;
        Pause.Off(PauseKey);
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning(
                "[GameplayPauseMenu] Settings Panel is not assigned.",
                this
            );
            return;
        }

        settingsPanel.SetActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (isMenuOpen && pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void RestartLevel()
    {
        if (gameplayScene == null)
        {
            Debug.LogError(
                "[GameplayPauseMenu] Gameplay Scene Reference is not assigned.",
                this
            );
            return;
        }

        isMenuOpen = false;
        Pause.Off(PauseKey);
        SceneSystem.Load(gameplayScene);
    }

    public void BackToMenu()
    {
        if (mainMenuScene == null)
        {
            Debug.LogError(
                "[GameplayPauseMenu] Main Menu Scene Reference is not assigned.",
                this
            );
            return;
        }

        isMenuOpen = false;
        Pause.Off(PauseKey);
        SceneSystem.Load(mainMenuScene);
    }

    private void OnDestroy()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(ResumeGame);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);

        if (backToMenuButton != null)
            backToMenuButton.onClick.RemoveListener(BackToMenu);

        Pause.Off(PauseKey);
    }
}