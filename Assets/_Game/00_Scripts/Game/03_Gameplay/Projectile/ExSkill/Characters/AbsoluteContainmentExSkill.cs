using System.Collections;
using UnityEngine;

public class AbsoluteContainmentExSkill : ExSkill
{
    [Header("Barrier Settings")]
    [SerializeField, Min(0f)]
    private float barrierDuration = 5f;

    [SerializeField, Min(0f)]
    private float barrierHitpointsMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float barrierRadius = 3f;

    [SerializeField]
    private LayerMask enemyProjectileMask;

    private Shield barrierShield;
    private Coroutine durationRoutine;

    public bool IsActive => barrierShield != null && barrierShield.Current > 0f;

    public override void Activate()
    {
        if (!CanUse()) return;

        CreateBarrier();
        StartDuration();
    }

    private void CreateBarrier()
    {
        var shieldGo = new GameObject("AbsoluteContainmentBarrier");
        shieldGo.transform.position = Owner.transform.position;

        var circle = shieldGo.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = barrierRadius;

        barrierShield = shieldGo.AddComponent<Shield>();
        float hp = Owner.EffectiveDefensePower * barrierHitpointsMultiplier;
        barrierShield.Set(Mathf.Max(1f, hp));

        barrierShield.OnBroken += OnBarrierBroken;

        var ownerHealth = Owner.GetComponent<Health>();
        if (ownerHealth != null)
            ownerHealth.AssignShield(barrierShield);
    }

    private void StartDuration()
    {
        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DurationRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(barrierDuration);
        DestroyBarrier();
    }

    private void OnBarrierBroken()
    {
        DestroyBarrier();
    }

    private void DestroyBarrier()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        if (barrierShield != null)
        {
            barrierShield.OnBroken -= OnBarrierBroken;

            var ownerHealth = Owner.GetComponent<Health>();
            if (ownerHealth != null && ownerHealth.Shield == barrierShield)
                ownerHealth.RemoveShield();

            if (barrierShield.gameObject != null)
                Destroy(barrierShield.gameObject);

            barrierShield = null;
        }
    }

    private void Update()
    {
        if (barrierShield != null && barrierShield.gameObject != null)
            barrierShield.gameObject.transform.position = Owner.transform.position;
    }

    public override void Cancel()
    {
        DestroyBarrier();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
