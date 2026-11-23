using System;
using UnityEngine;

public enum StoryLineStyle
{
    /// <summary>Speaker name + text in the dialogue box, with the speaker's portrait.</summary>
    Dialogue,
    /// <summary>Centered narration over the background, no speaker.</summary>
    Narration,
    /// <summary>Big rule card: Label ("RULE 01"), Heading and body Text.</summary>
    Rule,
    /// <summary>Chapter title card: Label ("CHAPTER 3") + Heading.</summary>
    Title
}

public enum StoryLineEffect
{
    None,
    /// <summary>White screen flash when the line appears.</summary>
    Flash,
    /// <summary>Screen shake when the line appears.</summary>
    Shake
}

public enum StorySegmentType
{
    Dialogue,
    Battle
}

[Serializable]
public class StoryLine
{
    public StoryLineStyle style = StoryLineStyle.Dialogue;

    [Tooltip("Speaking character (portrait + name). Leave empty for non-character speakers.")]
    public CharacterData speaker;

    [Tooltip("Name shown when there is no speaker character (e.g. Fajar, Council), or to override it.")]
    public string speakerName;

    [Tooltip("Show the speaker's portrait on the right instead of the left.")]
    public bool rightSide;

    [Tooltip("Rule / Title: small label above the heading (e.g. RULE 01, CHAPTER 3).")]
    public string label;

    [Tooltip("Rule / Title: big heading.")]
    public string heading;

    [TextArea(2, 6)] public string text;

    [Tooltip("Black background for this line (prologue voice, fades).")]
    public bool blackScreen;

    public StoryLineEffect effect = StoryLineEffect.None;

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(speakerName) ? speakerName :
        speaker != null ? speaker.CharacterName : string.Empty;
}

/// <summary>A speaker line shown as a small toast during battles or after a story redirects to a menu.</summary>
[Serializable]
public class StoryToastLine
{
    [Tooltip("Battle: seconds after the battle starts. After-scene toasts: delay after the previous one.")]
    [Min(0f)] public float time;
    public CharacterData speaker;
    public string speakerName;
    [TextArea(1, 3)] public string text;
    [Min(0.5f)] public float duration = 4.5f;

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(speakerName) ? speakerName :
        speaker != null ? speaker.CharacterName : string.Empty;
}

[Serializable]
public class StorySegment
{
    public StorySegmentType type = StorySegmentType.Dialogue;

    [Header("Dialogue")]
    public StoryLine[] lines = new StoryLine[0];

    [Header("Battle")]
    public LevelData level;

    [Tooltip("Fixed party for this battle (1-3). Ownership is not required.")]
    public CharacterData[] team = new CharacterData[0];

    [Tooltip("Scripted loss: the story continues when the party falls or the timer runs out.")]
    public bool scriptedLoss;

    [Min(5f), Tooltip("Scripted loss: seconds survived before the battle ends as a loss.")]
    public float scriptedLossSeconds = 45f;

    [Tooltip("Tutorial / banter toasts shown during the battle.")]
    public StoryToastLine[] battleToasts = new StoryToastLine[0];
}

[Serializable]
public class StoryRewardEntry
{
    public RewardData reward;
    [Min(1)] public int amount = 1;
}
