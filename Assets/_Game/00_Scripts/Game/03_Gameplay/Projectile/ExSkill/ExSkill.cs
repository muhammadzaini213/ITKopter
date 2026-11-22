
using UnityEngine;
using Slafurry.Systems.Audio;

public abstract class ExSkill : MonoBehaviour
{
    [Header("EX Skill Settings")]
    [SerializeField] private string skillName;

    [Header("Audio")]
    [SerializeField] private SFXReference activationSfx;

    private Character owner;

    public string SkillName => skillName;
    public Character Owner => owner;
    public SFXReference ActivationSfx => activationSfx;
    public float Cooldown => owner != null
        ? owner.ExSkillCooldown
        : 0f;

    public virtual void Initialize(Character character)
    {
        if (character == null)
        {
            Debug.LogError(
                "[ExSkill] Cannot initialize with a null Character.",
                this
            );
            return;
        }

        owner = character;
    }

    public virtual bool CanUse()
    {
        return owner != null &&
               owner.gameObject.activeInHierarchy;
    }

    public abstract void Activate();

    public virtual void Cancel()
    {
    }
}
