
using UnityEngine;

// Counter Mechanics (spec): Hierarchy matchup damage multipliers.
//
// Trinity cycle:  ABSTRACT > SIMPLE > SPECIAL > ABSTRACT (2.0x / 0.4x back)
// Duality:        PRIMORDIAL <-> ANOMALY are each other's weakness (0.4x
//                 both directions; neutral vs everything else)
// All other pairings (incl. same type): 1.0x
//
// Missing attacker or target type (no Character, no EnemyDataBinder/SO)
// resolves to neutral — the system never blocks damage.
public static class CounterMechanics
{
    public const float StrongMultiplier = 2.0f;
    public const float NeutralMultiplier = 1.0f;
    public const float WeakMultiplier = 0.4f;

    public static float GetMultiplier(
        CharacterType attacker, CharacterType target)
    {
        switch (attacker)
        {
            case CharacterType.ABSTRACT:
                if (target == CharacterType.SIMPLE)
                    return StrongMultiplier;
                if (target == CharacterType.SPECIAL)
                    return WeakMultiplier;
                break;

            case CharacterType.SIMPLE:
                if (target == CharacterType.SPECIAL)
                    return StrongMultiplier;
                if (target == CharacterType.ABSTRACT)
                    return WeakMultiplier;
                break;

            case CharacterType.SPECIAL:
                if (target == CharacterType.ABSTRACT)
                    return StrongMultiplier;
                if (target == CharacterType.SIMPLE)
                    return WeakMultiplier;
                break;

            case CharacterType.PRIMORDIAL:
                if (target == CharacterType.ANOMALY)
                    return WeakMultiplier;
                break;

            case CharacterType.ANOMALY:
                if (target == CharacterType.PRIMORDIAL)
                    return WeakMultiplier;
                break;
        }

        return NeutralMultiplier;
    }

    // Nullable overload: unknown side = neutral (1.0x).
    public static float GetMultiplier(
        CharacterType? attacker, CharacterType? target)
    {
        if (!attacker.HasValue || !target.HasValue)
            return NeutralMultiplier;

        return GetMultiplier(attacker.Value, target.Value);
    }

    // Matchup-scaled damage from a Character-based source (EX skills and
    // other direct TakeDamage callers). Target hierarchy: Character
    // component (player) or EnemyDataBinder + SO (enemy), on the same
    // object as the Health. Missing side = neutral.
    public static void Damage(Character attacker, Health target, float amount)
    {
        if (target == null)
            return;

        CharacterType? attackerType =
            attacker != null ? attacker.CharacterType : (CharacterType?)null;

        CharacterType? targetType = null;

        // Binder first: the EnemyData asset is the explicit hierarchy
        // source for enemies (covers enemies that also carry a Character).
        EnemyDataBinder binder = target.GetComponent<EnemyDataBinder>();

        if (binder != null && binder.Data != null)
        {
            targetType = binder.Data.characterType;
        }
        else
        {
            Character targetCharacter = target.GetComponent<Character>();

            if (targetCharacter != null)
                targetType = targetCharacter.CharacterType;
        }

        target.TakeDamage(amount * GetMultiplier(attackerType, targetType));
    }
}
