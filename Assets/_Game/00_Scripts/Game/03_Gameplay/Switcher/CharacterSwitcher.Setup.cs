using UnityEngine;

// Setup validation and character initialisation (called from Start).
public partial class CharacterSwitcher
{
    private bool ValidateSetup()
    {
        if (characterParent == null || parentCollider == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] Assign Character Parent and Parent Collider.",
                this);

            return false;
        }

        if (parentCollider.transform != characterParent)
        {
            Debug.LogError(
                "[CharacterSwitcher] Parent Collider must belong to Character Parent.",
                this);

            return false;
        }

        if (frontPosition == null ||
            frontPosition.parent != characterParent)
        {
            Debug.LogError(
                "[CharacterSwitcher] Front Position must be a direct child of Character Parent.",
                this);

            return false;
        }

        if (switchButtons == null || switchButtons.Length < 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Assign an array with three button slots.",
                this);

            return false;
        }

        int teamSize = gameplayInitializer.InitializedCharacters.Count;

        if (teamSize < 1 || teamSize > 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Gameplay team must contain between one and three characters.",
                this);

            return false;
        }

        if (teamSize >= 2 &&
            (leftPosition == null ||
             leftPosition.parent != characterParent))
        {
            Debug.LogError(
                "[CharacterSwitcher] A valid Left Position is required for a team of at least two characters.",
                this);

            return false;
        }

        if (teamSize >= 3 &&
            (rightPosition == null ||
             rightPosition.parent != characterParent))
        {
            Debug.LogError(
                "[CharacterSwitcher] A valid Right Position is required for a team of three characters.",
                this);

            return false;
        }

        return true;
    }

    private bool InitializeCharacters()
    {
        var initializedCharacters =
            gameplayInitializer.InitializedCharacters;

        int count = initializedCharacters.Count;

        if (count < 1 || count > 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Invalid character count.",
                this);

            return false;
        }

        characters = new GameObject[count];
        childColliders = new BoxCollider2D[count];
        characterHealths = new Health[count];
        defeatedCharacters = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Character character = initializedCharacters[i];

            if (character == null)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] Character at index {i} is missing.",
                    this);

                return false;
            }

            GameObject characterObject = character.gameObject;

            if (characterObject.transform.parent != characterParent)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] Character '{characterObject.name}' must be parented to Character Parent.",
                    characterObject);

                return false;
            }

            BoxCollider2D childCollider =
                characterObject.GetComponent<BoxCollider2D>();

            if (childCollider == null)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] '{characterObject.name}' needs a BoxCollider2D on its root.",
                    characterObject);

                return false;
            }

            Rigidbody2D body =
                characterObject.GetComponent<Rigidbody2D>();

            if (body != null)
            {
                Debug.LogWarning(
                    $"[CharacterSwitcher] Removing Rigidbody2D from '{characterObject.name}' because movement is handled by the parent.",
                    characterObject);

                Destroy(body);
            }

            characters[i] = characterObject;
            childColliders[i] = childCollider;
            characterHealths[i] =
                characterObject.GetComponent<Health>();

            characterObject.transform.localPosition =
                frontPosition.localPosition;

            characterObject.transform.localRotation =
                Quaternion.identity;

            characterObject.SetActive(false);

            if (characterHealths[i] != null &&
                characterHealths[i].IsDead)
            {
                defeatedCharacters[i] = true;
            }
        }

        return true;
    }
}
