
using System;
using UnityEngine;
using Slafurry.Utils.Pooling;

public class Projectile : MonoBehaviour, IPoolable
{
    public static event Action<Character, Health, float, bool> OnHit;

    private Character ownerCharacter;
    [Header("Projectile Settings")]
    [SerializeField, Min(0.01f)]
    private float speed = 15f;

    [SerializeField, Min(0.01f)]
    private float damage = 10f;

    [SerializeField]
    private Vector2 hitboxSize = new Vector2(0.2f, 0.4f);

    [SerializeField, Min(0.1f)]
    private float lifetime = 5f;

    [Header("Hit VFX")]
    [SerializeField]
    private GameObject hitVfxPrefab;

    private LayerMask hitMask;
    private Action<Projectile> returnToPool;

    private float lifetimeTimer;
    private bool isInitialized;
    private bool hasHit;

    public void SetReturnCallback(Action<Projectile> callback)
    {
        returnToPool = callback;
    }

    public void SetOwner(Character owner)
    {
        ownerCharacter = owner;
    }

    public void Initialize(
        Vector3 position,
        Quaternion rotation,
        float projectileSpeed,
        float projectileDamage,
        LayerMask targetMask)
    {
        transform.SetPositionAndRotation(position, rotation);

        speed = Mathf.Max(0.01f, projectileSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        hitMask = targetMask;

        lifetimeTimer = 0f;
        hasHit = false;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || hasHit)
            return;

        lifetimeTimer += Time.deltaTime;

        if (lifetimeTimer >= lifetime)
        {
            ReturnToPool();
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized || hasHit)
            return;

        float distance = speed * Time.fixedDeltaTime;

        if (distance <= 0f)
            return;

        Vector2 direction = GetMovementDirection();
        float angle = transform.eulerAngles.z;

        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            hitboxSize,
            angle,
            direction,
            distance,
            hitMask
        );

        if (hit.collider != null)
        {
            hasHit = true;

            Health targetHealth =
                hit.collider.GetComponentInParent<Health>();
            Character targetCharacter =
                targetHealth != null ? targetHealth.GetComponent<Character>() : null;

            bool wasAliveBefore = targetHealth != null && !targetHealth.IsDead;

            IDamageable damageable =
                hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
                damageable.TakeDamage(damage);

            bool wasKilled = wasAliveBefore &&
                targetHealth != null && targetHealth.IsDead;

            OnHit?.Invoke(ownerCharacter, targetHealth, damage, wasKilled);

            if (wasKilled && targetCharacter != null && ownerCharacter != null)
                CombatEventBus.InvokeEnemyDiedMarked(ownerCharacter, targetCharacter);

            if (hitVfxPrefab != null)
                Instantiate(hitVfxPrefab, hit.point, transform.rotation);

            transform.position = hit.centroid;
            ReturnToPool();
            return;
        }

        transform.position += (Vector3)(direction * distance);
    }

    protected virtual Vector2 GetMovementDirection() => transform.up;

    public void ReturnToPool()
    {
        if (hasHit && !isInitialized)
            return;

        isInitialized = false;

        if (returnToPool != null)
        {
            returnToPool.Invoke(this);
        }
        else
        {
            Debug.LogWarning(
                "[Projectile] Return callback is missing. Disabling projectile.",
                this
            );

            gameObject.SetActive(false);
        }
    }

    public void OnSpawn()
    {
        lifetimeTimer = 0f;
        hasHit = false;
        isInitialized = false;
    }

    public void OnDespawn()
    {
        isInitialized = false;
        hasHit = false;
        lifetimeTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}
