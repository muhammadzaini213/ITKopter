using UnityEngine;

/// <summary>
/// Main menu: starts (or resumes) the opening story chapters for players who
/// haven't finished onboarding, once they've entered their name.
/// </summary>
public class OnboardingLauncher : MonoBehaviour
{
    [SerializeField, Min(0f)] private float startDelay = 0.4f;

    [Header("After Onboarding")]
    [Tooltip("Glows until the player has done the stage select tutorial.")]
    [SerializeField] private GameObject combatButton;
    [SerializeField] private CharacterData combatHintSpeaker;

    private PlayerSystem playerSystem;
    private bool launched;

    private void Start()
    {
        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
            return;

        playerSystem.OnPlayerDataReady += TryLaunch;
        playerSystem.OnPlayerNameChanged += HandleNameChanged;

        Invoke(nameof(TryLaunch), startDelay);
    }

    private void OnDestroy()
    {
        if (playerSystem == null)
            return;

        playerSystem.OnPlayerDataReady -= TryLaunch;
        playerSystem.OnPlayerNameChanged -= HandleNameChanged;
    }

    private void HandleNameChanged(string _)
    {
        Invoke(nameof(TryLaunch), startDelay);
    }

    private void TryLaunch()
    {
        StorySystem story = StorySystem.Instance;

        if (launched || story == null || playerSystem == null ||
            !playerSystem.IsPlayerDataLoaded || playerSystem.NeedsNameEntry ||
            story.IsBusy)
        {
            return;
        }

        StoryData next = story.NextOnboardingChapter;

        if (next == null)
        {
            PointToCombat();
            return;
        }

        launched = true;
        story.StartStory(next, true);
    }

    // Onboarding done but no stage played yet: point at COMBAT.
    private void PointToCombat()
    {
        if (launched || StageSelectTutorial.IsDone)
            return;

        launched = true;
        UIAttentionPulse.Attach(combatButton);

        StorySystem.Instance?.Toasts?.Show(new StoryToastLine
        {
            speaker = combatHintSpeaker,
            text = "READY FOR REAL FIGHTS??? HIT COMBAT!!! BOTTOM RIGHT!!!",
            duration = 5f
        });
    }
}
