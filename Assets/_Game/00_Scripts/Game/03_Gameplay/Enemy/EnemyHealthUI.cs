
using UnityEngine;
using UnityEngine.UI;

// Enemy health bar: hidden until the enemy takes its first damage, then
// tracks Health (and hides again if healed back to full, optional).
//
// Put this on the enemy root (same object as Health) and assign barRoot =
// the child object holding the visual. Do NOT put this script on barRoot
// itself — it toggles that object's active state.
public class EnemyHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health health;

    [SerializeField, Tooltip("Child with the bar visual. Toggled on/off by this script.")]
    private GameObject barRoot;

    [SerializeField, Tooltip("Optional — UI Slider, value driven 0..1.")]
    private Slider slider;

    [SerializeField, Tooltip("Optional — fill Image, fillAmount driven 0..1.")]
    private Image fillImage;

    [Header("Visibility")]
    [SerializeField, Tooltip("Hide again when healed back to full HP.")]
    private bool hideWhenFull = true;

    [SerializeField, Tooltip("Hide the bar on death.")]
    private bool hideOnDeath = true;

    [Header("Billboard")]
    [SerializeField, Tooltip("Face the bar toward the camera (for world-space canvases).")]
    private bool billboard = true;

    private bool visible;
    private Camera cachedCamera;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health == null)
        {
            Debug.LogError("[EnemyHealthUI] No Health found on this object.", this);
            enabled = false;
            return;
        }

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.interactable = false;
        }

        if (fillImage != null)
            fillImage.fillAmount = 1f;

        if (barRoot == null)
        {
            Debug.LogError("[EnemyHealthUI] Bar Root is not assigned.", this);
            enabled = false;
            return;
        }

        barRoot.SetActive(false);

        // Spawned already damaged (startAtFullHealth off).
        if (health.HealthPercentage < 1f)
            Show();
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDeath -= HandleDeath;
    }

    private void LateUpdate()
    {
        if (!visible || !billboard || barRoot == null)
            return;

        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera != null)
            barRoot.transform.rotation = cachedCamera.transform.rotation;
    }

    // First actual damage (shield/defense fully absorbing does not fire it).
    private void HandleDamaged()
    {
        Show();
    }

    private void HandleHealthChanged(float current, float max)
    {
        SetFill(max > 0f ? current / max : 0f);

        if (hideWhenFull && !health.IsDead && current >= max)
            Hide();
        else
            Show();
    }

    private void HandleDeath()
    {
        SetFill(0f);

        if (hideOnDeath)
            Hide();
    }

    private void Show()
    {
        if (visible || barRoot == null)
            return;

        visible = true;
        barRoot.SetActive(true);
    }

    private void Hide()
    {
        if (!visible || barRoot == null)
            return;

        visible = false;
        barRoot.SetActive(false);
    }

    private void SetFill(float t)
    {
        if (slider != null)
            slider.value = t;

        if (fillImage != null)
            fillImage.fillAmount = t;
    }
}
