
using System.Collections;
using UnityEngine;

public class CharacterSwitchAnimation : MonoBehaviour
{
    public IEnumerator Animate(
        GameObject[] characters,
        bool[] defeatedCharacters,
        Vector3[] targets,
        float duration,
        System.Func<int, Vector3, Vector3> clampPosition)
    {
        if (characters == null ||
            defeatedCharacters == null ||
            targets == null)
        {
            yield break;
        }

        int count = characters.Length;
        Vector3[] starts = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            if (characters[i] != null)
                starts[i] = characters[i].transform.localPosition;
        }

        if (duration <= 0f)
        {
            ApplyTargets(
                characters,
                defeatedCharacters,
                targets,
                clampPosition);

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < count; i++)
            {
                if (characters[i] == null)
                    continue;

                if (defeatedCharacters[i])
                {
                    characters[i].SetActive(false);
                    continue;
                }

                Vector3 position = Vector3.Lerp(
                    starts[i],
                    targets[i],
                    t);

                characters[i].transform.localPosition =
                    clampPosition(i, position);
            }

            yield return null;
        }

        ApplyTargets(
            characters,
            defeatedCharacters,
            targets,
            clampPosition);
    }

    private void ApplyTargets(
        GameObject[] characters,
        bool[] defeatedCharacters,
        Vector3[] targets,
        System.Func<int, Vector3, Vector3> clampPosition)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            if (defeatedCharacters[i])
            {
                characters[i].SetActive(false);
                continue;
            }

            characters[i].transform.localPosition =
                clampPosition(i, targets[i]);
        }
    }
}
