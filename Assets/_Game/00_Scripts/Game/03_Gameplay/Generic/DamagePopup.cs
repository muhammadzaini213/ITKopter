using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField, Tooltip("This character's Health. Leave empty if it's on the same object.")]
    private Health health;

    [Header("Prefab")]
    [SerializeField, Tooltip("Number prefab (TextMeshProUGUI or TextMeshPro).")]
    private GameObject popupPrefab;

    [SerializeField, Tooltip("World Space Canvas child of this character. Can be empty if using a 3D TextMeshPro prefab.")]
    private Transform popupParent;

    [Header("Color")]
    [SerializeField] private Color lowDamageColor = Color.white;
    [SerializeField] private Color highDamageColor = new Color(0.45f, 0.02f, 0.02f);

    [SerializeField, Range(0.01f, 1f),
     Tooltip("Damage as a fraction of max HP at which the color is fully dark red.")]
    private float fullColorDamageFraction = 0.25f;

    [SerializeField] private Color healColor = new Color(0.15f, 0.85f, 0.2f);

    [Header("Placement & Animation")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1f, 0f);

    [SerializeField, Min(0f), Tooltip("Random jitter so numbers don't stack exactly.")]
    private float randomSpread = 0.3f;

    [SerializeField, Min(0.05f)] private float duration = 0.7f;

    [SerializeField, Min(0f), Tooltip("How far the number floats up.")]
    private float riseHeight = 1f;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health == null)
            Debug.LogWarning("[CharacterDamagePopup] Health is not assigned.", this);
    }

    private void OnEnable()
    {
        Health.OnAnyDamaged += HandleDamage;
        Health.OnAnyHealed += HandleHeal;
    }

    private void OnDisable()
    {
        Health.OnAnyDamaged -= HandleDamage;
        Health.OnAnyHealed -= HandleHeal;
    }

    // Blocked hits arrive as 0 and show a white "0".
    private void HandleDamage(Health h, float damage)
    {
        if (h == null || h != health)
            return; // not this character's hit

        float t = Mathf.Clamp01(damage / (health.MaxHealth * fullColorDamageFraction));
        Spawn(damage, Color.Lerp(lowDamageColor, highDamageColor, t));
    }

    private void HandleHeal(Health h, float amount)
    {
        if (h == null || h != health)
            return;

        Spawn(amount, healColor);
    }

    private void Spawn(float value, Color color)
    {
        if (popupPrefab == null)
            return;

        Vector3 start = health.transform.position + spawnOffset +
            (Vector3)(Random.insideUnitCircle * randomSpread);

        GameObject popup = popupParent != null
            ? Instantiate(popupPrefab, start, popupParent.rotation, popupParent)
            : Instantiate(popupPrefab, start, Quaternion.identity);

        TMP_Text text = popup.GetComponentInChildren<TMP_Text>(true);

        if (text == null)
        {
            Debug.LogWarning("[CharacterDamagePopup] Prefab has no TMP_Text.", popupPrefab);
            Destroy(popup);
            return;
        }

        text.text = Mathf.CeilToInt(value).ToString();
        text.color = color;

        StartCoroutine(Animate(popup, text, start));
    }

    private IEnumerator Animate(GameObject popup, TMP_Text text, Vector3 start)
    {
        Vector3 end = start + Vector3.up * riseHeight;
        float time = 0f;

        while (popup != null && time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            popup.transform.position = Vector3.Lerp(start, end, t);

            Color c = text.color;
            c.a = 1f - t;
            text.color = c;

            yield return null;
        }

        if (popup != null)
            Destroy(popup);
    }
}