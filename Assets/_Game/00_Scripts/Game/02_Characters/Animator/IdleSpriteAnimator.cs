using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class IdleSpriteAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField, Tooltip("Frames played in order, looping.")]
    private Sprite[] frames;

    [SerializeField, Min(0.01f), Tooltip("Seconds each frame is shown.")]
    private float frameDuration = 0.2f;

    [SerializeField, Tooltip("Start at a random point so multiple characters don't animate in sync.")]
    private bool randomizeStart = true;

    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private int currentIndex = -1;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        currentIndex = -1;

        if (randomizeStart && frames != null && frames.Length > 0)
            elapsed = UnityEngine.Random.Range(0f, frameDuration * frames.Length);

        UpdateFrame();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
            return;

        elapsed += Time.deltaTime;

        // Wrap the timer so it never grows unbounded.
        float cycleLength = frameDuration * frames.Length;
        if (elapsed >= cycleLength)
            elapsed %= cycleLength;

        UpdateFrame();
    }

    private void UpdateFrame()
    {
        if (frames == null || frames.Length == 0)
            return;

        int index = Mathf.Min((int)(elapsed / frameDuration), frames.Length - 1);

        // Only touch the renderer when the frame actually changes.
        if (index == currentIndex)
            return;

        currentIndex = index;
        spriteRenderer.sprite = frames[index];
    }
}