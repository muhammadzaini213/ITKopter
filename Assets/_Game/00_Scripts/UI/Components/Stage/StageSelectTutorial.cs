using UnityEngine;

/// <summary>
/// First visit to the stage select: Ei-Pi and Noz explain picking a stage and a team,
/// and the first playable stage glows. Done once the player starts a stage.
/// </summary>
public class StageSelectTutorial : MonoBehaviour
{
    /// <summary>Saved with the completed level/story ids.</summary>
    public const string TutorialId = "tutorial_stage_select";

    [SerializeField] private LevelSelectionUI levelSelection;
    [SerializeField] private Transform levelListRoot;
    [SerializeField] private GameObject teamPopup;

    [SerializeField] private CharacterData eiPi;
    [SerializeField] private CharacterData noz;

    private bool running;
    private bool teamTipsShown;
    private GameObject highlighted;

    public static bool IsDone =>
        PlayerSystem.Instance != null && PlayerSystem.Instance.IsPlayerDataLoaded &&
        PlayerSystem.Instance.IsLevelCompleted(TutorialId);

    private void Start()
    {
        if (levelSelection != null)
            levelSelection.OnLevelStartRequested += HandleLevelStart;

        Invoke(nameof(Begin), 0.5f);
    }

    private void OnDestroy()
    {
        if (levelSelection != null)
            levelSelection.OnLevelStartRequested -= HandleLevelStart;
    }

    private void Begin()
    {
        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded || IsDone)
            return;

        running = true;

        StorySystem.Instance?.Toasts?.PlaySequence(new[]
        {
            Line(eiPi, "THIS IS THE STAGE LIST!!! PICK THE GLOWING ONE!!!", 0.2f),
            Line(eiPi, "THE NUMBER ON THE RIGHT IS THE ENERGY COST!!! ENERGY REFILLS OVER TIME!!!", 0.3f),
            Line(eiPi, "EVERY THIRD STAGE HAS A BOSS!!! A REAL CONCEPT SPIRIT!!! SCARY!!! FUN!!!", 0.3f),
        });
    }

    private void Update()
    {
        if (!running)
            return;

        // The list is rebuilt when progress loads, so keep pointing at the current first stage.
        if (highlighted == null && levelListRoot != null)
        {
            foreach (Transform child in levelListRoot)
            {
                if (child.TryGetComponent(out LevelListItemUI item) && item.IsUnlocked && !item.IsCompleted)
                {
                    highlighted = child.gameObject;
                    UIAttentionPulse.Attach(highlighted);
                    break;
                }
            }
        }

        if (!teamTipsShown && teamPopup != null && teamPopup.activeInHierarchy)
        {
            teamTipsShown = true;
            StoryToastUI toasts = StorySystem.Instance?.Toasts;

            if (toasts != null)
            {
                toasts.HideAll();
                toasts.PlaySequence(new[]
                {
                    Line(noz, "Choose up to three units. The first one you pick starts on the field.", 0.1f),
                    Line(noz, "Mix your roles: a Protector keeps the team standing, a Support makes it stronger.", 0.3f),
                    Line(eiPi, "THEN HIT START!!! GO GO GO!!!", 0.3f),
                });
            }
        }
    }

    private void HandleLevelStart(LevelData level, System.Collections.Generic.IReadOnlyList<CharacterData> team)
    {
        if (!running)
            return;

        running = false;
        UIAttentionPulse.Detach(highlighted);
        StorySystem.Instance?.Toasts?.HideAll();

        if (PlayerSystem.Instance != null)
            PlayerSystem.Instance.CompleteLevel(TutorialId);
    }

    private static StoryToastLine Line(CharacterData speaker, string text, float delay) =>
        new StoryToastLine { speaker = speaker, text = text, time = delay, duration = 4.5f };
}
