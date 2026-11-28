using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Chapter complete" popup shown after a chapter replayed from the story
/// menu, when the next chapter is unlocked: NEXT CHAPTER or STORY MENU.
/// Built at runtime with the project's 9-slice frame.
/// </summary>
public class StoryEndPopupUI : MonoBehaviour
{
    private Action<bool> onChoice;
    private bool done;

    /// <param name="onChoice">true = play the next chapter, false = back to the story menu.</param>
    public static StoryEndPopupUI Show(StoryData finished, StoryData next, Action<bool> onChoice)
    {
        GameObject root = RuntimeUIKit.NewOverlayCanvas("StoryEndPopup", 330);
        var ui = root.AddComponent<StoryEndPopupUI>();
        ui.onChoice = onChoice;
        ui.Build(finished, next);
        return ui;
    }

    private void Build(StoryData finished, StoryData next)
    {
        var font = RuntimeUIKit.FindFont();
        Sprite frame = RuntimeUIKit.FindFrameSprite();

        var dim = RuntimeUIKit.NewImage("Dim", transform, RuntimeUIKit.Dim, null);
        RuntimeUIKit.Stretch(dim.rectTransform);

        RectTransform panel = RuntimeUIKit.NewImage("Panel", transform, Color.white, frame).rectTransform;
        panel.sizeDelta = new Vector2(680f, 360f);

        RuntimeUIKit.NewText("Title", panel, font, "CHAPTER COMPLETE", 34f, RuntimeUIKit.Cyan,
            new Vector2(0f, 128f), new Vector2(620f, 48f));
        RuntimeUIKit.NewText("Finished", panel, font, Describe(finished), 22f, Color.white,
            new Vector2(0f, 74f), new Vector2(620f, 36f));
        RuntimeUIKit.NewText("Next", panel, font,
            $"<color=#8FA9B5>Up next</color>\n{Describe(next)}", 22f, RuntimeUIKit.SubtleText,
            new Vector2(0f, -4f), new Vector2(620f, 70f));

        Button nextButton = RuntimeUIKit.NewButton("NextChapter", panel, font, frame, "NEXT CHAPTER", 22f,
            new Vector2(-150f, -118f), new Vector2(250f, 62f), 2f);
        Button menuButton = RuntimeUIKit.NewButton("StoryMenu", panel, font, frame, "STORY MENU", 22f,
            new Vector2(150f, -118f), new Vector2(250f, 62f), 2f);

        nextButton.onClick.AddListener(() => Choose(true));
        menuButton.onClick.AddListener(() => Choose(false));

        UIAttentionPulse.Attach(nextButton.gameObject);
    }

    private static string Describe(StoryData story) =>
        string.IsNullOrWhiteSpace(story.ChapterLabel)
            ? story.StoryName
            : $"<size=75%><color=#19F0FF>{story.ChapterLabel}</color></size>  {story.StoryName}";

    private void Choose(bool next)
    {
        if (done)
            return;

        done = true;
        Destroy(gameObject);
        onChoice?.Invoke(next);
    }
}
