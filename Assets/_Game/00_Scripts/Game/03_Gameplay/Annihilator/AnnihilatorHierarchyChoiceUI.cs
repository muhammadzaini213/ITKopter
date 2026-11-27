using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pause overlay that lets the player pick the hierarchy (ABSTRACT, SIMPLE,
/// SPECIAL) of the Annihilator's attack. Built at runtime so no scene needs
/// editing; it borrows the font and frame sprite from the Annihilator button.
/// Keys 1/2/3 pick too. The choice that counters the enemies on screen is
/// marked "COUNTERS FIELD".
/// </summary>
public class AnnihilatorHierarchyChoiceUI : MonoBehaviour
{
    public static readonly CharacterType[] Choices =
    {
        CharacterType.ABSTRACT,
        CharacterType.SIMPLE,
        CharacterType.SPECIAL
    };

    public const string FrameSpriteName = "9Slice_0";

    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);
    private static readonly Color Cyan = new Color(0.098f, 0.941f, 1f);
    private static readonly Color SubtleText = new Color(0.75f, 0.88f, 0.93f);

    private Action<CharacterType> onPick;
    private Action onCancel;
    private bool done;

    public static AnnihilatorHierarchyChoiceUI Show(
        TMP_FontAsset font,
        Sprite frame,
        CharacterType? recommended,
        Action<CharacterType> onPick,
        Action onCancel)
    {
        var root = new GameObject("AnnihilatorHierarchyChoice",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        var ui = root.AddComponent<AnnihilatorHierarchyChoiceUI>();
        ui.onPick = onPick;
        ui.onCancel = onCancel;
        ui.Build(font, frame, recommended);
        return ui;
    }

    private void Build(TMP_FontAsset font, Sprite frame, CharacterType? recommended)
    {
        frame = frame != null ? frame : FindFrameSprite();

        var dim = NewImage("Dim", transform, Dim, null, 1f);
        Stretch(dim.rectTransform);

        // Panel: the project's 9-slice frame (dark fill + cyan pixel border), untinted.
        RectTransform panel = NewImage("Panel", transform, Color.white, frame, 1f).rectTransform;
        panel.sizeDelta = new Vector2(820f, 420f);

        NewText("Title", panel, font, "MANIFEST YOUR AUTHORITY", 36f, Cyan, new Vector2(0f, 158f), new Vector2(760f, 50f));
        NewText("Subtitle", panel, font,
            "Choose the hierarchy of the Annihilator's attack.",
            19f, SubtleText, new Vector2(0f, 118f), new Vector2(760f, 28f));

        for (int i = 0; i < Choices.Length; i++)
        {
            CharacterType type = Choices[i];
            CharacterType beats = Choices[(i + 1) % Choices.Length];
            bool best = recommended.HasValue && recommended.Value == type;

            string label =
                $"<size=60%><color=#8FA9B5>[{i + 1}]</color></size>\n" +
                $"<size=130%>{HierarchyGuide.Tag(type)}</size>\n" +
                $"<size=72%><color=#6CFF8A>x{CounterMechanics.StrongMultiplier:0.#}</color> vs {HierarchyGuide.Tag(beats)}</size>" +
                (best ? "\n<size=68%><color=#FFD447>COUNTERS FIELD</color></size>" : "");

            Button button = NewButton($"Choice_{type}", panel, font, frame, label, 22f,
                new Vector2(-250f + 250f * i, -14f), new Vector2(226f, 190f), 1.5f);

            button.onClick.AddListener(() => Pick(type));

            if (best)
                UIAttentionPulse.Attach(button.gameObject);
        }

        Button cancel = NewButton("Cancel", panel, font, frame, "CANCEL", 20f,
            new Vector2(0f, -164f), new Vector2(190f, 50f), 2f);
        cancel.onClick.AddListener(Cancel);
    }

    /// <summary>The project's 9-slice frame sprite ("9Slice_0"), found among loaded sprites.</summary>
    public static Sprite FindFrameSprite() => RuntimeUIKit.FindFrameSprite();

    private void Update()
    {
        if (done || Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) Pick(Choices[0]);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) Pick(Choices[1]);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) Pick(Choices[2]);
    }

    public void Pick(CharacterType type)
    {
        if (done)
            return;

        done = true;
        Destroy(gameObject);
        onPick?.Invoke(type);
    }

    public void Cancel()
    {
        if (done)
            return;

        done = true;
        Destroy(gameObject);
        onCancel?.Invoke();
    }

    /// <summary>The trinity type that counters the most enemies on screen (null if none does).</summary>
    public static CharacterType? RecommendedForField()
    {
        CharacterType? best = null;
        int bestCount = 0;

        foreach (CharacterType type in Choices)
        {
            int count = 0;

            foreach (var health in SkillTargets.AliveEnemies())
            {
                var binder = health.GetComponent<EnemyDataBinder>();

                if (binder != null && binder.Data != null &&
                    CounterMechanics.GetMultiplier(type, binder.Data.characterType) > CounterMechanics.NeutralMultiplier)
                    count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                best = type;
            }
        }

        return best;
    }

    // ---- builders: the shared runtime kit ---------------------------------

    private static Image NewImage(string name, Transform parent, Color color, Sprite sprite, float pixelsPerUnit) =>
        RuntimeUIKit.NewImage(name, parent, color, sprite, pixelsPerUnit);

    private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, string text, float size,
        Color color, Vector2 position, Vector2 box) =>
        RuntimeUIKit.NewText(name, parent, font, text, size, color, position, box);

    private static Button NewButton(string name, Transform parent, TMP_FontAsset font, Sprite frame, string label,
        float size, Vector2 position, Vector2 box, float pixelsPerUnit) =>
        RuntimeUIKit.NewButton(name, parent, font, frame, label, size, position, box, pixelsPerUnit);

    private static void Stretch(RectTransform rt) => RuntimeUIKit.Stretch(rt);
}
