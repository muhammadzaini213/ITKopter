using UnityEngine;

[CreateAssetMenu(fileName = "StoryData", menuName = "Scriptable Objects/StoryData")]
public class StoryData : ScriptableObject
{
    [Header("Story Info")]
    [SerializeField] private string storyId;
    [SerializeField] private string storyName;
    [SerializeField, TextArea] private string storyDescription;

    [Tooltip("Leave empty if this story is unlocked from the beginning.")]
    [SerializeField] private StoryData previousStory;

    [Header("Content")]
    [Tooltip("Prefab instantiated by StorySceneController inside 11_StoryScene.")]
    [SerializeField] private GameObject storyPrefab;

    [Header("Rewards")]
    [Tooltip("Granted when the story ends. Not shown in the confirm popup.")]
    [SerializeField] private RewardData[] storyRewards;

    [Header("Time Limit")]
    [Tooltip("Maximum time allowed to complete this story, in seconds.")]
    [SerializeField, Min(1)] private int timeLimitSeconds = 180;

    [Header("Music")]
    [SerializeField] private MusicReference storyMusic;

    public string StoryId => storyId;
    public string StoryName => storyName;
    public string StoryDescription => storyDescription;
    public StoryData PreviousStory => previousStory;
    public GameObject StoryPrefab => storyPrefab;
    public RewardData[] StoryRewards => storyRewards;
    public int TimeLimitSeconds => timeLimitSeconds;
    public MusicReference StoryMusic => storyMusic;
}
