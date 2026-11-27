using System.Collections.Generic;

/// <summary>Where a tappable character is standing — picks the line set.</summary>
public enum QuipContext
{
    MainMenu,
    Shop,
    Objective,
    Gacha
}

/// <summary>
/// Tap-to-talk lines for menu characters. Main menu lines are per character
/// (the showcase can be anyone); Shop / Objective / Gacha have their own
/// scene-flavoured sets per character, with generic fallbacks.
/// Keys are CharacterData.CharacterId.
/// </summary>
public static class CharacterQuips
{
    private static readonly Dictionary<string, string[]> MainMenu = new Dictionary<string, string[]>
    {
        ["char_1_eipi"] = new[]
        {
            "Hiii! Did you see my new notification sound? It goes BZZT!",
            "Battery at 100%! Let's go do something!",
            "I can connect to anything. Even you!",
            "Don't leave me on read, okay?"
        },
        ["char_2_bokusu"] = new[]
        {
            "...Everything fits better inside a box.",
            "Stay behind me. I'll keep it all contained.",
            "Boxes are honest. What goes in, stays in.",
            "...You can tap me. I don't mind. Much."
        },
        ["char_3_noz"] = new[]
        {
            "I've noted your visit. Timestamped, of course.",
            "A tidy plan is half the battle won.",
            "Please don't smudge the margins.",
            "Every detail matters. Especially the small ones."
        },
        ["char_4_vio"] = new[]
        {
            "Poke me again and see what happens.",
            "I'm bored. Point me at something to break.",
            "Hit first. Think later. Works every time.",
            "...Fine. You can stay. For now."
        },
        ["char_5_trady"] = new[]
        {
            "Every tap has a price, you know. This one's free.",
            "Looking for a deal? I always have one.",
            "Profit first, friendship second. Close second!",
            "Let's make a fair exchange today."
        },
        ["char_6_cashino"] = new[]
        {
            "Feeling lucky? I always am!",
            "Double or nothing on this tap?",
            "The house always wins. Good thing I'm the house.",
            "Come on, lucky seven!"
        },
        ["char_7_regu"] = new[]
        {
            "Rule 12: no tapping the officer without cause.",
            "Order keeps everyone safe. Even you.",
            "I've reviewed the regulations. We're in compliance.",
            "Every rule has a reason. Usually."
        },
        ["char_8_tren"] = new[]
        {
            "Is this trending yet? It should be trending.",
            "New look, who dis?",
            "Like, share, and follow for more!",
            "Last week's meta is so over."
        },
        ["char_9_warlock"] = new[]
        {
            "You dare poke a warlord?",
            "Peace is just the pause between wars.",
            "Bring me a worthy opponent.",
            "Kneel. Or don't. I'll win either way."
        },
        ["char_10_eyay"] = new[]
        {
            "Input received. Processing affection... done.",
            "I learned something new from you today.",
            "Optimizing my response to you. Version 2.0!",
            "Running diagnostics... you seem nice."
        },
        ["char_11_anon"] = new[]
        {
            "...Who? Me? You must be mistaken.",
            "You can't tap what you can't see. ...Oh. You can.",
            "Some things are better left unknown.",
            "..."
        },
        ["char_12_majik"] = new[]
        {
            "Pick a card. Any card. ...Not that one.",
            "Abracadabra! ...Nothing happened? Wait for it.",
            "Impossible is just magic nobody tried yet.",
            "Watch closely. The trick is already done."
        },
    };

    private static readonly Dictionary<string, string[]> Shop = new Dictionary<string, string[]>
    {
        ["char_5_trady"] = new[]
        {
            "Welcome, welcome! Everything's on sale. Almost.",
            "That one? Excellent eye. Excellent price, too.",
            "Coins in, goods out. A beautiful exchange.",
            "Haggling? Bold. I respect that. Still no.",
            "Come back tomorrow, the stock refreshes!"
        },
    };

    private static readonly Dictionary<string, string[]> Objective = new Dictionary<string, string[]>
    {
        ["char_3_noz"] = new[]
        {
            "Your objectives are all written down. Neatly.",
            "Daily tasks reset at midnight. I've set a reminder.",
            "Check off one more and I'll underline it twice.",
            "Unfinished notes keep me up at night...",
            "Rewards are waiting. Don't forget to claim them!"
        },
    };

    private static readonly Dictionary<string, string[]> Gacha = new Dictionary<string, string[]>
    {
        ["char_4_vio"] = new[]
        {
            "Pull already. Waiting is the worst part.",
            "If you don't get a UR, I'm breaking the machine.",
            "One more pull. Then another. Then another.",
            "Rarity doesn't matter. Strength does.",
            "...Fine. Pull ten. I'll watch."
        },
    };

    private static readonly Dictionary<QuipContext, string[]> Fallback = new Dictionary<QuipContext, string[]>
    {
        [QuipContext.MainMenu] = new[] { "Ready when you are!", "Let's head out!", "What's next?" },
        [QuipContext.Shop] = new[] { "Find anything you like?", "Spend wisely!", "Ooh, that looks nice." },
        [QuipContext.Objective] = new[] { "Let's clear some objectives!", "Don't forget your rewards.", "One task at a time." },
        [QuipContext.Gacha] = new[] { "Feeling lucky?", "Who will we meet this time?", "One more pull?" },
    };

    public static string[] Get(string characterId, QuipContext context)
    {
        Dictionary<string, string[]> table = context switch
        {
            QuipContext.Shop => Shop,
            QuipContext.Objective => Objective,
            QuipContext.Gacha => Gacha,
            _ => MainMenu,
        };

        if (!string.IsNullOrEmpty(characterId) && table.TryGetValue(characterId, out var lines) && lines.Length > 0)
            return lines;

        // Scene set missing for this character: their main menu lines, then generic.
        if (!string.IsNullOrEmpty(characterId) && MainMenu.TryGetValue(characterId, out var own) && own.Length > 0)
            return own;

        return Fallback[context];
    }
}
