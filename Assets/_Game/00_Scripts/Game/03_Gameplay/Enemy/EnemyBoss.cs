using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an enemy as a boss: shown on the boss health bar and skipped by the level's minion tint.
/// Story bosses are playable Concept Spirits fighting on the enemy side.
/// </summary>
public class EnemyBoss : MonoBehaviour
{
    private static readonly List<EnemyBoss> active = new();

    [SerializeField] private string displayName;
    [Tooltip("The playable character this boss represents (optional, for the name and icon).")]
    [SerializeField] private CharacterData character;
    [SerializeField] private Color barColor = Color.white;

    private Health health;

    public static IReadOnlyList<EnemyBoss> Active => active;

    /// <summary>Raised when a boss enters play.</summary>
    public static event System.Action<EnemyBoss> OnBossAppeared;

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(displayName) ? displayName :
        character != null ? character.CharacterName : name;

    public CharacterData Character => character;
    public Color BarColor => barColor;
    public Health Health => health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (!active.Contains(this))
        {
            active.Add(this);
            OnBossAppeared?.Invoke(this);
        }
    }

    private void OnDisable()
    {
        active.Remove(this);
    }
}
