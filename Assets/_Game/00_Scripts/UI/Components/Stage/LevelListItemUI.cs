
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListItemUI : MonoBehaviour
{
    [Header("Level Info")]
    [SerializeField] private TMP_Text levelNameText;
    [SerializeField] private TMP_Text energyCostText;

    [Header("Reward Icons")]
    [SerializeField] private Transform rewardIconsRoot;
    [SerializeField] private Image rewardIconPrefab;

    [Header("Status")]
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject completedIndicator;

    [Header("Button")]
    [SerializeField] private Button startButton;

    private LevelData levelData;
    private Action<LevelData> onStartClicked;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(HandleStartClicked);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(HandleStartClicked);
    }

    public void Setup(
        LevelData data,
        bool isUnlocked,
        bool isCompleted,
        Action<LevelData> startCallback)
    {
        levelData = data;
        onStartClicked = startCallback;

        if (levelData == null)
            return;

        if (levelNameText != null)
            levelNameText.text = levelData.LevelName;

        if (energyCostText != null)
            energyCostText.text = levelData.EnergyCost.ToString();

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isUnlocked);

        if (completedIndicator != null)
            completedIndicator.SetActive(isCompleted);

        if (startButton != null)
            startButton.interactable = isUnlocked;

        RefreshRewardIcons();
    }

    private void RefreshRewardIcons()
    {
        if (rewardIconsRoot == null || rewardIconPrefab == null)
            return;

        foreach (Transform child in rewardIconsRoot)
            Destroy(child.gameObject);

        if (levelData == null || levelData.LevelRewards == null)
            return;

        foreach (RewardData reward in levelData.LevelRewards)
        {
            if (reward == null || reward.Icon == null)
                continue;

            Image icon = Instantiate(rewardIconPrefab, rewardIconsRoot);
            icon.sprite = reward.Icon;
            icon.enabled = true;
        }
    }

    private void HandleStartClicked()
    {
        if (levelData != null)
            onStartClicked?.Invoke(levelData);
    }
}
