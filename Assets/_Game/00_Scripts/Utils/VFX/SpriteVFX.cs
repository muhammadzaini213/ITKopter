using UnityEngine;

namespace Slafurry.Utils.VFX
{
    /// <summary>
    /// Plays a sprite-sheet animation on a SpriteRenderer, then removes
    /// itself. Attach to a VFX prefab root (with SpriteRenderer).
    /// If the prefab also has VFXCleaner, "removed" means released back
    /// to the pool instead of destroyed.
    /// </summary>
    public class SpriteVFX : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField]
        private Sprite[] frames;

        [SerializeField, Min(1)]
        private float framesPerSecond = 12f;

        private float timer;
        private int index;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            // Reset so pooled reuse restarts from frame 0.
            timer = 0f;
            index = 0;

            if (frames != null && frames.Length > 0 && spriteRenderer != null)
                spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || spriteRenderer == null)
            {
                Finish();
                return;
            }

            timer += Time.deltaTime;
            float step = 1f / framesPerSecond;

            while (timer >= step)
            {
                timer -= step;
                index++;

                if (index >= frames.Length)
                {
                    Finish();
                    return;
                }

                spriteRenderer.sprite = frames[index];
            }
        }

        private void Finish()
        {
            if (TryGetComponent(out VFXCleaner cleaner))
                cleaner.Clean();
            else
                Destroy(gameObject);
        }
    }
}
