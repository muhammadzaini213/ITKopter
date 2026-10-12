using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;
    private CharacterProgression characterProgression;
    private string characterName;
    private float health;
    private float attackPower;
    private float defensePower;
    private float movementSpeed;

    private void Awake()
    {
        if (characterData != null)
        {
            characterName = characterData.CharacterName;
            health = characterData.Health;
            attackPower = characterData.AttackPower;
            defensePower = characterData.DefensePower;
            movementSpeed = characterData.MovementSpeed;
        }
        else
        {
            Debug.LogError("CharacterData is not assigned in the inspector.");
        }
    }

    public abstract void BaseAttack();
    public abstract void SpecialAttack();
}