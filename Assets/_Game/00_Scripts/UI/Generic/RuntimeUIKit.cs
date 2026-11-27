using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small builders for popups made at runtime (no scene edits): the project's
/// 9-slice frame, Abaddon text and frame buttons. Used by the Annihilator
/// hierarchy choice and the stage sweep popup.
/// </summary>
public static class RuntimeUIKit
{
    public const string FrameSpriteName = "9Slice_0";
    public const string FontName = "Abaddon Bold";

    public static readonly Color Cyan = new Color(0.098f, 0.941f, 1f);
    public static readonly Color SubtleText = new Color(0.75f, 0.88f, 0.93f);
    public static readonly Color Dim = new Color(0f, 0f, 0f, 0.7f);

    /// <summary>The 9-slice frame sprite, found among loaded sprites.</summary>
    public static Sprite FindFrameSprite()
    {
        foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            if (sprite != null && sprite.name == FrameSpriteName)
                return sprite;

        return null;
    }

    /// <summary>The project font (Abaddon Bold), found among loaded fonts; null = TMP default.</summary>
    public static TMP_FontAsset FindFont()
    {
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (font != null && font.name == FontName)
                return font;

        return null;
    }

    /// <summary>A full-screen overlay canvas (1280x720 reference) on top of everything.</summary>
    public static GameObject NewOverlayCanvas(string name, int sortingOrder)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return root;
    }

    public static Image NewImage(string name, Transform parent, Color color, Sprite sprite, float pixelsPerUnit = 1f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;

        if (sprite != null)
        {
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = pixelsPerUnit;
        }

        return image;
    }

    public static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, string text, float size,
        Color color, Vector2 position, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = box;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.richText = true;
        return tmp;
    }

    /// <summary>A white 9-slice button (cyan frame) with a centred label; tints on hover / press.</summary>
    public static Button NewButton(string name, Transform parent, TMP_FontAsset font, Sprite frame, string label,
        float size, Vector2 position, Vector2 box, float pixelsPerUnit = 1.5f)
    {
        Image background = NewImage(name, parent, Color.white, frame, pixelsPerUnit);
        RectTransform rt = background.rectTransform;
        rt.anchoredPosition = position;
        rt.sizeDelta = box;

        NewText("Label", rt, font, label, size, Color.white, Vector2.zero, box - new Vector2(24f, 16f));

        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.75f, 1f, 1f, 1f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.55f, 0.8f, 0.85f, 1f);
        colors.disabledColor = new Color(0.45f, 0.5f, 0.55f, 0.7f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return button;
    }

    public static void SetLabel(Button button, string label)
    {
        var text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null) text.text = label;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
