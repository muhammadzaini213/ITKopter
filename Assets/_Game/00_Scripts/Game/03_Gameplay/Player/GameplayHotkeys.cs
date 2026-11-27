using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard shortcuts for gameplay:
/// 1 / 2 / 3 switch to the matching team slot, K fires the active character's EX skill,
/// L summons the Annihilator.
/// Polls Keyboard.current directly, same as PlayerMovement.
/// </summary>
public class GameplayHotkeys : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSwitcher characterSwitcher;
    [SerializeField] private ExSkillController exSkillController;
    [SerializeField, Tooltip("Empty = found in the scene.")]
    private AnnihilatorManifestation annihilator;

    [Header("Keys")]
    [SerializeField] private Key[] switchKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };
    [SerializeField] private Key exSkillKey = Key.K;
    [SerializeField] private Key annihilatorKey = Key.L;

    private void Start()
    {
        if (annihilator == null)
            annihilator = FindFirstObjectByType<AnnihilatorManifestation>();

        // Show the key on the Annihilator button ("READY [L]").
        if (annihilator != null)
            annihilator.HotkeyLabel = annihilatorKey.ToString();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // Pause sets timeScale to 0; ignore shortcuts while paused.
        if (Time.timeScale <= 0f)
            return;

        if (characterSwitcher != null)
        {
            for (int i = 0; i < switchKeys.Length; i++)
            {
                if (keyboard[switchKeys[i]].wasPressedThisFrame)
                {
                    characterSwitcher.SwitchTo(i);
                    break;
                }
            }
        }

        if (exSkillController != null &&
            keyboard[exSkillKey].wasPressedThisFrame)
        {
            exSkillController.UseExSkill();
        }

        if (annihilator != null &&
            keyboard[annihilatorKey].wasPressedThisFrame)
        {
            annihilator.ActivateManifestation();
        }
    }
}
