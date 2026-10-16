
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerComponentUI : MonoBehaviour
{
    [Header("Player UI")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private TextMeshProUGUI playerExpText;
    [SerializeField] private Slider expSlider;

    private PlayerSystem playerSystem;

    private void Start()
    {
        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
        {
            Debug.LogError(
                "[PlayerComponentUI] PlayerSystem is not available!"
            );

            return;
        }

        playerSystem.OnPlayerNameChanged += UpdatePlayerName;
        playerSystem.OnPlayerExpChanged += HandleExpChanged;
        playerSystem.OnPlayerLevelChanged += HandleLevelChanged;

        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.interactable = false;
            expSlider.wholeNumbers = true;
        }

        RefreshUI();
    }

    private void UpdatePlayerName(string value)
    {
        if (playerNameText != null)
            playerNameText.text = value;
    }

    private void HandleExpChanged(int value)
    {
        RefreshUI();
    }

    private void HandleLevelChanged(int value)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (playerSystem == null)
            return;

        int level = playerSystem.PlayerLevel;
        int currentExp = playerSystem.CurrentLevelExp;
        int requiredExp = playerSystem.RequiredLevelExp;

        if (playerNameText != null)
            playerNameText.text = playerSystem.PlayerName;

        if (playerLevelText != null)
            playerLevelText.text = $"Level {level}";

        if (playerExpText != null)
            playerExpText.text = $"EXP: {currentExp} / {requiredExp}";

        if (expSlider != null)
        {
            expSlider.maxValue = requiredExp;
            expSlider.value = currentExp;
        }
    }

    private void OnDestroy()
    {
        if (playerSystem == null)
            return;

        playerSystem.OnPlayerNameChanged -= UpdatePlayerName;
        playerSystem.OnPlayerExpChanged -= HandleExpChanged;
        playerSystem.OnPlayerLevelChanged -= HandleLevelChanged;
    }
}
