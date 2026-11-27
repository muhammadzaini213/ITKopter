using System.Collections;
using UnityEngine;

/// <summary>
/// Added to the gameplay scene by GameplayManager while a story battle runs.
/// Shows the segment's battle toasts, ends scripted-loss battles (party down or
/// timer), and hands the result back to <see cref="StorySystem"/>.
///
/// Toasts with an event trigger (perfect switch, switching window,
/// Annihilator, ...) play in list order: each group of consecutive lines with
/// the same trigger waits for its event after the previous group has played.
/// A SwitchDrill group stages a perfect switch (slow-mo + a guaranteed window)
/// and repeats if the player misses it.
/// </summary>
public class StoryBattleController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float resultHoldSeconds = 2.2f;

    [Header("Switch Drill")]
    [SerializeField, Min(0f), Tooltip("An enemy bullet this close to the active unit starts the drill.")]
    private float drillBulletRadius = 2.4f;

    [SerializeField, Min(0f), Tooltip("Start the drill anyway after this long without a close bullet.")]
    private float drillFallbackSeconds = 8f;

    [SerializeField, Min(0f), Tooltip("Real-time window in which any switch counts as perfect.")]
    private float drillWindowSeconds = 3f;

    [SerializeField, Min(0f)] private float drillSlowMoSeconds = 1.8f;
    [SerializeField, Range(0.05f, 1f)] private float drillSlowMoScale = 0.2f;

    [SerializeField, Min(0f), Tooltip("Repeat the drill when no perfect switch happens within this long.")]
    private float drillRetrySeconds = 7f;

    private StorySegment segment;
    private bool ended;
    private float elapsed;
    private float battleClock;
    private CharacterSwitcher switcher;

    private StoryToastTrigger armedTrigger = StoryToastTrigger.Time;
    private bool armed;
    private bool armedFired;

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

        SwitchRewards.PerfectSwitchPerformed -= OnPerfectSwitch;
        SwitchRewards.CounterTagInPerformed -= OnCounterTagIn;
        CharacterSwitcher.ManualSwitchStarted -= OnSwitchStarted;
        AnnihilatorManifestation.HierarchyChoiceOpened -= OnAnnihilatorChoice;
        AnnihilatorManifestation.Manifested -= OnAnnihilator;
    }

    private void Start()
    {
        if (segment == null)
            return;

        switcher = FindFirstObjectByType<CharacterSwitcher>();

        SwitchRewards.PerfectSwitchPerformed += OnPerfectSwitch;
        SwitchRewards.CounterTagInPerformed += OnCounterTagIn;
        CharacterSwitcher.ManualSwitchStarted += OnSwitchStarted;
        AnnihilatorManifestation.HierarchyChoiceOpened += OnAnnihilatorChoice;
        AnnihilatorManifestation.Manifested += OnAnnihilator;

        if (segment.battleToasts != null && segment.battleToasts.Length > 0)
        {
            StartCoroutine(ToastRoutine());
            StartCoroutine(CueRoutine());
        }
    }

    private void OnPerfectSwitch() => Fire(StoryToastTrigger.PerfectSwitch);
    private void OnCounterTagIn() => Fire(StoryToastTrigger.CounterTagIn);
    private void OnSwitchStarted() => Fire(StoryToastTrigger.SwitchingWindow);
    private void OnAnnihilatorChoice() => Fire(StoryToastTrigger.AnnihilatorChoice);
    private void OnAnnihilator() => Fire(StoryToastTrigger.Annihilator);

    private void Fire(StoryToastTrigger trigger)
    {
        if (armed && armedTrigger == trigger)
            armedFired = true;
    }

    private void Update()
    {
        if (!ended)
            battleClock += Time.deltaTime;

        if (ended || segment == null || !segment.scriptedLoss)
            return;

        // Scaled time: the pause menu also pauses the scripted timer.
        elapsed += Time.deltaTime;

        if (elapsed >= segment.scriptedLossSeconds)
            EndBattle(false);
    }

    private IEnumerator ToastRoutine()
    {
        var timed = new System.Collections.Generic.List<StoryToastLine>();

        foreach (StoryToastLine line in segment.battleToasts)
        {
            if (line != null && line.trigger == StoryToastTrigger.Time)
                timed.Add(line);
        }

        StoryToastLine[] toasts = timed.ToArray();
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

    // ---- event cues ----------------------------------------------------------

    private IEnumerator CueRoutine()
    {
        var cues = new System.Collections.Generic.List<StoryToastLine>();

        foreach (StoryToastLine line in segment.battleToasts)
        {
            if (line != null && line.trigger != StoryToastTrigger.Time)
                cues.Add(line);
        }

        int index = 0;
        bool preFired = false;

        while (index < cues.Count && !ended)
        {
            StoryToastTrigger trigger = cues[index].trigger;
            int groupEnd = index;

            while (groupEnd < cues.Count && cues[groupEnd].trigger == trigger)
                groupEnd++;

            while (battleClock < cues[index].notBefore)
            {
                if (ended) yield break;
                yield return null;
            }

            if (!preFired)
            {
                Arm(trigger);
                float armedAt = battleClock;

                while (!armedFired)
                {
                    if (ended) yield break;

                    if (trigger == StoryToastTrigger.SwitchReady && switcher != null && switcher.CanSwitch)
                        armedFired = true;
                    else if (trigger == StoryToastTrigger.SwitchDrill &&
                             (BulletNearActive() || battleClock - armedAt >= drillFallbackSeconds) &&
                             switcher != null && switcher.ActiveCharacter != null && !switcher.IsSwitching)
                        armedFired = true;

                    yield return null;
                }

                armed = false;
            }

            preFired = false;

            if (trigger == StoryToastTrigger.SwitchDrill)
                StartDrill();

            yield return PlayGroup(cues, index, groupEnd);

            // Missed the drill: run it again until the player lands a perfect switch.
            if (trigger == StoryToastTrigger.SwitchDrill &&
                groupEnd < cues.Count && cues[groupEnd].trigger == StoryToastTrigger.PerfectSwitch)
            {
                Arm(StoryToastTrigger.PerfectSwitch);
                float waitStart = battleClock;

                while (!armedFired && battleClock - waitStart < drillRetrySeconds)
                {
                    if (ended) yield break;
                    yield return null;
                }

                armed = false;

                if (!armedFired)
                    continue; // same index: drill again

                preFired = true;
            }

            index = groupEnd;
        }
    }

    private void Arm(StoryToastTrigger trigger)
    {
        armedTrigger = trigger;
        armedFired = false;
        armed = true;
    }

    private IEnumerator PlayGroup(System.Collections.Generic.List<StoryToastLine> cues, int from, int to)
    {
        // Real time: the drill runs in slow motion.
        float start = Time.unscaledTime;

        for (int i = from; i < to; i++)
        {
            while (Time.unscaledTime - start < cues[i].time)
            {
                if (ended) yield break;
                yield return null;
            }

            StoryToastUI ui = StorySystem.Instance != null ? StorySystem.Instance.Toasts : null;
            if (ui == null)
                continue;

            // A reaction: the first line cuts in, the rest go ahead of timed tips.
            if (i == from)
                ui.Interrupt(cues[i]);
            else
                ui.ShowPriority(cues[i]);
        }
    }

    private void StartDrill()
    {
        if (switcher == null)
            return;

        switcher.ResetCooldown();
        switcher.Rewards.OpenPerfectWindow(drillWindowSeconds);
        CombatFeel.SlowMo(drillSlowMoSeconds, drillSlowMoScale, letterbox: false);
    }

    private bool BulletNearActive()
    {
        GameObject active = switcher != null ? switcher.ActiveCharacter : null;
        if (active == null)
            return false;

        Vector2 center = active.transform.position;
        float sqr = drillBulletRadius * drillBulletRadius;

        foreach (var projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            if (projectile != null && projectile.isActiveAndEnabled && projectile.IsHostileToPlayer &&
                ((Vector2)projectile.transform.position - center).sqrMagnitude <= sqr)
                return true;
        }

        return false;
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
