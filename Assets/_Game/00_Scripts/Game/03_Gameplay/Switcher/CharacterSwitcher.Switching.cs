using System.Collections;
using UnityEngine;

// The manual switch: retreat, spread (switching window), merge; and recovering from an interrupted switch.
public partial class CharacterSwitcher
{
    private IEnumerator SwitchRoutine(int targetIndex)
    {
        isSwitching = true;
        RestoreFormationCollider();

        RefreshAutoShoot();
        RefreshButtons();

        onSwitchingWindowChanged?.Invoke(true);

        int fromIndex = activeCharacterIndex;
        int count = characters.Length;

        Vector3 frontLocal = frontPosition.localPosition;
        Vector3[] formationTargets = new Vector3[count];

        formationTargets[targetIndex] = frontLocal;

        int remainingPositionIndex = 0;

        for (int i = 0; i < count; i++)
        {
            if (i == targetIndex)
                continue;

            // Defeated members don't take a slot (a living teammate used to
            // end up on the far slot next to an empty one).
            if (!lifeController.IsAlive(i))
            {
                formationTargets[i] = characters[i].transform.localPosition;
                continue;
            }

            formationTargets[i] =
                remainingPositionIndex == 0
                    ? leftPosition.localPosition
                    : rightPosition.localPosition;

            remainingPositionIndex++;
        }

        for (int i = 0; i < count; i++)
        {
            if (!lifeController.IsAlive(i))
                continue;

            formationTargets[i] =
                ClampChildLocalPosition(i, formationTargets[i]);
        }

        Vector3 retreatPosition =
            ClampChildLocalPosition(
                fromIndex,
                characters[fromIndex].transform.localPosition +
                Vector3.down * retreatDistance);

        Vector3[] retreatTargets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            retreatTargets[i] =
                characters[i].transform.localPosition;

            if (lifeController.IsAlive(i) && i == fromIndex)
                retreatTargets[i] = retreatPosition;
        }

        yield return switchAnimation.Animate(
            characters,
            defeatedCharacters,
            retreatTargets,
            retreatDuration,
            ClampChildLocalPosition);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (!lifeController.IsAlive(i))
            {
                characters[i].SetActive(false);
                continue;
            }

            characters[i].transform.localPosition =
                ClampChildLocalPosition(i, retreatPosition);

            characters[i].SetActive(true);
        }

        RefreshAutoShoot();

        yield return switchAnimation.Animate(
            characters,
            defeatedCharacters,
            formationTargets,
            spreadDuration,
            ClampChildLocalPosition);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < switchingWindowDuration)
        {
            if (!IsSwitchTargetAlive(targetIndex))
            {
                FinishInterruptedSwitch();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 mainPosition = formationTargets[targetIndex];
        Vector3[] mergeTargets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            mergeTargets[i] =
                characters[i].transform.localPosition;

            if (lifeController.IsAlive(i))
                mergeTargets[i] = mainPosition;
        }

        yield return switchAnimation.Animate(
            characters,
            defeatedCharacters,
            mergeTargets,
            mergeDuration,
            ClampChildLocalPosition);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            characters[i].SetActive(
                i == targetIndex &&
                lifeController.IsAlive(i));
        }

        characters[targetIndex].transform.localPosition =
            ClampChildLocalPosition(
                targetIndex,
                frontLocal);

        activeCharacterIndex = targetIndex;
        cooldownTimer = switchCooldown;
        isSwitching = false;
        switchCoroutine = null;

        RefreshAutoShoot();
        RefreshButtons();
        FitColliderToActiveCharacter();
        PlaySwitchInFeel(targetIndex, switchInInvulnerability, forced: false);
        Rewards.OnManualSwitchCompleted(characters[targetIndex].GetComponent<Character>());

        NotifyActiveCharacterChanged();
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        Debug.Log(
            $"[CharacterSwitcher] Switch completed. Active index: {activeCharacterIndex}, Active character: {ActiveCharacter?.name}",
            this);
    }

    private bool IsSwitchTargetAlive(int targetIndex)
    {
        return lifeController != null &&
               lifeController.IsAlive(targetIndex);
    }

    private void FinishInterruptedSwitch()
    {
        switchCoroutine = null;
        isSwitching = false;

        int livingIndex = -1;

        if (lifeController != null &&
            lifeController.IsAlive(activeCharacterIndex))
        {
            livingIndex = activeCharacterIndex;
        }
        else if (lifeController != null)
        {
            livingIndex =
                lifeController.FindNextLivingCharacter(
                    activeCharacterIndex);
        }

        activeCharacterIndex = livingIndex;

        for (int i = 0; i < characters.Length; i++)
        {
            bool shouldBeActive =
                i == livingIndex &&
                lifeController.IsAlive(i);

            characters[i].SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                characters[i].transform.localPosition =
                    ClampChildLocalPosition(
                        i,
                        frontPosition.localPosition);
            }
        }

        RefreshAutoShoot();
        RefreshButtons();

        NotifyActiveCharacterChanged();
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);
        FitColliderToActiveCharacter();
    }
}
