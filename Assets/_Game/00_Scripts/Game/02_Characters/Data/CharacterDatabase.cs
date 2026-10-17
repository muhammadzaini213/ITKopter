
using UnityEngine;

[CreateAssetMenu(
    fileName = "CharacterDatabase",
    menuName = "Scriptable Objects/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] private CharacterData[] characters;

    public CharacterData[] Characters => characters;

    public CharacterData FindById(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) ||
            characters == null)
        {
            return null;
        }

        foreach (CharacterData character in characters)
        {
            if (character != null &&
                character.CharacterId == characterId)
            {
                return character;
            }
        }

        return null;
    }
}
