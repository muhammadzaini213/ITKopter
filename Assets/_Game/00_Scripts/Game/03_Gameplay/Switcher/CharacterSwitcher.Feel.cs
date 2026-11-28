using System.Collections;
using UnityEngine;

// Switch-in feel and i-frame blink, movement-collider fitting, and keeping members inside the play area.
public partial class CharacterSwitcher
{
    private void PlaySwitchInFeel(int index, float invulnerability, bool forced)
    {
        if (index < 0 || index >= characters.Length || characters[index] == null)
            return;

        GameObject character = characters[index];
        Vector3 pos = character.transform.position;
        float radius = SkillVFX.GetVisualRadius(character.transform);
        Color color = forced ? new Color(1f, 0.45f, 0.35f) : new Color(0.098f, 0.941f, 1f);

        SkillVFX.Ghost(character.transform, color.WithAlpha(0.7f), 0.3f, 1.5f);
        SkillVFX.Ring(pos, color, radius * 0.5f, radius * 2.6f, 0.3f, 0.08f);
        SkillVFX.Sparks(pos, color, 10, 4f, 0.3f);

        if (!forced)
            CombatFeel.Shake(0.08f, 0.12f);

        if (invulnerability > 0f && characterHealths[index] != null)
        {
            characterHealths[index].SetInvulnerable(invulnerability);
            StartCoroutine(InvulnerabilityBlink(index, invulnerability));
        }
    }

    private IEnumerator InvulnerabilityBlink(int index, float duration)
    {
        GameObject character = characters[index];
        SpriteRenderer[] sprites = character.GetComponentsInChildren<SpriteRenderer>(true);
        float elapsed = 0f;
        bool dim = false;

        while (elapsed < duration && character != null && character.activeInHierarchy)
        {
            dim = !dim;

            foreach (var sr in sprites)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = dim ? 0.35f : 1f;
                sr.color = c;
            }

            yield return new WaitForSeconds(0.07f);
            elapsed += 0.07f;
        }

        foreach (var sr in sprites)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    /// <summary>Shrinks the movement collider to the active character (no switch running).</summary>
    private void FitColliderToActiveCharacter()
    {
        if (!formationColliderCaptured || parentCollider == null || isSwitching)
            return;

        if (activeCharacterIndex < 0 || activeCharacterIndex >= characters.Length)
            return;

        BoxCollider2D child = childColliders[activeCharacterIndex];
        Transform childTransform = characters[activeCharacterIndex].transform;

        if (child == null)
            return;

        Vector2 scale = new Vector2(Mathf.Abs(childTransform.localScale.x), Mathf.Abs(childTransform.localScale.y));
        Vector2 size = Vector2.Scale(child.size, scale) + Vector2.one * 0.06f;
        Vector2 center = (Vector2)childTransform.localPosition + Vector2.Scale(child.offset, scale);

        parentCollider.size = size;
        parentCollider.offset = center;
    }

    /// <summary>Back to the full formation size while characters spread out.</summary>
    private void RestoreFormationCollider()
    {
        if (!formationColliderCaptured || parentCollider == null)
            return;

        parentCollider.size = formationColliderSize;
        parentCollider.offset = formationColliderOffset;
    }

    private Vector3 ClampChildLocalPosition(
        int characterIndex,
        Vector3 desiredLocalPosition)
    {
        if (boundaryController == null ||
            characterIndex < 0 ||
            characterIndex >= characters.Length)
        {
            return desiredLocalPosition;
        }

        return boundaryController.ClampChildLocalPosition(
            childColliders[characterIndex],
            characters[characterIndex].transform,
            desiredLocalPosition);
    }
}
