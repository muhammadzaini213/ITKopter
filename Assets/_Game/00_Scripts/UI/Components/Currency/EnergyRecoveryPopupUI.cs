using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Slafurry.Systems.Audio;

/// <summary>
/// Energy recovery popup, opened by tapping the energy bar. Shows current
/// energy, the live countdown to the next point and to full, the recovery
/// rate, and an optional gem refill. Built at runtime in the game's UI style
/// (Abaddon font, 9Slice frame, cyan) so it needs no scene or prefab setup.
/// </summary>
public class EnergyRecoveryPopupUI : MonoBehaviour
{
    // ---- tuning ----------------------------------------------------------
    public static int RefillGemCost = 50;
    public static int RefillEnergyAmount = 60;

    private static readonly Color Cyan = new Color(0.098f, 0.941f, 1f);
    private static readonly Color FrameTint = new Color(0.55f, 0.95f, 1f);
    private static readonly Color PanelColor = new Color(0.03f, 0.08f, 0.12f, 0.96f);

    private static EnergyRecoveryPopupUI instance;

    private TMP_FontAsset font;
    private Sprite frameSprite;
    private Sprite fillSprite;

    private RectTransform panel;
    private TextMeshProUGUI energyText;
    private TextMeshProUGUI nextText;
    private TextMeshProUGUI fullText;
    private TextMeshProUGUI refillLabel;
    private Image fillImage;
    private Slider energySlider;
    private Slider sliderTemplate;
    private Button refillButton;
    private float openTime;
    private float refreshTimer;

    public static bool IsOpen => instance != null;

    /// <summary>Opens the popup (or brings the open one to the front).</summary>
    /// <param name="barTemplate">The game's bar slider (the profile EXP slider) to reuse for the energy bar.</param>
    public static void Show(SFXReference clickSfx = null, Slider barTemplate = null)
    {
        if (EnergySystem.Instance == null)
            return;

        if (clickSfx != null)
            Audio.PlaySFX2D(clickSfx);

        if (instance != null)
            return;

        var go = new GameObject("EnergyRecoveryPopup");
        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid() && go.scene != active)
            SceneManager.MoveGameObjectToScene(go, active);

        instance = go.AddComponent<EnergyRecoveryPopupUI>();
        instance.sliderTemplate = barTemplate != null ? barTemplate : FindSceneBarTemplate();
        instance.Build();
    }

    public static void Hide()
    {
        if (instance != null)
            Destroy(instance.gameObject);
    }

    // =================================================================
    // Build
    // =================================================================

    private void Build()
    {
        FindStyleAssets();

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Backdrop (tap outside to close).
        var backdrop = CreateImage("Backdrop", transform, new Color(0f, 0f, 0f, 0.6f), null);
        Stretch(backdrop.rectTransform);
        var backdropButton = backdrop.gameObject.AddComponent<Button>();
        backdropButton.transition = Selectable.Transition.None;
        backdropButton.onClick.AddListener(Hide);

        // Panel.
        var panelImage = CreateImage("Panel", transform, PanelColor, null);
        panel = panelImage.rectTransform;
        panel.sizeDelta = new Vector2(760f, 500f);

        var frame = CreateImage("Frame", panel, FrameTint, frameSprite);
        Stretch(frame.rectTransform, -6f);
        frame.raycastTarget = false;
        if (frameSprite != null) frame.type = Image.Type.Sliced;

        CreateText("Title", panel, "ENERGY RECOVERY", 50f, Cyan, new Vector2(0f, 180f), new Vector2(660f, 64f));

        energyText = CreateText("Energy", panel, "", 46f, Color.white, new Vector2(0f, 105f), new Vector2(660f, 56f));

        // Bar.
        if (sliderTemplate != null)
        {
            // Same slider asset as the player-profile EXP bar.
            energySlider = Instantiate(sliderTemplate, panel, false);
            energySlider.name = "EnergySlider";
            energySlider.gameObject.SetActive(true);

            var rt = (RectTransform)energySlider.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 30f);
            rt.anchoredPosition = new Vector2(0f, 55f);

            energySlider.interactable = false;
            energySlider.wholeNumbers = true;
            energySlider.minValue = 0f;
            energySlider.onValueChanged.RemoveAllListeners();
        }
        else
        {
            var barBack = CreateImage("BarBack", panel, new Color(1f, 1f, 1f, 0.12f), fillSprite);
            barBack.rectTransform.sizeDelta = new Vector2(560f, 30f);
            barBack.rectTransform.anchoredPosition = new Vector2(0f, 55f);

            fillImage = CreateImage("BarFill", barBack.transform, Cyan, fillSprite);
            Stretch(fillImage.rectTransform);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
        }

        nextText = CreateText("Next", panel, "", 36f, FrameTint, new Vector2(0f, 0f), new Vector2(660f, 44f));
        fullText = CreateText("Full", panel, "", 36f, FrameTint, new Vector2(0f, -42f), new Vector2(660f, 44f));

        int minutes = EnergySystem.Instance.RecoveryIntervalMinutes;
        CreateText("Rate", panel, $"+1 energy every {minutes} min", 30f, new Color(1f, 1f, 1f, 0.6f),
            new Vector2(0f, -84f), new Vector2(660f, 36f));

        refillButton = CreateButton("Refill", panel, new Vector2(-150f, -180f), new Vector2(340f, 80f),
            $"+{RefillEnergyAmount}  ({RefillGemCost} GEMS)", out refillLabel);
        refillButton.onClick.AddListener(Refill);

        var close = CreateButton("Close", panel, new Vector2(190f, -180f), new Vector2(220f, 80f), "CLOSE", out _);
        close.onClick.AddListener(Hide);

        openTime = Time.unscaledTime;
        panel.localScale = Vector3.one * 0.6f;

        Refresh();
    }

    // Fallback when no template was passed: the profile EXP slider in the
    // scene, or the one in the reward popup's EXP bar (persistent, in Boot).
    private static Slider FindSceneBarTemplate()
    {
        var bar = FindFirstObjectByType<ExpGainBarUI>(FindObjectsInactive.Include);
        if (bar != null)
        {
            var slider = bar.GetComponentInChildren<Slider>(true);
            if (slider != null)
                return slider;
        }

        return null;
    }

    private void FindStyleAssets()
    {
        font = SkillVFX.Font;

        foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (image == null || image.sprite == null)
                continue;

            string name = image.sprite.name;

            if (frameSprite == null && name.StartsWith("9Slice", StringComparison.OrdinalIgnoreCase))
                frameSprite = image.sprite;
            else if (fillSprite == null && name.StartsWith("Fill_White", StringComparison.OrdinalIgnoreCase))
                fillSprite = image.sprite;

            if (frameSprite != null && fillSprite != null)
                break;
        }

        // A Filled Image ignores fillAmount without a sprite — make one.
        if (fillSprite == null)
        {
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var pixels = new Color32[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            fillSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }
    }

    private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, Color color,
        Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = dimensions;
        text.rectTransform.anchoredPosition = position;
        return text;
    }

    private Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, string label,
        out TextMeshProUGUI labelText)
    {
        var image = CreateImage(name, parent, FrameTint, frameSprite);
        if (frameSprite != null) image.type = Image.Type.Sliced;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;

        var button = image.gameObject.AddComponent<Button>();
        var colors = button.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        button.colors = colors;

        labelText = CreateText("Label", image.transform, label, 34f, Color.white, Vector2.zero, size);
        return button;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    // =================================================================
    // Live state
    // =================================================================

    private void Update()
    {
        // Pop-in (unscaled — works in paused menus too).
        float t = Mathf.Clamp01((Time.unscaledTime - openTime) / 0.18f);
        float scale = t < 0.7f ? Mathf.Lerp(0.6f, 1.06f, t / 0.7f) : Mathf.Lerp(1.06f, 1f, (t - 0.7f) / 0.3f);
        if (panel != null) panel.localScale = Vector3.one * scale;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.25f;
            Refresh();
        }
    }

    private void Refresh()
    {
        EnergySystem energy = EnergySystem.Instance;

        if (energy == null)
        {
            Hide();
            return;
        }

        int current = energy.Energy;
        int max = energy.MaxEnergy;
        bool full = current >= max;

        energyText.text = $"{current} / {max}";
        if (energySlider != null)
        {
            energySlider.maxValue = Mathf.Max(1, max);
            energySlider.value = current;
        }
        else if (fillImage != null)
        {
            fillImage.fillAmount = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
        }

        if (full)
        {
            nextText.text = "Energy is full!";
            fullText.text = "";
        }
        else
        {
            nextText.text = $"Next +1 in  {Format(energy.TimeUntilNextEnergy)}";
            fullText.text = $"Full in  {Format(energy.TimeUntilFull)}";
        }

        int gems = CurrencySystem.Instance != null ? CurrencySystem.Instance.Gems : 0;
        bool canRefill = !full && gems >= RefillGemCost;
        refillButton.interactable = canRefill;
        refillLabel.color = canRefill ? Color.white : new Color(1f, 1f, 1f, 0.45f);
    }

    private void Refill()
    {
        EnergySystem energy = EnergySystem.Instance;
        CurrencySystem currency = CurrencySystem.Instance;

        if (energy == null || currency == null || energy.EnergyMissing <= 0)
            return;

        if (!currency.SpendGems(RefillGemCost))
            return;

        energy.AddEnergy(RefillEnergyAmount);
        Refresh();
    }

    private static string Format(TimeSpan span)
    {
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:00}m";

        return $"{span.Minutes:00}:{span.Seconds:00}";
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
