
using System.Collections.Generic;
using UnityEngine;

public class CharacterStatUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameplayInitializer gameplayInitializer;
    [SerializeField] private ExSkillController exSkillController;

    [Header("Character Panels")]
    [SerializeField] private CharacterStatPanel[] characterPanels;

    private void Start()
    {
        InitializeUI();
    }

    public void InitializeUI()
    {
        if (gameplayInitializer == null)
        {
            Debug.LogError(
                "[CharacterStatUI] GameplayInitializer is not assigned.",
                this);
            return;
        }

        IReadOnlyList<Character> characters =
            gameplayInitializer.InitializedCharacters;

        if (characters == null)
        {
            Debug.LogError(
                "[CharacterStatUI] Initialized character list is unavailable.",
                this);
            return;
        }

        if (characterPanels == null || characterPanels.Length == 0)
        {
            Debug.LogError(
                "[CharacterStatUI] No character panels are assigned.",
                this);
            return;
        }

        for (int i = 0; i < characterPanels.Length; i++)
        {
            CharacterStatPanel panel = characterPanels[i];

            if (panel == null)
                continue;

            if (i < characters.Count && characters[i] != null)
            {
                panel.gameObject.SetActive(true);
                panel.Initialize(characters[i], exSkillController);
            }
            else
            {
                panel.gameObject.SetActive(false);
            }
        }
    }
}
