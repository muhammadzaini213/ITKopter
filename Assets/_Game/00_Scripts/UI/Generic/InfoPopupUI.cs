using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Slafurry.Systems.Audio;

/// <summary>
/// Generic read-only info popup (title + rich-text body + Close), built at
/// runtime in the game's UI style: Abaddon font, 9Slice frame, cyan.
/// Used by the hierarchy (type) guide and the EX skill info.
/// </summary>
public class InfoPopupUI : MonoBehaviour
{
    public static readonly Color Cyan = new Color(0.098f, 0.941f, 1f);
    public static readonly Color FrameTint = new Color(0.55f, 0.95f, 1f);
    private static readonly Color PanelColor = new Color(0.03f, 0.08f, 0.12f, 0.97f);

    private static InfoPopupUI instance;

    private RectTransform panel;
    private float openTime;

    public static bool IsOpen => instance != null;

    /// <summary>Opens (or replaces) the info popup.</summary>
    public static void Show(string title, string body, Vector2 size = default, SFXReference clickSfx = null)
    {
        if (clickSfx != null)
            Audio.PlaySFX2D(clickSfx);

        Hide();

        var go = new GameObject("InfoPopup");
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && go.scene != active)
            SceneManager.MoveGameObjectToScene(go, active);

        instance = go.AddComponent<InfoPopupUI>();
        instance.Build(title, body, size == default ? new Vector2(1100f, 760f) : size);
    }

    public static void Hide()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    private void Build(string title, string body, Vector2 size)
    {
        TMP_FontAsset font = SkillVFX.Font;
        Sprite frame = FindSprite("9Slice");

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 420;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        var backdrop = Image("Backdrop", transform, new Color(0f, 0f, 0f, 0.65f), null);
        Stretch(backdrop.rectTransform, 0f);
        var backdropButton = backdrop.gameObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);

        var panelImage = Image("Panel", transform, PanelColor, null);
        panel = panelImage.rectTransform;
        panel.sizeDelta = size;

        var frameImage = Image("Frame", panel, FrameTint, frame);
        Stretch(frameImage.rectTransform, -6f);
        frameImage.raycastTarget = false;
        if (frame != null) frameImage.type = UnityEngine.UI.Image.Type.Sliced;

        var titleText = Text("Title", panel, font, title, 52f, Cyan, TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.sizeDelta = new Vector2(-60f, 80f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, -24f);

        var bodyText = Text("Body", panel, font, body, 32f, Color.white, TextAlignmentOptions.TopLeft);
        bodyText.richText = true;
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 22f;
        bodyText.fontSizeMax = 36f;
        bodyText.lineSpacing = 4f;
        var bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(56f, 120f);
        bodyRect.offsetMax = new Vector2(-56f, -118f);

        // Close button.
        var closeImage = Image("Close", panel, FrameTint, frame);
        if (frame != null) closeImage.type = UnityEngine.UI.Image.Type.Sliced;
        var closeRect = closeImage.rectTransform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.sizeDelta = new Vector2(240f, 76f);
        closeRect.anchoredPosition = new Vector2(0f, 28f);

        var close = closeImage.gameObject.AddComponent<Button>();
        close.onClick.AddListener(Hide);

        var label = Text("Label", closeImage.transform, font, "CLOSE", 34f, Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 0f);

        openTime = Time.unscaledTime;
        panel.localScale = Vector3.one * 0.6f;
    }

    private void Update()
    {
        if (panel == null)
            return;

        float t = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.18f);
        float scale = t < 0.7f ? Mathf.Lerp(0.6f, 1.05f, t / 0.7f) : Mathf.Lerp(1.05f, 1f, (t - 0.7f) / 0.3f);
        panel.localScale = Vector3.one * scale;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // ---- helpers --------------------------------------------------------

    public static Sprite FindSprite(string namePrefix)
    {
        foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image != null && image.sprite != null &&
                image.sprite.name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return image.sprite;
            }
        }

        return null;
    }

    private static Image Image(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, string value,
        float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    /// <summary>
    /// Clones an existing button (keeps the game's look), relabels it and
    /// wires a new click action. Returns the clone.
    /// </summary>
    public static Button CloneButton(Button template, Transform parent, string name, string label, Action onClick)
    {
        if (template == null)
            return null;

        Button clone = Instantiate(template, parent, false);
        clone.name = name;
        clone.onClick = new Button.ButtonClickedEvent();   // drop the template's listeners
        clone.onClick.AddListener(() => onClick?.Invoke());
        clone.interactable = true;
        clone.gameObject.SetActive(true);

        foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true))
        {
            text.text = label;

            // A localized label would overwrite ours.
            foreach (var behaviour in text.GetComponents<MonoBehaviour>())
            {
                if (behaviour != null && behaviour.GetType().Name == "LocalizeText")
                    behaviour.enabled = false;
            }
        }

        return clone;
    }
}
