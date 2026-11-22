using UnityEngine;

public class HomingProjectile : Projectile
{
    [Header("Homing Settings")]
    [SerializeField, Min(0f)]
    private float turnSpeed = 720f;

    [SerializeField, Min(0f)]
    private float seekRange = 12f;

    [SerializeField]
    private LayerMask seekMask;

    private Transform target;
    private float seekTimer;

    // Pool reuse: drop the previous life's target so the first seek scan
    // runs immediately instead of aiming at a stale transform.
    public override void OnSpawn()
    {
        base.OnSpawn();
        target = null;
        seekTimer = 0f;
    }

    protected override Vector2 GetMovementDirection()
    {
        RefreshTarget();

        if (target == null)
            return transform.up;

        Vector2 desired = ((Vector2)target.position - (Vector2)transform.position).normalized;
        Vector2 current = transform.up;

        float maxStep = turnSpeed * Time.fixedDeltaTime;
        float currentAngle = Mathf.Atan2(current.y, current.x) * Mathf.Rad2Deg;
        float desiredAngle = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
        float nextAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, maxStep);
        Vector2 next = new Vector2(Mathf.Cos(nextAngle * Mathf.Deg2Rad), Mathf.Sin(nextAngle * Mathf.Deg2Rad));

        float angle = Mathf.Atan2(next.y, next.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        return next;
    }

    private void RefreshTarget()
    {
        seekTimer -= Time.fixedDeltaTime;

        if (seekTimer > 0f)
            return;

        seekTimer = 0.2f;

        Transform best = null;
        float bestDistSqr = seekRange * seekRange;

        var hits = Physics2D.OverlapCircleAll(transform.position, seekRange, seekMask);
        foreach (var hit in hits)
        {
            if (hit.transform == null || hit.transform == transform)
                continue;

            float distSqr = (hit.transform.position - transform.position).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                best = hit.transform;
            }
        }

        target = best;
    }
}
