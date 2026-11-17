public static class StorySession
{
    public static StoryData Selected { get; set; }

    public static void Clear()
    {
        Selected = null;
    }
}
