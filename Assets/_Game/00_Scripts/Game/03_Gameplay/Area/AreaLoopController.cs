
using System;
using UnityEngine;

public class AreaLoopController : MonoBehaviour
{
    [Header("Scrolling Settings")]
    [SerializeField, Min(0f)] private float scrollSpeed = 3f;

    [Tooltip("World Y position at which an area's top edge triggers recycling.")]
    [SerializeField] private float recycleY = -10f;

    [Tooltip("Distance between the top edges of consecutive areas.")]
    [SerializeField, Min(0.01f)] private float areaHeight = 20f;

    private Transform[] areas;
    private bool isInitialized;

    public float ScrollSpeed
    {
        get => scrollSpeed;
        set => scrollSpeed = Mathf.Max(0f, value);
    }

    public float AreaHeight => areaHeight;

    public event Action<int, Vector3> AreaRecycled;

    public void Initialize(
        Transform firstArea,
        Transform secondArea,
        float height,
        float speed,
        float recyclePositionY)
    {
        if (firstArea == null || secondArea == null)
        {
            Debug.LogError(
                "[AreaLoopController] Cannot initialize with null areas.",
                this);
            return;
        }

        if (height <= 0f)
        {
            Debug.LogError(
                "[AreaLoopController] Area height must be greater than zero.",
                this);
            return;
        }

        areas = new[] { firstArea, secondArea };
        areaHeight = height;
        scrollSpeed = Mathf.Max(0f, speed);
        recycleY = recyclePositionY;

        isInitialized = true;
    }

    public void ReplaceArea(int index, Transform replacement)
    {
        if (areas == null || index < 0 || index >= areas.Length)
        {
            Debug.LogError(
                $"[AreaLoopController] Invalid area index: {index}.",
                this);
            return;
        }

        if (replacement == null)
        {
            Debug.LogError(
                "[AreaLoopController] Replacement area is null.",
                this);
            return;
        }

        areas[index] = replacement;
    }

    private void Update()
    {
        if (!isInitialized || areas == null)
            return;

        for (int i = 0; i < areas.Length; i++)
        {
            Transform area = areas[i];

            if (area == null)
                continue;

            area.position +=
                Vector3.down * (scrollSpeed * Time.deltaTime);

            if (area.position.y <= recycleY)
            {
                RecycleArea(i);
            }
        }
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
        {
            highestY = area.position.y;
        }

        Vector3 newPosition = area.position;
        newPosition.y = highestY + areaHeight;

        area.position = newPosition;

        AreaRecycled?.Invoke(index, newPosition);
    }
}
