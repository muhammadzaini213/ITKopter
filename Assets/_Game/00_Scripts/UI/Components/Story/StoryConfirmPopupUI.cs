using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryConfirmPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Story Info")]
    [SerializeField] private TMP_Text storyNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private StoryData selectedStory;
    private Action<StoryData> onConfirm;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Hide);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Hide);
    }

    public void Show(StoryData story, Action<StoryData> confirmCallback)
    {
        if (story == null)
        {
            Debug.LogWarning(
                "[StoryConfirmPopupUI] Cannot open popup without a story.");
            return;
        }

        selectedStory = story;
        onConfirm = confirmCallback;

        if (storyNameText != null)
            storyNameText.text = story.StoryName;

        if (descriptionText != null)
            descriptionText.text = story.StoryDescription;

        if (popupRoot != null)
            popupRoot.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        selectedStory = null;
        onConfirm = null;
    }

    private void HandleConfirmClicked()
    {
        StoryData story = selectedStory;
        Action<StoryData> callback = onConfirm;

        Hide();
        callback?.Invoke(story);
    }
}
