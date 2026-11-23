using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryListItemUI : MonoBehaviour
{
    [Header("Story Info")]
    [SerializeField] private TMP_Text storyNameText;

    [Header("Status")]
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject completedIndicator;

    [Header("Button")]
    [SerializeField] private Button startButton;

    private StoryData storyData;
    private Action<StoryData> onStartClicked;

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
        StoryData data,
        bool isUnlocked,
        bool isCompleted,
        Action<StoryData> startCallback)
    {
        storyData = data;
        onStartClicked = startCallback;

        if (storyData == null)
            return;

        if (storyNameText != null)
        {
            // "CHAPTER 3  The Loudest Thing in the Room" — label small and cyan.
            storyNameText.text = string.IsNullOrWhiteSpace(storyData.ChapterLabel)
                ? storyData.StoryName
                : $"<size=60%><color=#19F0FF>{storyData.ChapterLabel}</color></size>  {storyData.StoryName}";
        }

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isUnlocked);

        if (completedIndicator != null)
            completedIndicator.SetActive(isCompleted);

        if (startButton != null)
            startButton.interactable = isUnlocked;
    }

    private void HandleStartClicked()
    {
        if (storyData != null)
            onStartClicked?.Invoke(storyData);
    }
}
