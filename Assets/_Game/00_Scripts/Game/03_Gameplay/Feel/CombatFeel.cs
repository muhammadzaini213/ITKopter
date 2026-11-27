using System.Collections;
using UnityEngine;

/// <summary>
/// Static facade for combat juice: hitstop, camera shake and screen flash.
/// Backed by a scene-bound <see cref="CombatFeelRunner"/> that is created on
/// first use (no scene setup needed) and dies with the gameplay scene.
/// Every call is pause-aware and safe to spam: overlapping requests merge.
/// </summary>
public static class CombatFeel
{
    /// <summary>Global kill switch (e.g. for an accessibility option).</summary>
    public static bool Enabled = true;

    /// <summary>Scales every shake request (0 = no shake).</summary>
    public static float ShakeScale = 1f;

    private static CombatFeelRunner runner;

    public static CombatFeelRunner Runner
    {
        get
        {
            if (runner == null)
                runner = CombatFeelRunner.Create();

            return runner;
        }
    }

    /// <summary>The live runner, or null — never creates one.</summary>
    public static CombatFeelRunner Current => runner;

    /// <summary>Make sure the runner (and its combat event reactions) exists.</summary>
    public static void Ensure() => _ = Runner;

    /// <summary>Freeze time almost completely for <paramref name="duration"/> real seconds.</summary>
    public static void HitStop(float duration, float timeScale = 0.02f)
    {
        if (!Enabled || duration <= 0f)
            return;

        Runner.HitStop(duration, timeScale);
    }

    /// <summary>
    /// Slow-motion "skill moment": time drops to <paramref name="timeScale"/>,
    /// holds, then eases back. Optional cinematic letterbox bars.
    /// </summary>
    public static void SlowMo(float duration, float timeScale = 0.2f, bool letterbox = true)
    {
        if (!Enabled || duration <= 0f)
            return;

        Runner.SlowMo(duration, timeScale, letterbox);
    }

    /// <summary>Shake the camera. Strength is the peak offset in world units.</summary>
    public static void Shake(float strength, float duration = 0.2f)
    {
        if (!Enabled || strength <= 0f || duration <= 0f || ShakeScale <= 0f)
            return;

        Runner.Shake(strength * ShakeScale, duration);
    }

    /// <summary>Full-screen colour flash that fades out over <paramref name="duration"/>.</summary>
    public static void Flash(Color color, float duration = 0.15f)
    {
        if (!Enabled || duration <= 0f || color.a <= 0f)
            return;

        Runner.Flash(color, duration);
    }

    /// <summary>
    /// Runs a coroutine on the scene runner — survives the caller being
    /// disabled (e.g. a team EX skill whose owner was switched out).
    /// </summary>
    public static Coroutine Run(IEnumerator routine)
    {
        return routine != null ? Runner.StartCoroutine(routine) : null;
    }

    public static void Stop(Coroutine routine)
    {
        if (routine != null && runner != null)
            runner.StopCoroutine(routine);
    }

    // ---- presets -------------------------------------------------------

    public static void LightImpact()
    {
        HitStop(0.03f);
        Shake(0.06f, 0.1f);
    }

    public static void MediumImpact()
    {
        HitStop(0.06f);
        Shake(0.16f, 0.18f);
    }

    public static void HeavyImpact()
    {
        HitStop(0.12f);
        Shake(0.32f, 0.35f);
    }
}
