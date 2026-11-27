using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the playable column (the inner edges of the "=== Border ===" walls)
/// lined up with the visible gap between the HUD's LeftPanel and RightPanel
/// at any screen aspect (WebGL window, itch.io iframe, fullscreen).
///
/// The HUD scales with screen width, so the gap is a fixed fraction of the
/// screen; the perspective camera's visible width, however, changes with
/// aspect. This recentres the virtual camera on the play area and sets its
/// field of view so the play area's width fills the gap exactly, while its
/// full height stays on screen (on very wide screens the height wins and the
/// play area sits centred in the gap).
///
/// Added at runtime by GameplayManager; needs no scene setup.
/// </summary>
[DisallowMultipleComponent]
public class PlayAreaCameraFit : MonoBehaviour
{
    private const string BorderPath = "====== GAME ======/=== Border ===";
    private const string HudName = "====== HUD ======";

    [SerializeField, Tooltip("Extra world units shown around the play area (0 = edge to edge).")]
    private float padding = 0f;

    [SerializeField, Tooltip("Cover everything outside the play area with the HUD's panel colour, so extra " +
                             "space on unusual aspect ratios looks like part of the frame.")]
    private bool maskOutsidePlayArea = true;

    [SerializeField] private Color maskColor = new Color32(9, 24, 36, 255);

    [SerializeField, Tooltip("Keep HUD panels readable on very wide/narrow screens (CanvasScaler Expand).")]
    private bool expandHud = true;

    private Transform maskRoot;

    private CinemachineVirtualCamera vcam;
    private Camera outputCamera;
    private RectTransform leftPanel;
    private RectTransform rightPanel;
    private Canvas hudCanvas;

    private Rect playArea;          // world units, z = 0 plane
    private bool hasPlayArea;
    private int lastWidth = -1;
    private int lastHeight = -1;
    private int settleFrames;

    /// <summary>Play area in world units (inner edges of the border walls).</summary>
    public Rect PlayArea => playArea;

    public static PlayAreaCameraFit Ensure()
    {
        var existing = FindFirstObjectByType<PlayAreaCameraFit>();
        if (existing != null)
            return existing;

        var vcam = FindFirstObjectByType<CinemachineVirtualCamera>();
        if (vcam == null)
            return null;

        return vcam.gameObject.AddComponent<PlayAreaCameraFit>();
    }

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        outputCamera = Camera.main;
        FindPlayArea();
        FindHudPanels();

        if (expandHud && hudCanvas != null)
        {
            // Width-only matching squeezes the panels vertically on wide
            // screens (labels overlapping). Expand = identical at 16:9,
            // never smaller than the 1280x720 reference in either direction.
            var scaler = hudCanvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }

        if (maskOutsidePlayArea && hasPlayArea)
            BuildMask();

        // The HUD lays itself out during the first frames; refit a few times.
        settleFrames = 5;
    }

    private void FindPlayArea()
    {
        GameObject border = GameObject.Find(BorderPath);
        if (border == null)
        {
            Debug.LogWarning("[PlayAreaCameraFit] '=== Border ===' not found; camera fit disabled.", this);
            return;
        }

        Collider2D left = Wall(border, "Left");
        Collider2D right = Wall(border, "Right");
        Collider2D top = Wall(border, "Top");
        Collider2D bottom = Wall(border, "Bottom");

        if (left == null || right == null || top == null || bottom == null)
        {
            Debug.LogWarning("[PlayAreaCameraFit] Border walls Left/Right/Top/Bottom need 2D colliders.", this);
            return;
        }

        float xMin = left.bounds.max.x;
        float xMax = right.bounds.min.x;
        float yMin = bottom.bounds.max.y;
        float yMax = top.bounds.min.y;

        playArea = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        hasPlayArea = playArea.width > 0.01f && playArea.height > 0.01f;
    }

    // Four large panels around the play rect, drawn above gameplay.
    private void BuildMask()
    {
        var texture = Texture2D.whiteTexture;
        Sprite square = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), texture.width);

        maskRoot = new GameObject("PlayAreaMask").transform;

        const float far = 200f;
        Rect r = playArea;

        MaskPanel(square, "Left", Rect.MinMaxRect(r.xMin - far, r.yMin - far, r.xMin, r.yMax + far));
        MaskPanel(square, "Right", Rect.MinMaxRect(r.xMax, r.yMin - far, r.xMax + far, r.yMax + far));
        MaskPanel(square, "Top", Rect.MinMaxRect(r.xMin, r.yMax, r.xMax, r.yMax + far));
        MaskPanel(square, "Bottom", Rect.MinMaxRect(r.xMin, r.yMin - far, r.xMax, r.yMin));
    }

    private void MaskPanel(Sprite square, string name, Rect area)
    {
        var go = new GameObject(name);
        go.transform.SetParent(maskRoot, false);
        go.transform.position = new Vector3(area.center.x, area.center.y, 0f);
        go.transform.localScale = new Vector3(area.width, area.height, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = maskColor;
        sr.sortingLayerID = SkillVFX.SortingLayerId;
        sr.sortingOrder = 1000;
    }

    private void OnDestroy()
    {
        if (maskRoot != null)
            Destroy(maskRoot.gameObject);
    }

    private static Collider2D Wall(GameObject border, string name)
    {
        Transform t = border.transform.Find(name);
        return t != null ? t.GetComponent<Collider2D>() : null;
    }

    private void FindHudPanels()
    {
        GameObject hud = GameObject.Find(HudName);
        if (hud == null)
            return;

        hudCanvas = hud.GetComponent<Canvas>();

        Transform left = hud.transform.Find("LeftPanel");
        Transform right = hud.transform.Find("RightPanel");
        leftPanel = left as RectTransform;
        rightPanel = right as RectTransform;
    }

    private void LateUpdate()
    {
        if (!hasPlayArea || vcam == null)
            return;

        bool resized = Screen.width != lastWidth || Screen.height != lastHeight;

        if (!resized && settleFrames <= 0)
            return;

        if (resized)
            settleFrames = Mathf.Max(settleFrames, 3);

        lastWidth = Screen.width;
        lastHeight = Screen.height;
        settleFrames--;

        Fit();
    }

    /// <summary>Viewport x-range (0..1) left visible between the HUD side panels.</summary>
    public Vector2 VisibleGap()
    {
        float minX = 0f;
        float maxX = 1f;

        if (Screen.width <= 0)
            return new Vector2(minX, maxX);

        Camera uiCamera = hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? hudCanvas.worldCamera
            : null;

        var corners = new Vector3[4];

        if (leftPanel != null && leftPanel.gameObject.activeInHierarchy)
        {
            leftPanel.GetWorldCorners(corners);
            minX = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]).x / Screen.width;
        }

        if (rightPanel != null && rightPanel.gameObject.activeInHierarchy)
        {
            rightPanel.GetWorldCorners(corners);
            maxX = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]).x / Screen.width;
        }

        minX = Mathf.Clamp01(minX);
        maxX = Mathf.Clamp01(maxX);

        if (maxX - minX < 0.05f)
            return new Vector2(0f, 1f);

        return new Vector2(minX, maxX);
    }

    private void Fit()
    {
        float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
        Vector2 gap = VisibleGap();
        float gapWidth = gap.y - gap.x;
        float gapCenter = (gap.x + gap.y) * 0.5f;

        float width = playArea.width + padding * 2f;
        float height = playArea.height + padding * 2f;

        // Visible height needed so `width` spans exactly the gap...
        float heightForWidth = width / (gapWidth * aspect);
        // ...but never less than the play area's own height.
        float visibleHeight = Mathf.Max(heightForWidth, height);
        float visibleWidth = visibleHeight * aspect;

        // Shift so the play area's centre lands on the gap's centre.
        float offsetX = (gapCenter - 0.5f) * visibleWidth;

        Vector3 position = transform.position;
        position.x = playArea.center.x - offsetX;
        position.y = playArea.center.y;
        transform.position = position;

        LensSettings lens = vcam.m_Lens;
        bool orthographic = outputCamera != null && outputCamera.orthographic;

        if (orthographic)
        {
            lens.OrthographicSize = visibleHeight * 0.5f;
        }
        else
        {
            float distance = Mathf.Max(0.01f, Mathf.Abs(position.z));
            lens.FieldOfView = 2f * Mathf.Atan(visibleHeight * 0.5f / distance) * Mathf.Rad2Deg;
        }

        vcam.m_Lens = lens;
    }
}
