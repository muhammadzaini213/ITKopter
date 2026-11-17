using UnityEngine;

public class StorySceneController : MonoBehaviour
{
    [Tooltip("Where the story prefab spawns. Null = this transform.")]
    [SerializeField] private Transform contentRoot;

    private GameObject spawnedStory;

    private void Start()
    {
        SpawnSelectedStory();
    }

    public void SpawnSelectedStory()
    {
        if (spawnedStory != null)
        {
            Destroy(spawnedStory);
            spawnedStory = null;
        }

        StoryData story = StorySession.Selected;

        if (story == null)
        {
            Debug.LogWarning(
                "[StorySceneController] StorySession.Selected is null. " +
                "Enter via StoryConfirmPopupUI, or assign a story first.");
            return;
        }

        if (story.StoryPrefab == null)
        {
            Debug.LogWarning(
                $"[StorySceneController] '{story.StoryName}' has no prefab assigned.");
            return;
        }

        Transform parent = contentRoot != null ? contentRoot : transform;
        spawnedStory = Instantiate(story.StoryPrefab, parent);
    }
}
