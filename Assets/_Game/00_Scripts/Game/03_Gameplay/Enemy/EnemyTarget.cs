
using UnityEngine;

// Central lookup for "the player" read at use time (aimed shots, Dive lock).
public static class EnemyTarget
{
    private static CharacterSwitcher switcher;

    public static Transform Get()
    {
        if (switcher == null)
            switcher = Object.FindFirstObjectByType<CharacterSwitcher>();

        if (switcher == null)
            return null;

        Character character = switcher.ActiveCharacterComponent;
        return character != null ? character.transform : null;
    }

    public static Vector2 Position(Vector2 fallback)
    {
        Transform target = Get();
        return target != null ? (Vector2)target.position : fallback;
    }

    public static Vector2 DirectionFrom(Vector2 origin, Vector2 fallbackDirection)
    {
        Transform target = Get();
        if (target == null)
            return fallbackDirection;

        Vector2 toPlayer = (Vector2)target.position - origin;
        return toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : fallbackDirection;
    }
}
