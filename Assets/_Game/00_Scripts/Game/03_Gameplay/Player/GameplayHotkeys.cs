using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard shortcuts for gameplay:
/// 1 / 2 / 3 switch to the matching team slot, K fires the active character's EX skill.
/// Polls Keyboard.current directly, same as PlayerMovement.
/// </summary>
public class GameplayHotkeys : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSwitcher characterSwitcher;
    [SerializeField] private ExSkillController exSkillController;

    [Header("Keys")]
    [SerializeField] private Key[] switchKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };
    [SerializeField] private Key exSkillKey = Key.K;

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
    }
}
