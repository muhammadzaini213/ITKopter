using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Notification badge on the main menu Story button: how many stage-gated
/// chapters are unlocked but not played yet. Works like ObjectiveBadgeUI.
///
/// Attaches itself to any active GameObject named "StoryBtn" when a scene
/// loads, and, when no badge object is assigned, copies the Objective
/// button's badge so both look the same. No scene edit needed.
/// </summary>
public class StoryBadgeUI : MonoBehaviour
{
    public const string StoryButtonName = "StoryBtn";

    [SerializeField] private GameObject badge;
    [SerializeField] private TMP_Text countText;

    private PlayerSystem player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == StoryButtonName && t.GetComponent<StoryBadgeUI>() == null)
                    t.gameObject.AddComponent<StoryBadgeUI>();
            }
        }
    }

    private void Start()
    {
        if (badge == null)
            BuildFromObjectiveBadge();

        player = PlayerSystem.Instance;

        if (player != null)
        {
            player.OnLevelProgressChanged += Refresh;
            player.OnPlayerDataReady += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnLevelProgressChanged -= Refresh;
            player.OnPlayerDataReady -= Refresh;
        }
    }

    private void Refresh()
    {
        int count = StoryProgress.NewCount(player);

        if (badge != null)
            badge.SetActive(count > 0);

        if (countText != null)
            countText.text = count > 9 ? "9+" : count.ToString();
    }

    // Copy the objective badge (same art, same corner) onto this button.
    private void BuildFromObjectiveBadge()
    {
        ObjectiveBadgeUI objective = FindFirstObjectByType<ObjectiveBadgeUI>(FindObjectsInactive.Include);

        if (objective == null || objective.Badge == null)
            return;

        RectTransform source = objective.Badge.transform as RectTransform;
        GameObject copy = Instantiate(objective.Badge, transform, false);
        copy.name = "StoryBadge";

        foreach (ObjectiveBadgeUI stray in copy.GetComponentsInChildren<ObjectiveBadgeUI>(true))
            Destroy(stray);

        if (source != null && copy.transform is RectTransform rt)
        {
            rt.anchorMin = source.anchorMin;
            rt.anchorMax = source.anchorMax;
            rt.pivot = source.pivot;
            rt.anchoredPosition = source.anchoredPosition;
            rt.sizeDelta = source.sizeDelta;
        }

        badge = copy;
        countText = copy.GetComponentInChildren<TMP_Text>(true);
        copy.transform.SetAsLastSibling();
    }
}
