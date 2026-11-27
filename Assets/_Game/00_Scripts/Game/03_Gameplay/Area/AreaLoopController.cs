
using System;
using UnityEngine;

/// <summary>
/// Scrolls a stack of equally tall area tiles downward and recycles the
/// bottom one to the top once it has fully left the camera's view.
///
/// Tiles are positioned by their centre (area prefabs are centred sprites).
/// The recycle point follows the camera's real bottom edge, so it stays
/// correct whatever the camera framing/aspect is, and several tiles can be
/// recycled in one frame after a long hitch.
/// </summary>
public class AreaLoopController : MonoBehaviour
{
    [Header("Scrolling Settings")]
    [SerializeField, Min(0f)] private float scrollSpeed = 3f;

    [Tooltip("Fallback world Y for recycling (tile centre) when no camera is available.")]
    [SerializeField] private float recycleY = -10f;

    [Tooltip("Height of one area tile (distance between consecutive tile centres).")]
    [SerializeField, Min(0.01f)] private float areaHeight = 20f;

    [Tooltip("How far below the camera's bottom edge a tile's top must be before it is recycled.")]
    [SerializeField, Min(0f)] private float recycleMargin = 0.5f;

    // A hair of overlap hides sub-pixel rasterisation seams between tiles.
    private const float SeamOverlap = 0.02f;

    private Transform[] areas;
    private Camera viewCamera;
    private bool isInitialized;

    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = Mathf.Max(0f, value);
    }

    public float AreaHeight => areaHeight;

    public int SlotCount => areas != null ? areas.Length : 0;

    public event Action<int, Vector3> AreaRecycled;

    /// <summary>Two-tile overload (kept for older callers).</summary>
    public void Initialize(Transform firstArea, Transform secondArea, float height, float speed, float recyclePositionY)
    {
        Initialize(new[] { firstArea, secondArea }, height, speed, recyclePositionY);
    }

    public void Initialize(Transform[] tiles, float height, float speed, float recyclePositionY)
    {
        if (tiles == null || tiles.Length < 2 || Array.Exists(tiles, t => t == null))
        {
            Debug.LogError("[AreaLoopController] Needs at least two non-null areas.", this);
            return;
        }

        if (height <= 0f)
        {
            Debug.LogError("[AreaLoopController] Area height must be greater than zero.", this);
            return;
        }

        areas = (Transform[])tiles.Clone();
        areaHeight = height;
        scrollSpeed = Mathf.Max(0f, speed);
        recycleY = recyclePositionY;
        viewCamera = Camera.main;

        isInitialized = true;
    }

    public void ReplaceArea(int index, Transform replacement)
    {
        if (areas == null || index < 0 || index >= areas.Length)
        {
            Debug.LogError($"[AreaLoopController] Invalid area index: {index}.", this);
            return;
        }

        if (replacement == null)
        {
            Debug.LogError("[AreaLoopController] Replacement area is null.", this);
            return;
        }

        areas[index] = replacement;
    }

    /// <summary>
    /// World Y of the camera's bottom edge on the z = 0 play plane, or NaN
    /// when there is no camera.
    /// </summary>
    public static float CameraBottomY(Camera cam)
    {
        if (cam == null)
            return float.NaN;

        float distance = Mathf.Abs(cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, distance)).y;
    }

    /// <summary>World Y of the camera's top edge on the z = 0 play plane, or NaN.</summary>
    public static float CameraTopY(Camera cam)
    {
        if (cam == null)
            return float.NaN;

        float distance = Mathf.Abs(cam.transform.position.z);
        return cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, distance)).y;
    }

    private void Update()
    {
        if (!isInitialized || areas == null)
            return;

        // Move every area first, then recycle. Recycling while the others
        // were still unmoved this frame left a one-frame gap (a visible seam).
        Vector3 step = Vector3.down * (scrollSpeed * Time.deltaTime);

        for (int i = 0; i < areas.Length; i++)
        {
            if (areas[i] != null)
                areas[i].position += step;
        }

        if (viewCamera == null)
            viewCamera = Camera.main;

        float bottom = CameraBottomY(viewCamera);

        // A tile leaves the view when its top edge is below the camera bottom.
        float threshold = float.IsNaN(bottom)
            ? recycleY
            : bottom - recycleMargin - areaHeight * 0.5f;

        // Recycle the lowest tiles first; repeat in case one frame moved
        // more than one tile height (long hitch, tab switch).
        for (int guard = 0; guard < areas.Length; guard++)
        {
            int lowest = LowestIndex();

            if (lowest < 0 || areas[lowest].position.y > threshold)
                break;

            RecycleArea(lowest);
        }
    }

    private int LowestIndex()
    {
        int index = -1;
        float lowestY = float.PositiveInfinity;

        for (int i = 0; i < areas.Length; i++)
        {
            if (areas[i] != null && areas[i].position.y < lowestY)
            {
                lowestY = areas[i].position.y;
                index = i;
            }
        }

        return index;
    }

    private void RecycleArea(int index)
    {
        Transform area = areas[index];

        if (area == null)
            return;

        float highestY = float.NegativeInfinity;

        foreach (Transform otherArea in areas)
        {
            if (otherArea == null || otherArea == area)
                continue;

            highestY = Mathf.Max(highestY, otherArea.position.y);
        }

        if (float.IsNegativeInfinity(highestY))
            highestY = area.position.y;

        Vector3 newPosition = area.position;
        newPosition.y = highestY + areaHeight - SeamOverlap;

        area.position = newPosition;

        AreaRecycled?.Invoke(index, newPosition);
    }
}
