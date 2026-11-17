using System.Collections.Generic;
using UnityEngine;

public class ProjectileLimiter : MonoBehaviour
{
    [Header("Limit Settings")]
    [SerializeField, Min(1)]
    private int maxEnemyProjectiles = 8;

    [SerializeField, Min(0.1f)]
    private float tickInterval = 0.5f;

    [SerializeField]
    [Tooltip("Layer(s) that identify enemy projectiles.")]
    private LayerMask enemyProjectileLayerMask;

    private float timer;

    public void SetMaxEnemyProjectiles(int max)
    {
        maxEnemyProjectiles = Mathf.Max(1, max);
    }

    public int MaxEnemyProjectiles => maxEnemyProjectiles;

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = tickInterval;

        var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);

        List<Projectile> enemyProjectiles = new List<Projectile>();

        foreach (var projectile in projectiles)
        {
            if (projectile == null)
                continue;

            if (IsEnemyProjectile(projectile))
                enemyProjectiles.Add(projectile);
        }

        while (enemyProjectiles.Count > maxEnemyProjectiles)
        {
            int last = enemyProjectiles.Count - 1;
            var excess = enemyProjectiles[last];

            if (excess != null)
                excess.ReturnToPool();

            enemyProjectiles.RemoveAt(last);
        }
    }

    private bool IsEnemyProjectile(Projectile projectile)
    {
        if (projectile == null)
            return false;

        int layer = projectile.gameObject.layer;
        return ((enemyProjectileLayerMask.value >> layer) & 1) == 1;
    }
}
