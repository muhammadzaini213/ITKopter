
using UnityEngine;

public class CharacterCombatController : MonoBehaviour
{
    [SerializeField]
    private string autoShootComponentName = "AutoShoot";

    private GameObject[] characters;
    private bool[] defeatedCharacters;
    private bool isInitialized;

    public void Initialize(
        GameObject[] characterObjects,
        bool[] defeated)
    {
        if (characterObjects == null ||
            defeated == null ||
            characterObjects.Length != defeated.Length)
        {
            Debug.LogError(
                "[CharacterCombatController] Invalid character arrays.",
                this);

            isInitialized = false;
            return;
        }

        characters = characterObjects;
        defeatedCharacters = defeated;
        isInitialized = true;
    }

    public void RefreshAutoShoot(
        int activeCharacterIndex,
        bool isSwitching)
    {
        if (!isInitialized)
            return;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            bool shouldShoot =
                !defeatedCharacters[i] &&
                (isSwitching || i == activeCharacterIndex);

            SetAutoShoot(characters[i], shouldShoot);
        }
    }

    private void SetAutoShoot(
        GameObject character,
        bool isEnabled)
    {
        MonoBehaviour[] components =
            character.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour component in components)
        {
            if (component == null)
                continue;

            if (component.GetType().Name != autoShootComponentName)
                continue;

            component.enabled = isEnabled;
        }
    }

    public float GetAverageMovementSpeed(
        int activeCharacterIndex,
        bool isSwitching,
        Health[] healths)
    {
        if (!isInitialized)
            return 0f;

        if (!isSwitching)
        {
            if (activeCharacterIndex < 0 ||
                activeCharacterIndex >= characters.Length ||
                defeatedCharacters[activeCharacterIndex])
            {
                return 0f;
            }

            Character active =
                characters[activeCharacterIndex]
                    .GetComponent<Character>();

            if (active == null)
                return 0f;

            Health health = healths[activeCharacterIndex];

            if (health != null && health.IsDead)
                return 0f;

            return active.MovementSpeed;
        }

        float totalSpeed = 0f;
        int count = 0;

        for (int i = 0; i < characters.Length; i++)
        {
            if (defeatedCharacters[i] || characters[i] == null)
                continue;

            Health health = healths[i];

            if (health != null && health.IsDead)
                continue;

            Character character =
                characters[i].GetComponent<Character>();

            if (character == null)
                continue;

            totalSpeed += character.MovementSpeed;
            count++;
        }

        return count > 0 ? totalSpeed / count : 0f;
    }
}
