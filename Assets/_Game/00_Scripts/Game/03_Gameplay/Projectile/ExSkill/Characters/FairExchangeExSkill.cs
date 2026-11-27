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

    private enum Trade { None, Defense, Attack }

    private TradyExchangeChoiceUI choiceUI;
    private Coroutine durationRoutine;
    private Shield shield;
    private Health holderHealth;
    private VfxRing aura;
    private VfxRing auraOuter;
    private VfxText label;
    private bool choiceMade;
    private Trade trade;

    protected override Color DefaultColor => new Color(0.2f, 1f, 0.7f);

    protected override string[] DefaultActivationLines => new[] { "Let's make a fair exchange.", "Everything has a price." };

    // The pause popup is the "moment"; the burst plays on the choice instead.
    public override bool PlayDefaultActivationFeel => false;

    // The trade is a team deal: it moves to whoever is switched in.
    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"Pauses the fight and offers a trade:\n\n" +
               $"<color=#5FB8FF>DEFENSE</color>: a shield worth {DefPct(defenseShieldMultiplier, defense)} for {Seconds(defenseDuration)}, but -{Pct(offensePenaltyMultiplier)} ATK.\n\n" +
               $"<color=#FF8C33>ATTACK</color>: ATK becomes {Pct(attackDamageMultiplier)} and fire rate {Pct(attackSpeedMultiplier)} for {Seconds(attackDuration)}, but DEF drops to {Pct(defensePenaltyMultiplier)}.\n\n" +
               "The trade carries over to whoever Trady switches to.";
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
        trade = Trade.Defense;
        BeginFollowField();

        float amount = Mathf.Max(1f, Owner.EffectiveDefensePower * defenseShieldMultiplier);

        var shieldGo = new GameObject("FairExchangeDefenseShield");
        shieldGo.transform.SetParent(FieldHolder.transform, false);

        shield = shieldGo.AddComponent<Shield>();
        shield.Set(amount);
        shield.OnAmountChanged += OnShieldChanged;
        shield.OnBroken += OnShieldBroken;

        holderHealth = FieldHolder.GetComponent<Health>();
        if (holderHealth != null)
            holderHealth.AssignShield(shield);

        ApplyTradeTo(FieldHolder);
        PlayChoiceFeel("DEFENSE TRADE", DefenseColor);

        StartTimer(defenseDuration);
    }

    private void OnAttackChosen()
    {
        if (Owner == null)
            return;

        choiceMade = true;
        trade = Trade.Attack;
        BeginFollowField();

        ApplyTradeTo(FieldHolder);
        PlayChoiceFeel("ATTACK TRADE", AttackColor);

        StartTimer(attackDuration);
    }

    // Stat changes + aura for the current holder of the trade.
    private void ApplyTradeTo(Character holder)
    {
        if (holder == null)
            return;

        StopVisuals();

        float radius = SkillVFX.GetVisualRadius(holder.transform);

        if (trade == Trade.Defense)
        {
            ApplyAttack(holder, this, -holder.AttackPower * offensePenaltyMultiplier);

            if (shield != null)
            {
                aura = SkillVFX.Aura(holder.transform, DefenseColor, radius * 1.5f, 0.1f, -1f, 0.03f);
                auraOuter = SkillVFX.Aura(holder.transform, DefenseColor.WithAlpha(0.5f), radius * 1.7f, 0.04f, -1f, 0.03f, 6, 1, 30f);
                OnShieldChanged(shield.Current);
            }

            label = SkillVFX.Label(holder.transform, Vector3.up * (radius + 0.5f), "DEFENSE TRADE", DefenseColor, 3f);
        }
        else if (trade == Trade.Attack)
        {
            ApplyAttack(holder, this, holder.AttackPower * Mathf.Max(0f, attackDamageMultiplier - 1f));
            ApplyFireInterval(holder, this, 1f / Mathf.Max(0.05f, attackSpeedMultiplier));
            ApplyDefense(holder, this, -holder.DefensePower * (1f - defensePenaltyMultiplier));

            aura = SkillVFX.Aura(holder.transform, AttackColor, radius * 1.35f, 0.07f, -1f, 0.1f, 3, 1, 180f);
            auraOuter = SkillVFX.Aura(holder.transform, AttackColor.WithAlpha(0.5f), radius * 1.6f, 0.04f, -1f, 0.08f, 3, 1, -180f);
            label = SkillVFX.Label(holder.transform, Vector3.up * (radius + 0.5f), "ATTACK TRADE", AttackColor, 3f);
        }
    }

    protected override void OnFieldHolderChanged(Character previous, Character next)
    {
        ClearModifiers(previous, this);

        if (shield != null)
            holderHealth = MoveShield(shield, holderHealth, next);

        ApplyTradeTo(next);
    }

    private void PlayChoiceFeel(string title, Color color)
    {
        Transform holder = FieldHolder != null ? FieldHolder.transform : Owner.transform;
        Vector3 pos = holder.position;
        float radius = SkillVFX.GetVisualRadius(holder);

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

        if (FieldHolder != null)
            SkillVFX.Sparks(FieldHolder.transform.position, DefenseColor, 4, 3f, 0.2f, 0.08f);
    }

    private void OnShieldBroken()
    {
        if (FieldHolder != null)
        {
            SkillVFX.Explosion(FieldHolder.transform.position, DefenseColor, 1.6f, withFeel: false);
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

        if (holderHealth != null && holderHealth.Shield == shield)
            holderHealth.RemoveShield();

        if (shield != null)
            Destroy(shield.gameObject);

        shield = null;
    }

    private void StopVisuals()
    {
        if (aura != null) aura.Stop();
        if (auraOuter != null) auraOuter.Stop();
        if (label != null) label.Stop();

        aura = null;
        auraOuter = null;
        label = null;
    }

    private void EndTrade()
    {
        RemoveShield();
        ClearPartyModifiers(this);
        StopVisuals();
        EndFollowField();

        choiceMade = false;
        trade = Trade.None;
        holderHealth = null;
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
