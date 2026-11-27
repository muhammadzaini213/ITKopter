using System.Collections;
using UnityEngine;

/// <summary>
/// Makes manual switching worth doing on purpose:
///
///   Tag-in burst    – the incoming character lands with a shockwave that
///                     erases nearby enemy bullets and hits nearby enemies.
///   Perfect switch  – switching while an enemy bullet is about to hit the
///                     current character: slow-mo, the outgoing character is
///                     protected, longer i-frames for the incoming one and
///                     part of the switch cooldown back.
///   Fresh-in buff   – the incoming character gets a short ATK / fire-rate
///                     boost, so rotating the team beats camping one unit.
///   Counter tag-in  – if the incoming character's hierarchy type counters
///                     the enemies on screen, burst and buff are stronger.
///
/// Forced swaps (a teammate fell) get none of this. Added automatically by
/// CharacterSwitcher; every number is tunable here in the Inspector.
/// </summary>
public class SwitchRewards : MonoBehaviour
{
    [Header("Tag-in Burst")]
    [SerializeField] private bool burstEnabled = true;

    [SerializeField, Min(0f), Tooltip("Burst radius around the incoming character (world units).")]
    private float burstRadius = 2.6f;

    [SerializeField, Min(0f), Tooltip("Burst damage as a multiple of the incoming character's ATK.")]
    private float burstAtkMultiplier = 1.5f;

    [SerializeField, Tooltip("Erase enemy bullets inside the burst.")]
    private bool burstClearsBullets = true;

    [Header("Perfect Switch")]
    [SerializeField] private bool perfectEnabled = true;

    [SerializeField, Min(0f), Tooltip("An enemy bullet this close to the active character (beyond its own size) when switching makes it perfect.")]
    private float perfectRadius = 0.7f;

    [SerializeField, Min(0f), Tooltip("Protection for the outgoing character, so the dodge actually dodges.")]
    private float perfectOutgoingInvulnerability = 0.8f;

    [SerializeField, Min(0f), Tooltip("I-frames for the incoming character after a perfect switch.")]
    private float perfectIncomingInvulnerability = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("Share of the switch cooldown refunded.")]
    private float perfectCooldownRefund = 0.4f;

    [SerializeField, Min(0f)] private float perfectSlowMoDuration = 0.45f;
    [SerializeField, Range(0.05f, 1f)] private float perfectSlowMoScale = 0.25f;

    [SerializeField, Min(1f), Tooltip("Burst damage / radius multiplier on a perfect switch.")]
    private float perfectBurstMultiplier = 1.3f;

    [Header("Fresh-in Buff")]
    [SerializeField] private bool buffEnabled = true;

    [SerializeField, Min(0f)] private float buffDuration = 3f;

    [SerializeField, Min(0f), Tooltip("+ATK as a fraction of base ATK (0.25 = +25%).")]
    private float buffAttack = 0.25f;

    [SerializeField, Min(0f), Tooltip("+fire rate (0.2 = 20% faster).")]
    private float buffFireRate = 0.2f;

    [Header("Counter Tag-in")]
    [SerializeField] private bool counterEnabled = true;

    [SerializeField, Range(0f, 1f), Tooltip("Share of on-screen enemies the incoming type must counter.")]
    private float counterShare = 0.5f;

    [SerializeField, Min(1f), Tooltip("Burst damage and buff strength multiplier on a counter tag-in.")]
    private float counterMultiplier = 1.5f;

    private static readonly Color BurstColor = new Color(0.098f, 0.941f, 1f);
    private static readonly Color PerfectColor = new Color(1f, 0.85f, 0.25f);
    private static readonly Color CounterColor = new Color(1f, 0.35f, 0.45f);

    private CharacterSwitcher switcher;
    private bool pendingPerfect;
    private Character buffed;
    private Coroutine buffRoutine;
    private VfxRing buffAura;

    /// <summary>True when the switch in progress was started as a perfect switch.</summary>
    public bool PendingPerfect => pendingPerfect;

    /// <summary>Total perfect switches this run (for tests / stats).</summary>
    public int PerfectCount { get; private set; }

    public int CounterCount { get; private set; }

    private void Awake()
    {
        switcher = GetComponent<CharacterSwitcher>();
    }

    // ---- hooks (called by CharacterSwitcher) ------------------------------

    /// <summary>A manual switch was accepted; <paramref name="outgoing"/> is still on the field.</summary>
    public void OnManualSwitchStarted(Character outgoing)
    {
        pendingPerfect = false;
        EndBuff();

        if (!perfectEnabled || outgoing == null || !IsBulletAbout(outgoing))
            return;

        pendingPerfect = true;
        PerfectCount++;

        Vector3 pos = outgoing.transform.position;

        Health health = outgoing.GetComponent<Health>();
        if (health != null)
            health.SetInvulnerable(perfectOutgoingInvulnerability);

        CombatFeel.SlowMo(perfectSlowMoDuration, perfectSlowMoScale, letterbox: false);
        CombatFeel.Flash(PerfectColor.WithAlpha(0.25f), 0.2f);
        SkillVFX.Ghost(outgoing.transform, PerfectColor.WithAlpha(0.8f), 0.35f, 1.8f);
        SkillVFX.Ring(pos, PerfectColor, 0.2f, 2.4f, 0.3f, 0.1f, 4, 1, 45f);
        SkillVFX.Text(pos + Vector3.up * 1.5f, "PERFECT SWITCH", PerfectColor, 4.5f, 1f, 1f);
    }

    /// <summary>The manual switch finished; <paramref name="incoming"/> is now on the field.</summary>
    public void OnManualSwitchCompleted(Character incoming)
    {
        bool perfect = pendingPerfect;
        pendingPerfect = false;

        if (incoming == null)
            return;

        bool counter = counterEnabled && CountersField(incoming);
        if (counter)
            CounterCount++;

        Vector3 pos = incoming.transform.position;
        float power = (perfect ? perfectBurstMultiplier : 1f) * (counter ? counterMultiplier : 1f);

        if (perfect)
        {
            Health health = incoming.GetComponent<Health>();
            if (health != null)
                health.SetInvulnerable(perfectIncomingInvulnerability);

            if (switcher != null)
                switcher.RefundCooldown(perfectCooldownRefund);
        }

        if (burstEnabled)
            Burst(incoming, pos, power, perfect, counter);

        if (buffEnabled)
            StartBuff(incoming, counter ? counterMultiplier : 1f);

        if (counter)
            SkillVFX.Text(pos + Vector3.up * 1.9f, "COUNTER!", CounterColor, 4.5f, 1f, 1f);
    }

    /// <summary>A forced swap (teammate down) or reset: no rewards, end any buff.</summary>
    public void OnForcedSwap()
    {
        pendingPerfect = false;
        EndBuff();
    }

    // ---- perfect switch ---------------------------------------------------

    private bool IsBulletAbout(Character character)
    {
        Vector2 center = character.transform.position;
        float reach = SkillVFX.GetVisualRadius(character.transform) * 0.6f + perfectRadius;
        float sqr = reach * reach;

        foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            if (projectile == null || !projectile.isActiveAndEnabled || !projectile.IsHostileToPlayer)
                continue;

            if (((Vector2)projectile.transform.position - center).sqrMagnitude <= sqr)
                return true;
        }

        return false;
    }

    // ---- counter tag-in ---------------------------------------------------

    /// <summary>Whether the character's type counters enough of the enemies on screen.</summary>
    public bool CountersField(Character character)
    {
        if (character == null)
            return false;

        int total = 0;
        int countered = 0;

        foreach (var health in SkillTargets.AliveEnemies())
        {
            var binder = health.GetComponent<EnemyDataBinder>();
            if (binder == null || binder.Data == null)
                continue;

            total++;

            if (CounterMechanics.GetMultiplier(character.CharacterType, binder.Data.characterType) > CounterMechanics.NeutralMultiplier)
                countered++;
        }

        return total > 0 && countered >= Mathf.CeilToInt(total * counterShare);
    }

    // ---- burst ------------------------------------------------------------

    private void Burst(Character incoming, Vector3 pos, float power, bool perfect, bool counter)
    {
        float radius = burstRadius * (perfect ? Mathf.Sqrt(perfectBurstMultiplier) : 1f);
        Color color = counter ? CounterColor : perfect ? PerfectColor : BurstColor;

        if (burstClearsBullets)
        {
            float sqr = radius * radius;

            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
            {
                if (projectile == null || !projectile.isActiveAndEnabled || !projectile.IsHostileToPlayer)
                    continue;

                Vector2 p = projectile.transform.position;
                if ((p - (Vector2)pos).sqrMagnitude > sqr)
                    continue;

                SkillVFX.Sparks(p, color, 3, 2.5f, 0.15f, 0.07f);
                projectile.ReturnToPool();
            }
        }

        float damage = incoming.EffectiveAttackPower * burstAtkMultiplier * power;
        int hits = 0;

        foreach (var enemy in SkillTargets.EnemiesInRadius(pos, radius))
        {
            CounterMechanics.Damage(incoming, enemy, damage);
            SkillVFX.Sparks(enemy.transform.position, color, 6, 4f, 0.25f, 0.09f);
            hits++;
        }

        SkillVFX.Ring(pos, color, 0.3f, radius, 0.3f, 0.12f);
        SkillVFX.Ring(pos, Color.white.WithAlpha(0.7f), 0.2f, radius * 0.75f, 0.22f, 0.05f);

        if (hits > 0)
            CombatFeel.HitStop(perfect || counter ? 0.07f : 0.04f);

        CombatFeel.Shake(perfect || counter ? 0.2f : 0.12f, 0.18f);
    }

    // ---- fresh-in buff ----------------------------------------------------

    private void StartBuff(Character incoming, float strength)
    {
        EndBuff();

        buffed = incoming;
        buffed.SetAttackModifier(this, buffed.AttackPower * buffAttack * strength);
        buffed.SetFireIntervalModifier(this, 1f / (1f + buffFireRate * strength));

        Color color = strength > 1f ? CounterColor : BurstColor;
        float radius = SkillVFX.GetVisualRadius(incoming.transform);
        buffAura = SkillVFX.Aura(incoming.transform, color.WithAlpha(0.55f), radius * 1.25f, 0.035f, -1f, 0.05f, 3, 1, 120f);

        buffRoutine = StartCoroutine(BuffTimer());
    }

    private IEnumerator BuffTimer()
    {
        yield return new WaitForSeconds(buffDuration);

        buffRoutine = null;
        EndBuff();
    }

    private void EndBuff()
    {
        if (buffRoutine != null)
        {
            StopCoroutine(buffRoutine);
            buffRoutine = null;
        }

        if (buffed != null)
            buffed.RemoveModifiers(this);

        if (buffAura != null)
            buffAura.Stop();

        buffed = null;
        buffAura = null;
    }

    private void OnDisable()
    {
        EndBuff();
    }
}
