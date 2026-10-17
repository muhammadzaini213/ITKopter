
using UnityEngine;

public class PlayerAutoShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BulletSpawner bulletSpawner;

    [Header("Firing Settings")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.2f;

    private Transform firePoint;
    private CharacterSwitcher characterSwitcher;
    private Character character;
    private float fireTimer;
    private bool hasLoggedMissingReference;

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        firePoint = GetComponentInChildren<Firepoint>().transform;
        if (character == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Character component is missing.",
                this);
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Fire Point is not assigned.",
                this);
        }

        if (bulletSpawner == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Bullet Spawner is not assigned.",
                this);
        }

        characterSwitcher = Object.FindFirstObjectByType<CharacterSwitcher>();

        if (characterSwitcher == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] CharacterSwitcher is not found in the scene.",
                this);
        }
    }

    private void Update()
    {
        if (!CanShoot())
        {
            fireTimer = 0f;
            return;
        }

        fireTimer += Time.deltaTime;

        if (fireTimer < fireInterval)
            return;

        fireTimer -= fireInterval;

        Debug.Log(
            $"[AutoShooter] {character.name} firing. " +
            $"Switching: {characterSwitcher.IsSwitching}, " +
            $"Active: {characterSwitcher.ActiveCharacter?.name}",
            this);

        bulletSpawner.Fire(
            firePoint,
            character.BulletSpeed
        );
    }

    private bool CanShoot()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return false;

        if (character == null || firePoint == null || bulletSpawner == null)
            return false;

        if (characterSwitcher == null)
        {
            if (!hasLoggedMissingReference)
            {
                Debug.LogError(
                    "[PlayerAutoShooter] CharacterSwitcher is not assigned.",
                    this);

                hasLoggedMissingReference = true;
            }

            return false;
        }

        // During the switching window, every active character can shoot.
        if (characterSwitcher.IsSwitching)
            return true;

        // Outside the switching window, only the selected character can shoot.
        if (characterSwitcher.ActiveCharacter != character.gameObject)
            return false;

        Health health = GetComponent<Health>();

        if (health != null && health.IsDead)
            return false;

        return true;
    }

    private void OnDisable()
    {
        fireTimer = 0f;
    }
}
