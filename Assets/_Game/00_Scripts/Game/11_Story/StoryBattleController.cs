using System.Collections;
using UnityEngine;

/// <summary>
/// Added to the gameplay scene by GameplayManager while a story battle runs.
/// Shows the segment's battle toasts, ends scripted-loss battles (party down or
/// timer), and hands the result back to <see cref="StorySystem"/>.
/// </summary>
public class StoryBattleController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float resultHoldSeconds = 2.2f;

    private StorySegment segment;
    private bool ended;
    private float elapsed;

    public static StoryBattleController Active { get; private set; }

    private void Awake()
    {
        Active = this;
        segment = StorySystem.Instance != null ? StorySystem.Instance.CurrentSegment : null;
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    private void Start()
    {
        if (segment == null)
            return;

        if (segment.battleToasts != null && segment.battleToasts.Length > 0)
            StartCoroutine(ToastRoutine());
    }

    private void Update()
    {
        if (ended || segment == null || !segment.scriptedLoss)
            return;

        // Scaled time: the pause menu also pauses the scripted timer.
        elapsed += Time.deltaTime;

        if (elapsed >= segment.scriptedLossSeconds)
            EndBattle(false);
    }

    private IEnumerator ToastRoutine()
    {
        StoryToastLine[] toasts = (StoryToastLine[])segment.battleToasts.Clone();
        System.Array.Sort(toasts, (a, b) => a.time.CompareTo(b.time));

        float clock = 0f;

        foreach (StoryToastLine toast in toasts)
        {
            if (toast == null)
                continue;

            while (clock < toast.time)
            {
                if (ended)
                    yield break;

                clock += Time.deltaTime;
                yield return null;
            }

            if (ended)
                yield break;

            StoryToastUI ui = StorySystem.Instance != null ? StorySystem.Instance.Toasts : null;

            if (ui != null)
                ui.Show(toast);
        }
    }

    /// <summary>All waves cleared.</summary>
    public void HandleVictory()
    {
        EndBattle(true);
    }

    /// <summary>The party fell. Returns true when the story takes over (scripted loss).</summary>
    public bool HandleDefeat()
    {
        if (segment == null || !segment.scriptedLoss)
            return false;

        EndBattle(false);
        return true;
    }

    private void EndBattle(bool won)
    {
        if (ended)
            return;

        ended = true;
        StartCoroutine(EndRoutine(won));
    }

    private IEnumerator EndRoutine(bool won)
    {
        Time.timeScale = 0f;

        StoryToastUI ui = StorySystem.Instance != null ? StorySystem.Instance.Toasts : null;

        if (ui != null)
        {
            ui.HideAll();
            ui.ShowBanner(won ? "VICTORY" : "DEFEAT", won);
        }

        yield return new WaitForSecondsRealtime(resultHoldSeconds);

        if (ui != null)
            ui.HideBanner();

        if (StorySystem.Instance != null)
            StorySystem.Instance.ReportBattleResult(won);
        else
            Time.timeScale = 1f;
    }
}
