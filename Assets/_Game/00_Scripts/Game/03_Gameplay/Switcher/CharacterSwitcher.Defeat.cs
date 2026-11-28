using UnityEngine;

// A teammate falling (forced swap to the next living member) and revives.
public partial class CharacterSwitcher
{
    private void HandleCharacterDeath(int characterIndex)
    {
        if (!isInitialized || isHandlingDeath)
            return;

        isHandlingDeath = true;

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }

        bool wasSwitching = isSwitching;
        isSwitching = false;

        // An interrupted switch or the on-field character falling: no rewards.
        if (wasSwitching || characterIndex == activeCharacterIndex)
            Rewards.OnForcedSwap();

        // A defeat mid-switch used to leave the spread-out teammates active
        // (visible and hittable but not shooting). Settle the formation.
        if (wasSwitching && characterIndex != activeCharacterIndex &&
            lifeController.IsAlive(activeCharacterIndex))
        {
            for (int i = 0; i < characters.Length; i++)
                characters[i].SetActive(i == activeCharacterIndex);

            characters[activeCharacterIndex].transform.localPosition =
                ClampChildLocalPosition(activeCharacterIndex, frontPosition.localPosition);
        }

        bool forcedSwap = false;

        if (characterIndex == activeCharacterIndex ||
            !lifeController.IsAlive(activeCharacterIndex))
        {
            int nextIndex =
                lifeController.FindNextLivingCharacter(
                    activeCharacterIndex);

            if (nextIndex >= 0)
            {
                activeCharacterIndex = nextIndex;

                characters[nextIndex].transform.localPosition =
                    ClampChildLocalPosition(
                        nextIndex,
                        frontPosition.localPosition);

                for (int i = 0; i < characters.Length; i++)
                    characters[i].SetActive(i == nextIndex);

                cooldownTimer = 0f;
                forcedSwap = true;
            }
            else
            {
                activeCharacterIndex = -1;

                for (int i = 0; i < characters.Length; i++)
                    characters[i].SetActive(false);
            }
        }

        for (int i = 0; i < characters.Length; i++)
        {
            if (!lifeController.IsAlive(i))
                characters[i].SetActive(false);
        }

        RefreshAutoShoot();
        RefreshButtons();

        NotifyActiveCharacterChanged();
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        // Losing a teammate should land hard.
        CombatFeel.HitStop(0.2f, 0.02f);
        CombatFeel.Shake(0.45f, 0.45f);
        CombatFeel.Flash(new Color(1f, 0.1f, 0.1f, 0.35f), 0.35f);

        if (forcedSwap)
            PlaySwitchInFeel(activeCharacterIndex, forcedSwapInvulnerability, forced: true);

        FitColliderToActiveCharacter();
        isHandlingDeath = false;
    }

    private void HandleCharacterRevived(int characterIndex)
    {
        if (!isInitialized)
            return;

        // Revived members stay benched until switched in.
        if (characterIndex != activeCharacterIndex && !isSwitching &&
            characterIndex >= 0 && characterIndex < characters.Length)
        {
            characters[characterIndex].SetActive(false);
        }

        RefreshAutoShoot();
        RefreshButtons();
    }
}
