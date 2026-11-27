using System.Collections.Generic;

/// <summary>
/// Story chapter unlock rules shared by the story menu and the main menu
/// badge: the previous chapter must be complete and, for stage-gated
/// chapters, the required stage cleared.
/// </summary>
public static class StoryProgress
{
    public static bool IsCompleted(StoryData story, PlayerSystem player) =>
        story != null && player != null && player.IsPlayerDataLoaded &&
        !string.IsNullOrWhiteSpace(story.StoryId) && player.IsLevelCompleted(story.StoryId);

    public static bool IsUnlocked(StoryData story, PlayerSystem player)
    {
        if (story == null)
            return false;

        bool needsProgress = story.PreviousStory != null || story.RequiredLevel != null;

        if (!needsProgress)
            return true;

        if (player == null || !player.IsPlayerDataLoaded)
            return false;

        if (story.PreviousStory != null && !IsCompleted(story.PreviousStory, player))
            return false;

        if (story.RequiredLevel != null && !player.IsLevelCompleted(story.RequiredLevel.LevelId))
            return false;

        return true;
    }

    /// <summary>Unlocked by a stage clear and not played yet (shows NEW / the menu badge).</summary>
    public static bool IsNew(StoryData story, PlayerSystem player) =>
        story != null && story.RequiredLevel != null &&
        IsUnlocked(story, player) && !IsCompleted(story, player);

    /// <summary>Why a locked chapter is locked, e.g. "Clear Stage 6 to unlock".</summary>
    public static string LockReason(StoryData story, PlayerSystem player)
    {
        if (story == null)
            return null;

        bool previousDone = story.PreviousStory == null || IsCompleted(story.PreviousStory, player);
        bool stageDone = story.RequiredLevel == null ||
                         (player != null && player.IsPlayerDataLoaded && player.IsLevelCompleted(story.RequiredLevel.LevelId));

        string stage = story.RequiredLevel != null
            ? story.RequiredLevel.LevelName.Replace(" - BOSS", string.Empty)
            : null;

        if (!previousDone && !stageDone)
            return $"Finish the previous chapter and clear {stage}";
        if (!previousDone)
            return "Finish the previous chapter";
        if (!stageDone)
            return $"Clear {stage} to unlock";

        return null;
    }

    /// <summary>All main story chapters: the onboarding list, then the nextStory chain after it.</summary>
    public static List<StoryData> MainStories()
    {
        var result = new List<StoryData>();
        StorySystem system = StorySystem.Instance;

        if (system == null || system.OnboardingChapters == null)
            return result;

        foreach (StoryData chapter in system.OnboardingChapters)
        {
            if (chapter != null && !result.Contains(chapter))
                result.Add(chapter);
        }

        StoryData next = result.Count > 0 ? result[result.Count - 1].NextStory : null;

        while (next != null && !result.Contains(next))
        {
            result.Add(next);
            next = next.NextStory;
        }

        return result;
    }

    /// <summary>Stage-gated chapters that are unlocked and not played yet.</summary>
    public static int NewCount(PlayerSystem player)
    {
        int count = 0;

        foreach (StoryData story in MainStories())
        {
            if (IsNew(story, player))
                count++;
        }

        return count;
    }
}
