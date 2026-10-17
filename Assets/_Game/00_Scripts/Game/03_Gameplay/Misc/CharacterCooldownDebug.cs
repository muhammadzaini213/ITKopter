
using System;
using UnityEngine;

[Serializable]
public class CharacterCooldownDebug
{
    [SerializeField] private Character character;
    [SerializeField, Min(0f)] private float cooldownDuration;
    [SerializeField, Min(0f)] private float cooldownRemaining;

    public Character Character => character;
    public float CooldownDuration => cooldownDuration;
    public float CooldownRemaining => cooldownRemaining;

    public CharacterCooldownDebug(Character character, float duration)
    {
        this.character = character;
        cooldownDuration = duration;
        cooldownRemaining = 0f;
    }

    public void SetCooldown(float duration, float remaining)
    {
        cooldownDuration = duration;
        cooldownRemaining = remaining;
    }
}
