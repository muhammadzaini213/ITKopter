
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Slafurry.Systems.Scene;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneReference gameScene;
    [SerializeField] private SceneReference stageSelectScene;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryGame);
        else
            Debug.LogWarning(
                "[GameOverUI] Retry Button is not assigned.",
                this);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
        else
            Debug.LogWarning(
                "[GameOverUI] Exit Button is not assigned.",
                this);
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;

        SceneSystem.Load(gameScene);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;

        Debug.Log("[GameOverUI] Exiting game.");
        SceneSystem.Load(stageSelectScene);
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(RetryGame);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
    }
}
