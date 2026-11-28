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
        CharacterType.SPECIAL,
        CharacterType.PRIMORDIAL,
        CharacterType.ANOMALY
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
        panel.sizeDelta = new Vector2(1000f, 430f);

        NewText("Title", panel, font, "MANIFEST YOUR AUTHORITY", 36f, Cyan, new Vector2(0f, 162f), new Vector2(900f, 50f));
        NewText("Subtitle", panel, font,
            "Choose the hierarchy of the Annihilator's attack.",
            19f, SubtleText, new Vector2(0f, 122f), new Vector2(900f, 28f));

        const float cardWidth = 172f, step = 184f;
        float first = -(Choices.Length - 1) * step * 0.5f;

        for (int i = 0; i < Choices.Length; i++)
        {
            CharacterType type = Choices[i];
            bool best = recommended.HasValue && recommended.Value == type;
            bool weak = !best && IsWeakAgainstField(type);

            string label =
                $"<size=60%><color=#8FA9B5>[{i + 1}]</color></size>\n" +
                $"<size=95%>{HierarchyGuide.Tag(type)}</size>\n" +
                $"<size=68%>{MatchupLine(type)}</size>" +
                (best ? "\n<size=64%><color=#FFD447>COUNTERS FIELD</color></size>"
                      : weak ? "\n<size=64%><color=#FF5A6E>WEAK HERE</color></size>" : "");

            Button button = NewButton($"Choice_{type}", panel, font, frame, label, 20f,
                new Vector2(first + step * i, -14f), new Vector2(cardWidth, 196f), 1.5f);

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

        var digits = new[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
        var numpad = new[] { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5 };

        for (int i = 0; i < Choices.Length && i < digits.Length; i++)
        {
            if (Keyboard.current[digits[i]].wasPressedThisFrame || Keyboard.current[numpad[i]].wasPressedThisFrame)
            {
                Pick(Choices[i]);
                return;
            }
        }
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

    /// <summary>
    /// What a type does against the others, read from CounterMechanics:
    /// "x2 vs SIMPLE" for the trinity, "Neutral vs trinity / x0.4 vs ANOMALY"
    /// for the duality.
    /// </summary>
    public static string MatchupLine(CharacterType type)
    {
        var strong = new System.Collections.Generic.List<string>();
        var weak = new System.Collections.Generic.List<string>();

        foreach (CharacterType other in Choices)
        {
            float m = CounterMechanics.GetMultiplier(type, other);
            if (m > CounterMechanics.NeutralMultiplier) strong.Add(HierarchyGuide.Tag(other));
            else if (m < CounterMechanics.NeutralMultiplier) weak.Add(HierarchyGuide.Tag(other));
        }

        string line = strong.Count > 0
            ? $"<color=#6CFF8A>x{CounterMechanics.StrongMultiplier:0.#}</color> vs {string.Join(" ", strong)}"
            : "<color=#8FA9B5>Neutral vs trinity</color>";

        if (strong.Count == 0 && weak.Count > 0)
            line += $"\n<color=#FF5A6E>x{CounterMechanics.WeakMultiplier:0.#}</color> vs {string.Join(" ", weak)}";

        return line;
    }

    /// <summary>Most enemies on screen resist this type.</summary>
    public static bool IsWeakAgainstField(CharacterType type)
    {
        int total = 0, resisted = 0;

        foreach (var health in SkillTargets.AliveEnemies())
        {
            var binder = health.GetComponent<EnemyDataBinder>();
            if (binder == null || binder.Data == null)
                continue;

            total++;
            if (CounterMechanics.GetMultiplier(type, binder.Data.characterType) < CounterMechanics.NeutralMultiplier)
                resisted++;
        }

        return total > 0 && resisted * 2 > total;
    }

    /// <summary>The type that counters the most enemies on screen (null if none does).</summary>
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
