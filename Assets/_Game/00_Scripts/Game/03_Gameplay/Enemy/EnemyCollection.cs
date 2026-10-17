
using UnityEngine;

public class EnemyCollection : MonoBehaviour
{
    private Health[] enemyHealths;

    public bool IsCleared { get; private set; }

    private void Awake()
    {
        enemyHealths = GetComponentsInChildren<Health>(true);

        if (enemyHealths.Length == 0)
        {
            Debug.LogError(
                "[EnemyCollection] No enemy Health components found.",
                this);
        }
    }

    private void Update()
    {
        if (IsCleared || enemyHealths == null ||
            enemyHealths.Length == 0)
        {
            return;
        }

        foreach (Health enemyHealth in enemyHealths)
        {
            // Destroyed enemies no longer block wave completion.
            if (enemyHealth == null)
                continue;

            if (!enemyHealth.IsDead)
                return;
        }

        IsCleared = true;

        Debug.Log(
            $"[EnemyCollection] Collection '{name}' cleared.",
            this);
    }
}
