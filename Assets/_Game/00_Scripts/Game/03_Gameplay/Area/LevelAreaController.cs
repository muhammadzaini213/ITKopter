
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the scrolling background from the current level's parts and keeps
/// it fed: every time AreaLoopController recycles a tile, the next part's
/// area takes its place.
///
/// Areas are pooled (one inactive instance per prefab is created up front),
/// so recycling never instantiates or destroys anything mid-play; that used
/// to cause frame hitches and a late/empty tile.
/// </summary>
public class LevelAreaController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AreaLoopController areaLoopController;
    [SerializeField] private Transform areaContainer;

    [Header("Area Layout")]
    [Tooltip("Highest allowed centre Y for the first (lowest) area. It is lowered automatically so the camera's bottom edge is always covered.")]
    [SerializeField] private float startY = 10f;

    [Tooltip("Height of one area tile (area prefabs are 10x10, centred).")]
    [SerializeField, Min(0.01f)] private float areaHeight = 20f;

    [Tooltip("Fallback recycle Y (tile centre) when no camera is found.")]
    [SerializeField] private float recycleY = -10f;

    [Tooltip("Minimum number of tiles kept in the loop. Two 10-unit tiles can't cover a 12-unit view while scrolling.")]
    [SerializeField, Min(2)] private int minimumSlots = 3;

    [Header("Scrolling")]
    [SerializeField, Min(0f)] private float scrollSpeed = 3f;

    private LevelSystem levelSystem;
    private LevelPartData[] levelParts;
    private Transform[] activeAreas = new Transform[0];
    private readonly Dictionary<Transform, GameObject> prefabOf = new Dictionary<Transform, GameObject>();
    private readonly Dictionary<GameObject, Stack<Transform>> pool = new Dictionary<GameObject, Stack<Transform>>();

    private int nextPartIndex;
    private bool isBuilt;

    private void Awake()
    {
        if (levelSystem == null)
            levelSystem = LevelSystem.Instance;

        if (areaLoopController == null)
            areaLoopController = GetComponent<AreaLoopController>();

        if (areaContainer == null)
            areaContainer = transform;
    }

    private void OnEnable()
    {
        if (areaLoopController != null)
            areaLoopController.AreaRecycled += HandleAreaRecycled;
    }

    private void Start()
    {
        BuildLevelAreas();
    }

    private void OnDisable()
    {
        if (areaLoopController != null)
            areaLoopController.AreaRecycled -= HandleAreaRecycled;
    }

    public bool BuildLevelAreas()
    {
        if (isBuilt)
        {
            Debug.LogWarning("[LevelAreaController] Areas have already been built.", this);
            return false;
        }

        if (levelSystem == null)
        {
            Debug.LogError("[LevelAreaController] LevelSystem is missing.", this);
            return false;
        }

        if (areaLoopController == null)
        {
            Debug.LogError("[LevelAreaController] AreaLoopController is missing.", this);
            return false;
        }

        levelParts = levelSystem.CurrentLevelParts;

        if (levelParts == null || levelParts.Length == 0)
        {
            Debug.LogError("[LevelAreaController] The current level has no area parts.", this);
            return false;
        }

        if (areaHeight <= 0f)
        {
            Debug.LogError("[LevelAreaController] Area height must be greater than zero.", this);
            return false;
        }

        for (int i = 0; i < levelParts.Length; i++)
        {
            if (levelParts[i] == null)
            {
                Debug.LogError($"[LevelAreaController] Level part at index {i} is null.", this);
                return false;
            }

            if (levelParts[i].AreaPrefab == null)
            {
                Debug.LogError($"[LevelAreaController] Area prefab is missing for part '{levelParts[i].PartName}'.", this);
                return false;
            }
        }

        ClearExistingAreas();
        Prewarm();

        Camera cam = Camera.main;
        float bottom = AreaLoopController.CameraBottomY(cam);
        float top = AreaLoopController.CameraTopY(cam);

        // Enough tiles to cover the view plus one tile of scrolling.
        int slots = minimumSlots;
        if (!float.IsNaN(bottom) && !float.IsNaN(top))
            slots = Mathf.Max(minimumSlots, Mathf.CeilToInt((top - bottom) / areaHeight) + 1);

        // The lowest tile must already cover the camera's bottom edge.
        float firstCenter = startY;
        if (!float.IsNaN(bottom))
            firstCenter = Mathf.Min(startY, bottom + areaHeight * 0.5f - 0.5f);

        nextPartIndex = 0;
        activeAreas = new Transform[slots];

        for (int i = 0; i < slots; i++)
            activeAreas[i] = SpawnNextArea(new Vector3(0f, firstCenter + areaHeight * i, 0f));

        areaLoopController.Initialize(activeAreas, areaHeight, scrollSpeed, recycleY);

        isBuilt = true;

        Debug.Log($"[LevelAreaController] Built {slots} scrolling areas for level '{levelSystem.CurrentLevelName}'.", this);
        return true;
    }

    // One inactive instance per distinct prefab, so the first recycle of
    // each part doesn't instantiate mid-play either.
    private void Prewarm()
    {
        foreach (LevelPartData part in levelParts)
        {
            GameObject prefab = part.AreaPrefab;

            if (pool.ContainsKey(prefab))
                continue;

            pool[prefab] = new Stack<Transform>();
            Release(Create(part));
        }
    }

    private Transform Create(LevelPartData part)
    {
        GameObject areaObject = Instantiate(part.AreaPrefab, Vector3.zero, Quaternion.identity, areaContainer);
        areaObject.name = $"{part.PartName}_Area";
        prefabOf[areaObject.transform] = part.AreaPrefab;
        return areaObject.transform;
    }

    private void Release(Transform area)
    {
        if (area == null)
            return;

        area.gameObject.SetActive(false);

        if (prefabOf.TryGetValue(area, out GameObject prefab))
        {
            if (!pool.TryGetValue(prefab, out var stack))
                pool[prefab] = stack = new Stack<Transform>();

            stack.Push(area);
        }
        else
        {
            Destroy(area.gameObject);
        }
    }

    private Transform SpawnNextArea(Vector3 position)
    {
        if (levelParts == null || levelParts.Length == 0)
            return null;

        LevelPartData part = levelParts[nextPartIndex];
        nextPartIndex = (nextPartIndex + 1) % levelParts.Length;

        Transform area = null;

        if (pool.TryGetValue(part.AreaPrefab, out var stack))
        {
            while (stack.Count > 0 && area == null)
                area = stack.Pop();
        }

        if (area == null)
            area = Create(part);

        area.position = position;
        area.gameObject.SetActive(true);
        return area;
    }

    private void HandleAreaRecycled(int index, Vector3 position)
    {
        if (index < 0 || index >= activeAreas.Length)
            return;

        // Take the next part's tile from the pool, then return the old one;
        // the old tile becomes available for a later recycle.
        Transform oldArea = activeAreas[index];
        Transform newArea = SpawnNextArea(position);

        if (newArea == null)
        {
            Debug.LogError("[LevelAreaController] Failed to spawn the next area.", this);
            return;
        }

        if (oldArea != null && oldArea != newArea)
            Release(oldArea);

        activeAreas[index] = newArea;
        areaLoopController.ReplaceArea(index, newArea);
    }

    private void ClearExistingAreas()
    {
        for (int i = 0; i < activeAreas.Length; i++)
        {
            if (activeAreas[i] != null)
            {
                Destroy(activeAreas[i].gameObject);
                activeAreas[i] = null;
            }
        }

        foreach (var stack in pool.Values)
        {
            foreach (Transform t in stack)
            {
                if (t != null)
                    Destroy(t.gameObject);
            }
        }

        pool.Clear();
        prefabOf.Clear();
        isBuilt = false;
    }
}
