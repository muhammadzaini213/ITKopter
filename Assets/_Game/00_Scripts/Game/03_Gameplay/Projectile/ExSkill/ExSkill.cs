using System.Collections;
using UnityEngine;
using Slafurry.Systems.Audio;

public abstract class ExSkill : MonoBehaviour
{
    [Header("EX Skill Settings")]
    [SerializeField] private string skillName;

    [SerializeField, Tooltip("Signature colour for this skill's visuals. Alpha 0 = use the skill's default.")]
    private Color skillColor = new Color(0f, 0f, 0f, 0f);

    [Header("Activation Dialogue")]
    [SerializeField, TextArea(1, 2), Tooltip("Lines the character says (speaker toast) when the skill fires. Empty = built-in defaults.")]
    private string[] activationLines = new string[0];

    [Header("Audio")]
    [SerializeField] private SFXReference activationSfx;

    private Character owner;

    public string SkillName => skillName;
    public Character Owner => owner;
    public SFXReference ActivationSfx => activationSfx;
    public float Cooldown => owner != null
        ? owner.ExSkillCooldown
        : 0f;

    /// <summary>Colour used by all of this skill's visuals.</summary>
    public Color SkillColor => skillColor.a > 0f ? skillColor : DefaultColor;

    protected virtual Color DefaultColor => new Color(0.098f, 0.941f, 1f);

    /// <summary>
    /// Team / field skills keep running after the owner is switched out
    /// (their routines run on the scene's CombatFeel runner). Self skills
    /// end when their owner leaves the field.
    /// </summary>
    protected virtual bool PersistsWhenBenched => false;

    /// <summary>Built-in quips used when no lines are set in the Inspector.</summary>
    protected virtual string[] DefaultActivationLines => null;

    public string PickActivationLine()
    {
        string[] lines = activationLines != null && activationLines.Length > 0
            ? activationLines
            : DefaultActivationLines;

        if (lines == null || lines.Length == 0)
            return null;

        return lines[Random.Range(0, lines.Length)];
    }

    /// <summary>Whether the controller should play the generic activation burst.</summary>
    public virtual bool PlayDefaultActivationFeel => true;

    /// <summary>Anchor that follows whichever party member is on the field.</summary>
    protected Transform TeamAnchor =>
        owner != null && owner.transform.parent != null
            ? owner.transform.parent
            : owner != null ? owner.transform : transform;

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

    // ---- description (character detail "EX Skill" info) ----------------

    /// <summary>
    /// Rich-text description built from this skill's own values. attack /
    /// defense are the character's stats at the level being viewed (0 = hide
    /// the "~N" estimates).
    /// </summary>
    public virtual string Describe(float attack, float defense)
    {
        return "No description yet.";
    }

    protected static string AtkPct(float multiplier, float attack)
    {
        string pct = $"<color=#FFD447>{multiplier * 100f:0}% ATK</color>";
        return attack > 0f
            ? $"{pct} <color=#8FA9B5>(~{Mathf.RoundToInt(multiplier * attack)})</color>"
            : pct;
    }

    protected static string DefPct(float multiplier, float defense)
    {
        string pct = $"<color=#5FB8FF>{multiplier * 100f:0}% DEF</color>";
        return defense > 0f
            ? $"{pct} <color=#8FA9B5>(~{Mathf.RoundToInt(multiplier * defense)})</color>"
            : pct;
    }

    /// <summary>A plain percentage (e.g. per-stack bonuses).</summary>
    protected static string Pct(float fraction) => $"<color=#19F0FF>{fraction * 100f:0}%</color>";

    protected static string Num(float value, string unit = "") => $"<color=#19F0FF>{value:0.#}{unit}</color>";

    protected static string Seconds(float value) => Num(value, "s");

    // ---- routine helpers ------------------------------------------------

    protected Coroutine StartSkillRoutine(IEnumerator routine)
    {
        if (routine == null)
            return null;

        return PersistsWhenBenched
            ? CombatFeel.Run(routine)
            : StartCoroutine(routine);
    }

    protected void StopSkillRoutine(ref Coroutine routine)
    {
        if (routine == null)
            return;

        if (PersistsWhenBenched)
            CombatFeel.Stop(routine);
        else
            StopCoroutine(routine);

        routine = null;
    }

    // ---- following the on-field character ------------------------------

    /// <summary>
    /// The party member currently carrying this skill's effect. Starts as
    /// the owner; with <see cref="BeginFollowField"/> it moves to whoever is
    /// switched in, so buffs and shields aren't lost by switching.
    /// </summary>
    protected Character FieldHolder { get; private set; }

    private bool followingField;

    protected void BeginFollowField()
    {
        FieldHolder = owner;

        if (followingField)
            return;

        followingField = true;
        CharacterSwitcher.FieldCharacterChanged += HandleFieldCharacterChanged;
    }

    protected void EndFollowField()
    {
        if (followingField)
            CharacterSwitcher.FieldCharacterChanged -= HandleFieldCharacterChanged;

        followingField = false;
        FieldHolder = null;
    }

    private void HandleFieldCharacterChanged(Character next)
    {
        if (next == null || next == FieldHolder)
            return;

        Character previous = FieldHolder;
        FieldHolder = next;
        OnFieldHolderChanged(previous, next);

        SkillVFX.Ring(next.transform.position, SkillColor, 0.3f, 2.2f, 0.3f, 0.08f);
        SkillVFX.Text(next.transform.position + Vector3.up * 1.4f, "INHERITED", SkillColor, 3f, 0.8f);
    }

    /// <summary>Move this skill's effect from <paramref name="previous"/> to <paramref name="next"/>.</summary>
    protected virtual void OnFieldHolderChanged(Character previous, Character next)
    {
    }

    /// <summary>Hand an active shield over to another party member.</summary>
    protected static Health MoveShield(Shield shield, Health from, Character to)
    {
        if (shield == null || to == null)
            return from;

        if (from != null && from.Shield == shield)
            from.RemoveShield();

        shield.transform.SetParent(to.transform, false);

        Health next = to.GetComponent<Health>();
        if (next != null && next.Shield == null)
            next.AssignShield(shield);

        return next;
    }

    protected virtual void OnDisable()
    {
        // Self skills end when the owner is benched / defeated.
        if (!PersistsWhenBenched)
            Cancel();
    }

    protected virtual void OnDestroy()
    {
        Cancel();
        EndFollowField();
    }

    // ---- stat helpers ---------------------------------------------------

    protected static void ApplyAttack(Character target, object source, float flat)
    {
        if (target != null) target.SetAttackModifier(source, flat);
    }

    protected static void ApplyDefense(Character target, object source, float flat)
    {
        if (target != null) target.SetDefenseModifier(source, flat);
    }

    protected static void ApplyFireInterval(Character target, object source, float multiplier)
    {
        if (target != null) target.SetFireIntervalModifier(source, multiplier);
    }

    protected static void ClearModifiers(Character target, object source)
    {
        if (target != null) target.RemoveModifiers(source);
    }

    protected void ClearPartyModifiers(object source)
    {
        foreach (var member in SkillTargets.Party)
            ClearModifiers(member, source);
    }
}
