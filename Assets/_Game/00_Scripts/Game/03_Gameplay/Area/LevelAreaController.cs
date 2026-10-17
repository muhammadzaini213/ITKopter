
using UnityEngine;

public class LevelAreaController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AreaLoopController areaLoopController;
    [SerializeField] private Transform areaContainer;

    [Header("Area Layout")]
    [Tooltip("World Y position of the top edge reference of the first area.")]
    [SerializeField] private float startY = 10f;

    [Tooltip("Distance between the top edge references of consecutive areas.")]
    [SerializeField, Min(0.01f)] private float areaHeight = 20f;

    [Tooltip("World Y position at which an area is recycled.")]
    [SerializeField] private float recycleY = -10f;

    [Header("Scrolling")]
    [SerializeField, Min(0f)] private float scrollSpeed = 3f;

    private LevelSystem levelSystem;
    private LevelPartData[] levelParts;
    private Transform[] activeAreas = new Transform[2];

    private int nextPartIndex;
    private bool isBuilt;

    private void Awake()
    {
        if (levelSystem == null)
        {
            levelSystem = LevelSystem.Instance;
        }

        if (areaLoopController == null)
        {
            areaLoopController = GetComponent<AreaLoopController>();
        }

        if (areaContainer == null)
        {
            areaContainer = transform;
        }
    }

    private void OnEnable()
    {
        if (areaLoopController != null)
        {
            areaLoopController.AreaRecycled += HandleAreaRecycled;
        }
    }

    private void Start()
    {
        BuildLevelAreas();
    }

    private void OnDisable()
    {
        if (areaLoopController != null)
        {
            areaLoopController.AreaRecycled -= HandleAreaRecycled;
        }
    }

    public bool BuildLevelAreas()
    {
        if (isBuilt)
        {
            Debug.LogWarning(
                "[LevelAreaController] Areas have already been built.",
                this);
            return false;
        }

        if (levelSystem == null)
        {
            Debug.LogError(
                "[LevelAreaController] LevelSystem is missing.",
                this);
            return false;
        }

        if (areaLoopController == null)
        {
            Debug.LogError(
                "[LevelAreaController] AreaLoopController is missing.",
                this);
            return false;
        }

        levelParts = levelSystem.CurrentLevelParts;

        if (levelParts == null || levelParts.Length == 0)
        {
            Debug.LogError(
                "[LevelAreaController] The current level has no area parts.",
                this);
            return false;
        }

        if (areaHeight <= 0f)
        {
            Debug.LogError(
                "[LevelAreaController] Area height must be greater than zero.",
                this);
            return false;
        }

        for (int i = 0; i < levelParts.Length; i++)
        {
            if (levelParts[i] == null)
            {
                Debug.LogError(
                    $"[LevelAreaController] Level part at index {i} is null.",
                    this);
                return false;
            }

            if (levelParts[i].AreaPrefab == null)
            {
                Debug.LogError(
                    $"[LevelAreaController] Area prefab is missing for part '{levelParts[i].PartName}'.",
                    this);
                return false;
            }
        }

        ClearExistingAreas();

        nextPartIndex = 0;

        activeAreas[0] = SpawnNextArea(
            new Vector3(0f, startY, 0f));

        activeAreas[1] = SpawnNextArea(
            new Vector3(0f, startY + areaHeight, 0f));

        if (activeAreas[0] == null || activeAreas[1] == null)
        {
            ClearExistingAreas();
            return false;
        }

        areaLoopController.Initialize(
            activeAreas[0],
            activeAreas[1],
            areaHeight,
            scrollSpeed,
            recycleY);

        isBuilt = true;

        Debug.Log(
            $"[LevelAreaController] Built {activeAreas.Length} scrolling areas for level '{levelSystem.CurrentLevelName}'.",
            this);

        return true;
    }

    private Transform SpawnNextArea(Vector3 position)
    {
        if (levelParts == null || levelParts.Length == 0)
            return null;

        LevelPartData part = levelParts[nextPartIndex];

        nextPartIndex = (nextPartIndex + 1) % levelParts.Length;

        GameObject areaObject = Instantiate(
            part.AreaPrefab,
            position,
            Quaternion.identity,
            areaContainer);

        areaObject.name = $"{part.PartName}_Area";

        Debug.Log(
            $"[LevelAreaController] Spawned area part '{part.PartName}'.",
            areaObject);

        return areaObject.transform;
    }

    private void HandleAreaRecycled(int index, Vector3 position)
    {
        if (index < 0 || index >= activeAreas.Length)
            return;

        Transform oldArea = activeAreas[index];

        if (oldArea != null)
        {
            Destroy(oldArea.gameObject);
        }

        Transform newArea = SpawnNextArea(position);

        if (newArea == null)
        {
            Debug.LogError(
                "[LevelAreaController] Failed to spawn the next area.",
                this);
            return;
        }

        activeAreas[index] = newArea;

        areaLoopController.ReplaceArea(index, newArea);

        Debug.Log(
            $"[LevelAreaController] Recycled area slot {index}.",
            newArea);
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

        isBuilt = false;
    }
}
