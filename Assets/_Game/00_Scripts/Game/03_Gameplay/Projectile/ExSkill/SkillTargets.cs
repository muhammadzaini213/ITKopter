using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Target queries shared by EX skills. Enemies come from the
/// <see cref="EnemyLifecycle"/> registry (never the player's own Health),
/// the party from <see cref="GameplayInitializer"/> (includes benched members,
/// which FindObjectsByType would skip because they are inactive).
/// </summary>
public static class SkillTargets
{
    private static GameplayInitializer initializer;
    private static readonly List<Health> buffer = new List<Health>();

    public static IReadOnlyList<Character> Party
    {
        get
        {
            if (initializer == null)
                initializer = Object.FindFirstObjectByType<GameplayInitializer>();

            return initializer != null && initializer.InitializedCharacters != null
                ? initializer.InitializedCharacters
                : System.Array.Empty<Character>();
        }
    }

    public static bool IsPartyMember(Character character)
    {
        if (character == null)
            return false;

        var party = Party;

        for (int i = 0; i < party.Count; i++)
        {
            if (party[i] == character)
                return true;
        }

        return false;
    }

    /// <summary>Alive enemies, optionally only those inside the camera view.</summary>
    public static List<Health> AliveEnemies(bool onScreenOnly = true)
    {
        buffer.Clear();

        Camera cam = Camera.main;

        foreach (var enemy in EnemyLifecycle.Active)
        {
            if (enemy == null || !enemy.IsAlive)
                continue;

            if (onScreenOnly && cam != null && !IsOnScreen(cam, enemy.transform.position))
                continue;

            buffer.Add(enemy.Health);
        }

        return buffer;
    }

    public static List<Health> EnemiesInRadius(Vector2 center, float radius, bool onScreenOnly = false)
    {
        var result = new List<Health>();
        float sqr = radius * radius;

        foreach (var health in AliveEnemies(onScreenOnly))
        {
            float extra = SkillVFX.GetVisualRadius(health.transform) * 0.5f;
            float reach = radius + extra;

            if (((Vector2)health.transform.position - center).sqrMagnitude <= Mathf.Max(sqr, reach * reach))
                result.Add(health);
        }

        return result;
    }

    public static Health HighestHealthEnemy(bool onScreenOnly = true)
    {
        Health best = null;

        foreach (var health in AliveEnemies(onScreenOnly))
        {
            if (best == null || health.CurrentHealth > best.CurrentHealth)
                best = health;
        }

        // Nothing on screen yet — fall back to anything alive.
        if (best == null && onScreenOnly)
            return HighestHealthEnemy(false);

        return best;
    }

    public static Health NearestEnemy(Vector2 from, ICollection<Health> among = null)
    {
        Health best = null;
        float bestSqr = float.MaxValue;

        IEnumerable<Health> source = among != null ? (IEnumerable<Health>)among : AliveEnemies(false);

        foreach (var health in source)
        {
            if (health == null || health.IsDead)
                continue;

            float sqr = ((Vector2)health.transform.position - from).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = health;
            }
        }

        return best;
    }

    public static void AddStatus(Health target, string effectId, float duration)
    {
        if (target == null || string.IsNullOrEmpty(effectId) || StatusEffectSystem.Instance == null)
            return;

        StatusEffectSystem.Instance.Add(target.gameObject, effectId, duration, stacks: 1, refreshOnReapply: true);
    }

    public static void RemoveStatus(Health target, string effectId)
    {
        if (target == null || string.IsNullOrEmpty(effectId) || StatusEffectSystem.Instance == null)
            return;

        StatusEffectSystem.Instance.Remove(target.gameObject, effectId);
    }

    private static bool IsOnScreen(Camera cam, Vector3 position)
    {
        Vector3 vp = cam.WorldToViewportPoint(position);
        return vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.05f && vp.y < 1.05f;
    }
}
