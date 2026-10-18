
using System;
using UnityEngine;

public class CharacterLifeController : MonoBehaviour
{
    private GameObject[] characters;
    private Health[] healths;
    private bool[] defeated;
    private Action[] deathCallbacks;

    public event Action<int> CharacterDefeated;

    public bool IsInitialized { get; private set; }

    public int CharacterCount =>
        characters != null ? characters.Length : 0;

    public bool AllCharactersDefeated
    {
        get
        {
            if (!IsInitialized || CharacterCount == 0)
                return false;

            for (int i = 0; i < CharacterCount; i++)
            {
                if (IsAlive(i))
                    return false;
            }

            return true;
        }
    }

    public void Initialize(
        GameObject[] characterObjects,
        Health[] characterHealths,
        bool[] defeatedCharacters)
    {
        UnregisterCallbacks();

        IsInitialized = false;

        characters = characterObjects;
        healths = characterHealths;
        defeated = defeatedCharacters;

        if (characters == null ||
            healths == null ||
            defeated == null ||
            characters.Length == 0 ||
            characters.Length != healths.Length ||
            characters.Length != defeated.Length)
        {
            Debug.LogError(
                "[CharacterLifeController] Invalid character arrays.",
                this);

            return;
        }

        deathCallbacks = new Action[characters.Length];

        for (int i = 0; i < characters.Length; i++)
        {
            int index = i;

            if (characters[i] == null)
            {
                defeated[i] = true;
                continue;
            }

            if (healths[i] == null)
            {
                Debug.LogWarning(
                    $"[CharacterLifeController] Health is missing for character at index {i}.",
                    characters[i]);

                continue;
            }

            deathCallbacks[i] = () => MarkDefeated(index);
            healths[i].OnDeath += deathCallbacks[i];

            if (healths[i].IsDead)
                defeated[i] = true;

            if (defeated[i])
                characters[i].SetActive(false);
        }

        IsInitialized = true;

        Debug.Log(
            $"[CharacterLifeController] Initialized with {CharacterCount} characters.",
            this);
    }

    private void Update()
    {
        if (!IsInitialized)
            return;

        for (int i = 0; i < CharacterCount; i++)
        {
            if (defeated[i])
                continue;

            if (characters[i] == null)
            {
                MarkDefeated(i);
                continue;
            }

            if (healths[i] != null && healths[i].IsDead)
                MarkDefeated(i);
        }
    }

    public bool IsAlive(int index)
    {
        if (!IsInitialized ||
            index < 0 ||
            index >= CharacterCount ||
            defeated[index] ||
            characters[index] == null)
        {
            return false;
        }

        if (healths[index] != null && healths[index].IsDead)
            return false;

        return true;
    }

    public int FindNextLivingCharacter(int currentIndex)
    {
        if (!IsInitialized || CharacterCount == 0)
            return -1;

        for (int offset = 1; offset <= CharacterCount; offset++)
        {
            int index =
                ((currentIndex + offset) % CharacterCount
                + CharacterCount) % CharacterCount;

            if (IsAlive(index))
                return index;
        }

        return -1;
    }

    public bool HasLivingCharacterOtherThan(int currentIndex)
    {
        if (!IsInitialized)
            return false;

        for (int i = 0; i < CharacterCount; i++)
        {
            if (i != currentIndex && IsAlive(i))
                return true;
        }

        return false;
    }

    public void MarkDefeated(int index)
    {
        if (!IsInitialized ||
            index < 0 ||
            index >= CharacterCount ||
            defeated[index])
        {
            return;
        }

        defeated[index] = true;

        if (characters[index] != null)
            characters[index].SetActive(false);

        Debug.Log(
            $"[CharacterLifeController] Character at index {index} was defeated.",
            this);

        CharacterDefeated?.Invoke(index);
    }

    private void UnregisterCallbacks()
    {
        if (healths != null && deathCallbacks != null)
        {
            int count = Mathf.Min(
                healths.Length,
                deathCallbacks.Length);

            for (int i = 0; i < count; i++)
            {
                if (healths[i] != null &&
                    deathCallbacks[i] != null)
                {
                    healths[i].OnDeath -= deathCallbacks[i];
                }
            }
        }

        deathCallbacks = null;
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();
    }
}
