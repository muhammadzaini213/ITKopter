
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SwitchCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSwitcher characterSwitcher;

    [Header("Cooldown UI")]
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private void Awake()
    {
        if (cooldownSlider != null)
        {
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = 1f;
            cooldownSlider.value = 1f;
        }
    }

    private void Update()
    {
        if (characterSwitcher == null)
        {
            SetCooldownUI(0f, 0f);
            return;
        }

        float remaining = characterSwitcher.CooldownRemaining;
        float duration = characterSwitcher.SwitchCooldownDuration;

        SetCooldownUI(remaining, duration);
    }

    private void SetCooldownUI(float remaining, float duration)
    {
        remaining = Mathf.Max(0f, remaining);
        duration = Mathf.Max(0f, duration);

        if (cooldownSlider != null)
        {
            cooldownSlider.value = duration > 0f
                ? Mathf.Clamp01(1f - remaining / duration)
                : 1f;
        }

        if (cooldownText != null)
        {
            cooldownText.text = remaining > 0f
                ? $"{remaining:0.0}s"
                : "READY";
        }
    }
}
