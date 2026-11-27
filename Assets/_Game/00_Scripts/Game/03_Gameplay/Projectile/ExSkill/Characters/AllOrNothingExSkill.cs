using System.Collections;
using UnityEngine;

// Cashino — All or Nothing.
// A slot-machine roll for the whole party:
//   JACKPOT  — revive one fallen ally + faster fire for everyone
//   LUCKY 7  — big ATK and fire-rate boost for everyone
//   BAD LUCK — everyone fires slower
// Team effect: keeps running after Cashino is switched out.
public class AllOrNothingExSkill : ExSkill
{
    [Header("Jackpot Settings")]
    [SerializeField, Min(0f)]
    private float cooldownReductionMultiplier = 0.5f;

    [Header("Lucky Seven Settings")]
    [SerializeField, Min(0f)]
    private float luckyDamageMultiplier = 2f;

    [SerializeField, Min(0f)]
    private float luckySpeedMultiplier = 2f;

    [Header("Bad Luck Settings")]
    [SerializeField, Min(0f)]
    private float badLuckCooldownPenalty = 3f;

    [Header("Duration")]
    [SerializeField, Min(0f)]
    private float effectDuration = 8f;

    [Header("Roll Presentation")]
    [SerializeField, Min(0f)]
    private float rollDuration = 0.7f;

    private static readonly Color JackpotColor = new Color(1f, 0.85f, 0.2f);
    private static readonly Color LuckyColor = new Color(0.35f, 1f, 0.45f);
    private static readonly Color BadLuckColor = new Color(0.55f, 0.5f, 0.6f);
    private static readonly string[] RollFaces = { "JACKPOT", "LUCKY 7", "BAD LUCK" };

    private Coroutine routine;
    private VfxRing aura;
    private VfxText label;

    protected override Color DefaultColor => JackpotColor;

    protected override string[] DefaultActivationLines => new[] { "All or nothing, let's roll!", "Come on, lucky seven!" };

    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"Spins a slot machine for the whole team ({Seconds(effectDuration)}), each result 1-in-3:\n\n" +
               $"<color=#FFD447>JACKPOT</color>: revives one fallen ally and the team fires {Pct(1f / Mathf.Max(0.05f, cooldownReductionMultiplier))} as fast.\n" +
               $"<color=#6CFF8A>LUCKY 7</color>: +{Pct(luckyDamageMultiplier)} ATK and {Pct(luckySpeedMultiplier)} fire rate for everyone.\n" +
               $"<color=#8FA9B5>BAD LUCK</color>: everyone fires at only {Pct(1f / Mathf.Max(0.05f, badLuckCooldownPenalty))} speed.\n\n" +
               "Keeps working while Cashino is benched.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        routine = StartSkillRoutine(RollRoutine(Random.Range(0, 3)));
    }

    private IEnumerator RollRoutine(int result)
    {
        Transform anchor = TeamAnchor;
        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        Vector3 labelOffset = Vector3.up * (radius + 0.7f);

        // Slot reel: cycle the faces, slowing down, then land on the result.
        label = SkillVFX.Label(anchor, labelOffset, RollFaces[0], Color.white, 4f);

        float elapsed = 0f;
        float tick = 0.05f;
        int face = 0;

        while (elapsed < rollDuration)
        {
            face = (face + 1) % RollFaces.Length;

            if (label != null)
            {
                label.SetColor(ColorFor(face));
                label.SetText(RollFaces[face]);
            }

            SkillVFX.Sparks(anchor.position + labelOffset, ColorFor(face), 3, 2f, 0.2f, 0.08f);

            yield return new WaitForSeconds(tick);

            elapsed += tick;
            tick *= 1.25f;
        }

        if (label != null)
        {
            label.SetColor(ColorFor(result));
            label.SetText(result == 0 ? "JACKPOT!!" : result == 1 ? "LUCKY 7!" : "BAD LUCK...");
        }

        ApplyEffect(result);
        PlayResultFeel(result, anchor, radius);

        float labelTime = Mathf.Min(1.5f, effectDuration);
        yield return new WaitForSeconds(labelTime);

        if (label != null) label.Stop();
        label = null;

        yield return new WaitForSeconds(effectDuration - labelTime);

        RestoreAll();
        routine = null;
    }

    private static Color ColorFor(int face) =>
        face == 0 ? JackpotColor : face == 1 ? LuckyColor : BadLuckColor;

    private void PlayResultFeel(int result, Transform anchor, float radius)
    {
        Color color = ColorFor(result);
        Vector3 pos = anchor.position;

        aura = SkillVFX.Aura(anchor, color, radius * 1.4f, result == 2 ? 0.04f : 0.07f, -1f, 0.06f, result == 2 ? 0 : 7, 3, result == 2 ? 0f : 70f);

        if (result == 2)
        {
            SkillVFX.Sparks(pos, color, 10, 2f, 0.6f);
            CombatFeel.Shake(0.08f, 0.2f);
            return;
        }

        SkillVFX.Ring(pos, color, radius, radius * 5f, 0.5f, 0.2f);
        SkillVFX.Sparks(pos, color, result == 0 ? 45 : 28, 9f, 0.7f);
        SkillVFX.Sparks(pos, Color.white, 15, 6f, 0.5f);
        CombatFeel.Flash(color.WithAlpha(result == 0 ? 0.35f : 0.22f), 0.3f);
        CombatFeel.HitStop(result == 0 ? 0.15f : 0.08f);
        CombatFeel.Shake(result == 0 ? 0.35f : 0.22f, 0.35f);
    }

    private void ApplyEffect(int effectIndex)
    {
        switch (effectIndex)
        {
            case 0: ApplyJackpot(); break;
            case 1: ApplyLuckySeven(); break;
            default: ApplyBadLuck(); break;
        }
    }

    private void ApplyJackpot()
    {
        var lifeController = Object.FindFirstObjectByType<CharacterLifeController>();

        if (lifeController != null)
        {
            for (int i = 0; i < lifeController.CharacterCount; i++)
            {
                if (lifeController.CanRevive(i) && lifeController.Revive(i))
                {
                    var revived = SkillTargets.Party.Count > i ? SkillTargets.Party[i] : null;
                    string name = revived != null ? revived.CharacterName : "ALLY";
                    SkillVFX.Text(TeamAnchor.position + Vector3.up * 2.4f, $"{name} REVIVED!", JackpotColor, 3.5f, 1.4f);
                    break;
                }
            }
        }

        foreach (var member in SkillTargets.Party)
            ApplyFireInterval(member, this, cooldownReductionMultiplier);
    }

    private void ApplyLuckySeven()
    {
        foreach (var member in SkillTargets.Party)
        {
            if (member == null)
                continue;

            ApplyAttack(member, this, member.AttackPower * luckyDamageMultiplier);
            ApplyFireInterval(member, this, 1f / Mathf.Max(0.05f, luckySpeedMultiplier));
        }
    }

    private void ApplyBadLuck()
    {
        foreach (var member in SkillTargets.Party)
            ApplyFireInterval(member, this, badLuckCooldownPenalty);
    }

    private void RestoreAll()
    {
        ClearPartyModifiers(this);

        if (aura != null) aura.Stop();
        if (label != null) label.Stop();

        aura = null;
        label = null;
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref routine);
        RestoreAll();
    }
}
