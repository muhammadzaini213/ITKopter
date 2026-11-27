using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// "Enemies" info for a level (character select popup): every enemy that
/// appears in the level's waves with its hierarchy, plus which hierarchy to
/// bring (and avoid). Read straight from LevelData → parts → waves → wave
/// prefabs → EnemyDataBinder, and CounterMechanics for the matchups.
/// </summary>
public static class LevelEnemyIntel
{
    private class Entry
    {
        public string Name;
        public CharacterType Type;
        public int Count;
        public bool IsBoss;
        public float Health;
    }

    private static readonly CharacterType[] AllTypes =
        (CharacterType[])System.Enum.GetValues(typeof(CharacterType));

    public static string Title(LevelData level) =>
        level != null ? $"{level.LevelName.ToUpperInvariant()} ENEMIES" : "ENEMIES";

    public static string BuildBody(LevelData level)
    {
        if (level == null)
            return "No level selected.";

        List<Entry> entries = Collect(level);

        if (entries.Count == 0)
            return "No enemy data for this level.";

        var sb = new StringBuilder();

        // ---- roster ----------------------------------------------------
        sb.Append("<color=#19F0FF>ENEMIES</color>\n");

        foreach (Entry e in entries)
        {
            string boss = e.IsBoss ? " <color=#FF5A6E>[BOSS]</color>" : "";
            string count = e.IsBoss ? "" : $" x{e.Count}";
            sb.Append($"{HierarchyGuide.Tag(e.Type)}  {e.Name}{count}{boss}  ")
              .Append($"<color=#8FA9B5>HP {Mathf.RoundToInt(e.Health)}</color>\n");
        }

        // ---- per-type counters -------------------------------------------
        var types = new List<CharacterType>();
        foreach (Entry e in entries)
            if (!types.Contains(e.Type)) types.Add(e.Type);

        sb.Append("\n<color=#19F0FF>HOW TO COUNTER</color>\n");

        foreach (CharacterType enemyType in types)
        {
            var strong = new List<string>();
            var weak = new List<string>();

            foreach (CharacterType mine in AllTypes)
            {
                float m = CounterMechanics.GetMultiplier(mine, enemyType);
                if (m > CounterMechanics.NeutralMultiplier) strong.Add(HierarchyGuide.Tag(mine));
                else if (m < CounterMechanics.NeutralMultiplier) weak.Add(HierarchyGuide.Tag(mine));
            }

            sb.Append($"vs {HierarchyGuide.Tag(enemyType)}: ");

            if (strong.Count > 0)
                sb.Append($"use {string.Join(" / ", strong)} <color=#6CFF8A>(200%)</color>");
            else
                sb.Append("no type deals bonus damage; any neutral type is fine");

            if (weak.Count > 0)
                sb.Append($", avoid {string.Join(" / ", weak)} <color=#FF5A6E>(40%)</color>");

            sb.Append("\n");
        }

        // ---- overall suggestion ------------------------------------------
        CharacterType best = BestType(entries, out float bestScore);
        sb.Append("\n<color=#19F0FF>SUGGESTED</color>\n");

        if (bestScore > 0f)
        {
            sb.Append($"Bring {HierarchyGuide.Tag(best)} units");
            string owned = OwnedOfType(best);
            if (!string.IsNullOrEmpty(owned))
                sb.Append($": <color=#FFFFFF>{owned}</color>");
            sb.Append(".\n");
        }
        else
        {
            sb.Append("No clear counter here: bring your strongest neutral team.\n");
        }

        // Explicit boss call-out.
        foreach (Entry e in entries)
        {
            if (!e.IsBoss)
                continue;

            var counters = new List<string>();
            foreach (CharacterType mine in AllTypes)
                if (CounterMechanics.GetMultiplier(mine, e.Type) > CounterMechanics.NeutralMultiplier)
                    counters.Add(HierarchyGuide.Tag(mine));

            sb.Append($"Boss {e.Name} ({HierarchyGuide.Tag(e.Type)}): ");
            sb.Append(counters.Count > 0
                ? $"switch to {string.Join(" / ", counters)} for the fight.\n"
                : "no type has the edge; bring your strongest unit.\n");
        }

        sb.Append("<color=#8FA9B5>Suggestion is weighted by enemy HP, so bosses matter most.</color>");
        return sb.ToString();
    }

    private static List<Entry> Collect(LevelData level)
    {
        var map = new Dictionary<string, Entry>();
        var order = new List<Entry>();
        float mult = level.EnemyStatMultiplier;

        if (level.LevelParts == null)
            return order;

        foreach (LevelPartData part in level.LevelParts)
        {
            if (part == null || part.EnemyWaves == null)
                continue;

            foreach (WaveData wave in part.EnemyWaves)
            {
                if (wave == null || wave.EnemyCollectionPrefab == null)
                    continue;

                foreach (EnemyDataBinder binder in wave.EnemyCollectionPrefab.GetComponentsInChildren<EnemyDataBinder>(true))
                {
                    if (binder == null || binder.Data == null)
                        continue;

                    EnemyBoss boss = binder.GetComponent<EnemyBoss>();
                    string name = boss != null && !string.IsNullOrEmpty(boss.DisplayName)
                        ? boss.DisplayName
                        : CleanName(binder.gameObject.name);

                    string key = name + "|" + binder.Data.characterType;

                    if (!map.TryGetValue(key, out Entry e))
                    {
                        e = new Entry
                        {
                            Name = name,
                            Type = binder.Data.characterType,
                            IsBoss = boss != null,
                            Health = binder.Data.maxHealth * mult
                        };
                        map[key] = e;
                        order.Add(e);
                    }

                    e.Count++;
                }
            }
        }

        // Bosses last, like the stage itself.
        order.Sort((a, b) => a.IsBoss.CompareTo(b.IsBoss));
        return order;
    }

    // Highest total multiplier advantage across the level, weighted by
    // each enemy's total HP (a 9000 HP boss outweighs a few minions).
    private static CharacterType BestType(List<Entry> entries, out float bestScore)
    {
        CharacterType best = CharacterType.ABSTRACT;
        bestScore = float.MinValue;

        foreach (CharacterType mine in AllTypes)
        {
            float score = 0f;

            foreach (Entry e in entries)
            {
                float weight = e.Health * e.Count;
                score += (CounterMechanics.GetMultiplier(mine, e.Type) - 1f) * weight;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = mine;
            }
        }

        return best;
    }

    private static string OwnedOfType(CharacterType type)
    {
        PlayerSystem player = PlayerSystem.Instance;
        if (player == null || player.CharacterOwned == null)
            return null;

        var names = new List<string>();
        foreach (CharacterData c in player.CharacterOwned)
            if (c != null && c.CharacterType == type)
                names.Add(c.CharacterName);

        return names.Count > 0 ? string.Join(", ", names) : null;
    }

    private static string CleanName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Enemy";

        string name = raw.Replace("(Clone)", "").Trim();
        if (name.EndsWith("Enemy") && name.Length > 5)
            name = name.Substring(0, name.Length - 5);
        if (name.StartsWith("S_"))
            name = name.Substring(2);

        return name.Trim();
    }
}
