using System.Collections;
using UnityEngine;

// Trady — Fair Exchange.
// Opens a choice popup (game paused):
//   DEFENSE — big shield bubble, weaker attack
//   ATTACK  — huge attack + fire rate, weaker defense
public class FairExchangeExSkill : ExSkill
{
    [Header("Defense Settings")]
    [SerializeField, Min(0f)]
    private float defenseShieldMultiplier = 4f;

    [SerializeField, Min(0f)]
    private float defenseDuration = 8f;

    [SerializeField, Min(0f)]
    private float offensePenaltyMultiplier = 0.8f;

    [Header("Attack Settings")]
    [SerializeField, Min(0f)]
    private float attackDamageMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float attackSpeedMultiplier = 2f;

    [SerializeField, Min(0f)]
    private float attackDuration = 8f;

    [SerializeField, Min(0f)]
    private float defensePenaltyMultiplier = 0.6f;

    private static readonly Color DefenseColor = new Color(0.35f, 0.75f, 1f);
    private static readonly Color AttackColor = new Color(1f, 0.55f, 0.2f);

    private TradyExchangeChoiceUI choiceUI;
    private Coroutine durationRoutine;
    private Shield shield;
    private Health ownerHealth;
    private VfxRing aura;
    private VfxRing auraOuter;
    private VfxText label;
    private bool choiceMade;

    protected override Color DefaultColor => new Color(0.2f, 1f, 0.7f);

    protected override string[] DefaultActivationLines => new[] { "Let's make a fair exchange.", "Everything has a price." };

    // The pause popup is the "moment"; the burst plays on the choice instead.
    public override bool PlayDefaultActivationFeel => false;

    public override string Describe(float attack, float defense)
    {
        return $"Pauses the fight and offers a trade:\n\n" +
               $"<color=#5FB8FF>DEFENSE</color>: a shield worth {DefPct(defenseShieldMultiplier, defense)} for {Seconds(defenseDuration)}, but -{Pct(offensePenaltyMultiplier)} ATK.\n\n" +
               $"<color=#FF8C33>ATTACK</color>: ATK becomes {Pct(attackDamageMultiplier)} and fire rate {Pct(attackSpeedMultiplier)} for {Seconds(attackDuration)}, but DEF drops to {Pct(defensePenaltyMultiplier)}.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        choiceUI = Object.FindFirstObjectByType<TradyExchangeChoiceUI>();

        if (choiceUI == null)
        {
            // No popup in this scene — don't waste the skill, pick attack.
            OnAttackChosen();
            return;
        }

        choiceMade = false;
        ownerHealth = Owner.GetComponent<Health>();

        SkillVFX.Ghost(Owner.transform, SkillColor, 0.4f, 1.6f);

        choiceUI.Show(
            Owner,
            Mathf.Max(defenseDuration, attackDuration),
            OnDefenseChosen,
            OnAttackChosen);
    }

    private void OnDefenseChosen()
    {
        if (Owner == null)
            return;

        choiceMade = true;
        ownerHealth = Owner.GetComponent<Health>();

        float amount = Mathf.Max(1f, Owner.EffectiveDefensePower * defenseShieldMultiplier);

        var shieldGo = new GameObject("FairExchangeDefenseShield");
        shieldGo.transform.SetParent(Owner.transform, false);

        shield = shieldGo.AddComponent<Shield>();
        shield.Set(amount);
        shield.OnAmountChanged += OnShieldChanged;
        shield.OnBroken += OnShieldBroken;

        if (ownerHealth != null)
            ownerHealth.AssignShield(shield);

        ApplyAttack(Owner, this, -Owner.AttackPower * offensePenaltyMultiplier);

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        aura = SkillVFX.Aura(Owner.transform, DefenseColor, radius * 1.5f, 0.1f, -1f, 0.03f);
        auraOuter = SkillVFX.Aura(Owner.transform, DefenseColor.WithAlpha(0.5f), radius * 1.7f, 0.04f, -1f, 0.03f, 6, 1, 30f);
        PlayChoiceFeel("DEFENSE TRADE", DefenseColor);

        StartTimer(defenseDuration);
    }

    private void OnAttackChosen()
    {
        if (Owner == null)
            return;

        choiceMade = true;

        ApplyAttack(Owner, this, Owner.AttackPower * Mathf.Max(0f, attackDamageMultiplier - 1f));
        ApplyFireInterval(Owner, this, 1f / Mathf.Max(0.05f, attackSpeedMultiplier));
        ApplyDefense(Owner, this, -Owner.DefensePower * (1f - defensePenaltyMultiplier));

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        aura = SkillVFX.Aura(Owner.transform, AttackColor, radius * 1.35f, 0.07f, -1f, 0.1f, 3, 1, 180f);
        auraOuter = SkillVFX.Aura(Owner.transform, AttackColor.WithAlpha(0.5f), radius * 1.6f, 0.04f, -1f, 0.08f, 3, 1, -180f);
        PlayChoiceFeel("ATTACK TRADE", AttackColor);

        StartTimer(attackDuration);
    }

    private void PlayChoiceFeel(string title, Color color)
    {
        Vector3 pos = Owner.transform.position;
        float radius = SkillVFX.GetVisualRadius(Owner.transform);

        label = SkillVFX.Label(Owner.transform, Vector3.up * (radius + 0.5f), title, color, 3f);
        SkillVFX.Activation(Owner, null, color);
        SkillVFX.Text(pos + Vector3.up * (radius + 1.2f), title, color, 4.5f, 1f);
    }

    private void OnShieldChanged(float current)
    {
        if (aura == null || shield == null || shield.Max <= 0f)
            return;

        float t = Mathf.Clamp01(current / shield.Max);
        aura.Width = Mathf.Lerp(0.03f, 0.1f, t);
        aura.Color = DefenseColor.WithAlpha(Mathf.Lerp(0.35f, 1f, t));

        if (Owner != null)
            SkillVFX.Sparks(Owner.transform.position, DefenseColor, 4, 3f, 0.2f, 0.08f);
    }

    private void OnShieldBroken()
    {
        if (Owner != null)
        {
            SkillVFX.Explosion(Owner.transform.position, DefenseColor, 1.6f, withFeel: false);
            CombatFeel.Shake(0.15f, 0.2f);
        }

        RemoveShield();

        if (aura != null) aura.Stop();
        if (auraOuter != null) auraOuter.Stop();
        aura = null;
        auraOuter = null;
    }

    private void StartTimer(float seconds)
    {
        StopSkillRoutine(ref durationRoutine);
        durationRoutine = StartSkillRoutine(DurationRoutine(seconds));
    }

    private IEnumerator DurationRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        durationRoutine = null;
        EndTrade();
    }

    private void RemoveShield()
    {
        if (shield == null)
            return;

        shield.OnAmountChanged -= OnShieldChanged;
        shield.OnBroken -= OnShieldBroken;

        if (ownerHealth != null && ownerHealth.Shield == shield)
            ownerHealth.RemoveShield();

        if (shield != null)
            Destroy(shield.gameObject);

        shield = null;
    }

    private void EndTrade()
    {
        RemoveShield();
        ClearModifiers(Owner, this);

        if (aura != null) aura.Stop();
        if (auraOuter != null) auraOuter.Stop();
        if (label != null) label.Stop();

        aura = null;
        auraOuter = null;
        label = null;
        choiceMade = false;
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref durationRoutine);

        // Benched while the popup is still open: close it so the game
        // doesn't stay paused waiting for a dead callback.
        if (!choiceMade && choiceUI != null && choiceUI.IsOpen)
            choiceUI.Close();

        EndTrade();
    }
}
