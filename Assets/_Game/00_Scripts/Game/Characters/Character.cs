using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;

    private string characterName;
    private int health;
    private int attackPower;
    private int defensePower;
    private float movementSpeed;

    private void Awake()
    {
        if (characterData != null)
        {
            characterName = characterData.characterName;
            health = characterData.health;
            attackPower = characterData.attackPower;
            defensePower = characterData.defensePower;
            movementSpeed = characterData.movementSpeed;
        }
        else
        {
            Debug.LogError("CharacterData is not assigned in the inspector.");
        }
    }

    public abstract void BaseAttack();
    public abstract void SpecialAttack();
}