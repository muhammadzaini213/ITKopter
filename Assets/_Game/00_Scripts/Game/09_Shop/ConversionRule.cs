using System;
using UnityEngine;

public enum ConversionKind
{
    /// <summary>Character engram -> dust of that character's hierarchy.</summary>
    EngramToDust,
    /// <summary>Hierarchy dust -> that character's engram.</summary>
    DustToEngram,
    /// <summary>Character engrams -> unlock the character (only if not owned).</summary>
    EngramToUnlock
}

/// <summary>
/// One row of the conversion table. Applied to every character of <see cref="Rarity"/>;
/// the daily limit counts per character.
/// </summary>
[Serializable]
public class ConversionRule
{
    [SerializeField] private ConversionKind kind;
    [SerializeField] private RewardRarity rarity;
    [SerializeField, Min(1)] private int inputAmount = 1;
    [Tooltip("Ignored for EngramToUnlock (always unlocks one character).")]
    [SerializeField, Min(1)] private int outputAmount = 1;
    [Tooltip("Conversions allowed per character per day. 0 = unlimited.")]
    [SerializeField, Min(0)] private int dailyLimit = 1;

    public ConversionKind Kind => kind;
    public RewardRarity Rarity => rarity;
    public int InputAmount => inputAmount;
    public int OutputAmount => kind == ConversionKind.EngramToUnlock ? 1 : outputAmount;
    public int DailyLimit => dailyLimit;
}

/// <summary>A conversion rule applied to one specific character.</summary>
public class ConversionOffer
{
    public ConversionRule Rule { get; }
    public CharacterData Character { get; }
    public ItemData InputItem { get; }
    public RewardData Output { get; }

    public int InputAmount => Rule.InputAmount;
    public int OutputAmount => Rule.OutputAmount;
    public int DailyLimit => Rule.DailyLimit;

    /// <summary>Stable key for daily-limit tracking.</summary>
    public string Key => $"{Rule.Kind}:{Character.CharacterId}";

    public string OutputName => Rule.Kind switch
    {
        ConversionKind.EngramToUnlock => $"Unlock {Character.CharacterName}",
        _ => $"{OutputAmount}x {ItemName(Output)}"
    };

    public string InputName => $"{InputAmount}x {ItemName(InputItem)}";

    public ConversionOffer(ConversionRule rule, CharacterData character)
    {
        Rule = rule;
        Character = character;

        // Upgrade materials are the verified hierarchy dust / own engram; fall back to the dupe items.
        ItemData engram = character.EngramMaterial != null ? character.EngramMaterial : character.DuplicateEngram;
        ItemData dust = character.DustMaterial != null ? character.DustMaterial : character.DuplicateDust;

        switch (rule.Kind)
        {
            case ConversionKind.EngramToDust:
                InputItem = engram;
                Output = dust;
                break;
            case ConversionKind.DustToEngram:
                InputItem = dust;
                Output = engram;
                break;
            case ConversionKind.EngramToUnlock:
                InputItem = engram;
                Output = character;
                break;
        }
    }

    public bool IsValid => Character != null && InputItem != null && Output != null;

    private static string ItemName(RewardData data) =>
        data is ItemData item ? item.ItemName : data != null ? data.name : "?";
}
