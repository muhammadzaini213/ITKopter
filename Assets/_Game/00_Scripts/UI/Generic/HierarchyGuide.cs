using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Builds the "hierarchy" (CharacterType) matchup guide shown from the
/// character select popup. Multipliers come straight from CounterMechanics
/// so the guide can't drift from the real damage rules.
/// </summary>
public static class HierarchyGuide
{
    public const string Title = "HIERARCHY GUIDE";

    public static string Color(CharacterType type)
    {
        switch (type)
        {
            case CharacterType.ABSTRACT: return "#C77DFF";
            case CharacterType.SIMPLE: return "#5FB8FF";
            case CharacterType.SPECIAL: return "#FFD447";
            case CharacterType.PRIMORDIAL: return "#6CFF8A";
            case CharacterType.ANOMALY: return "#FF5A6E";
            default: return "#FFFFFF";
        }
    }

    /// <summary>TMP sprite asset with one icon per CharacterType (Resources/Sprite Assets).</summary>
    public const string IconAsset = "HierarchyIcons";

    /// <summary>The type's icon (same art as the character detail panel) as a TMP sprite tag.</summary>
    public static string Icon(CharacterType type) =>
        $"<size=125%><sprite=\"{IconAsset}\" name=\"{type.ToString().ToUpperInvariant()}\"></size>";

    /// <summary>Icon + coloured type name.</summary>
    public static string Tag(CharacterType type) =>
        $"{Icon(type)}<color={Color(type)}>{type.ToString().ToUpperInvariant()}</color>";

    private static string Percent(float multiplier) => $"{Mathf.RoundToInt(multiplier * 100f)}%";

    public static string BuildBody()
    {
        string strong = Percent(CounterMechanics.StrongMultiplier);
        string weak = Percent(CounterMechanics.WeakMultiplier);
        const string Arrow = "  <color=#19F0FF>></color>  ";

        var sb = new StringBuilder();

        sb.Append($"Hit a type you beat: <color=#6CFF8A>{strong} damage</color>.  ")
          .Append($"Hit a type that beats you: <color=#FF5A6E>{weak}</color>.\n\n");

        sb.Append("<color=#19F0FF>TRINITY CYCLE</color>\n")
          .Append(Tag(CharacterType.ABSTRACT)).Append(Arrow)
          .Append(Tag(CharacterType.SIMPLE)).Append(Arrow)
          .Append(Tag(CharacterType.SPECIAL)).Append(Arrow)
          .Append(Tag(CharacterType.ABSTRACT)).Append("\n")
          .Append($"<color=#8FA9B5>Each beats the next: {strong} forward, {weak} backward.</color>\n\n");

        sb.Append("<color=#19F0FF>DUALITY</color>\n")
          .Append(Tag(CharacterType.PRIMORDIAL)).Append("  <color=#FF5A6E><></color>  ").Append(Tag(CharacterType.ANOMALY)).Append("\n")
          .Append($"<color=#8FA9B5>Mutual weakness: {weak} both ways. Neutral vs the trinity.</color>\n\n");

        sb.Append("<color=#19F0FF>OTHERWISE</color>  100% (same type, any other pair)\n\n");

        AppendRoster(sb);
        return sb.ToString();
    }

    private static void AppendRoster(StringBuilder sb)
    {
        PlayerSystem player = PlayerSystem.Instance;
        if (player == null || player.CharacterOwned == null || player.CharacterOwned.Count == 0)
            return;

        var byType = new SortedDictionary<CharacterType, List<string>>();

        foreach (CharacterData character in player.CharacterOwned)
        {
            if (character == null)
                continue;

            if (!byType.TryGetValue(character.CharacterType, out var names))
                byType[character.CharacterType] = names = new List<string>();

            names.Add(character.CharacterName);
        }

        sb.Append("<color=#19F0FF>YOUR UNITS</color>\n");

        foreach (var pair in byType)
            sb.Append($"{Tag(pair.Key)}  {string.Join(", ", pair.Value)}\n");
    }
}
