
using System.Collections;
using System.Collections.Generic;
using Slafurry.Systems.Audio;
using UnityEngine;
using UnityEngine.UI;

public class RewardPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button okButton;

    [Header("Reward List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private RewardItemUI rewardItemPrefab;

    [Header("Reveal (items punch in one by one)")]
    [SerializeField, Min(0f)] private float punchInterval = 0.12f;
    [SerializeField, Min(0.05f)] private float punchDuration = 0.25f;
    [SerializeField, Range(1f, 1.6f)] private float punchPeak = 1.2f;
    [Tooltip("Optional sound per item.")]
    [SerializeField] private SFXReference punchSfx;

    [Header("Player EXP")]
    [Tooltip("Profile EXP bar shown after a level clear (hidden when there is no EXP gain).")]
    [SerializeField] private ExpGainBarUI expGainBar;

    private readonly List<RewardItemUI> spawnedItems = new();
    private Coroutine revealRoutine;

    private void Awake()
    {
        if (okButton != null)
            okButton.onClick.AddListener(Close);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (okButton != null)
            okButton.onClick.RemoveListener(Close);
    }

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    public void ShowRewards(IReadOnlyList<RewardData> rewards)
    {
        ShowRewards(rewards, null);
    }

    /// <summary>
    /// Shows rewards with explicit amounts (amounts[i] belongs to rewards[i]).
    /// Same rewards are merged. With amounts == null each entry counts as 1.
    /// </summary>
    public void ShowRewards(IReadOnlyList<RewardData> rewards, IReadOnlyList<int> rewardAmounts)
    {
        if (popupRoot == null ||
            contentRoot == null ||
            rewardItemPrefab == null)
        {
            Debug.LogError(
                "[RewardPopupUI] Required references are missing.");
            return;
        }

        ClearRewards();

        bool hasExp = expGainBar != null &&
                      PlayerSystem.Instance != null &&
                      PlayerSystem.Instance.HasPendingExpGain;

        if ((rewards == null || rewards.Count == 0) && !hasExp)
            return;

        rewards ??= System.Array.Empty<RewardData>();

        Dictionary<RewardData, int> amounts = new();
        List<RewardData> orderedRewards = new();

        for (int i = 0; i < rewards.Count; i++)
        {
            RewardData reward = rewards[i];

            if (reward == null)
                continue;

            int amount = rewardAmounts != null && i < rewardAmounts.Count
                ? Mathf.Max(1, rewardAmounts[i])
                : 1;

            if (amounts.ContainsKey(reward))
            {
                amounts[reward] += amount;
            }
            else
            {
                amounts.Add(reward, amount);
                orderedRewards.Add(reward);
            }
        }

        foreach (RewardData reward in orderedRewards)
        {
            RewardItemUI item = Instantiate(
                rewardItemPrefab,
                contentRoot);

            item.Setup(reward, amounts[reward]);
            item.transform.localScale = Vector3.zero;
            spawnedItems.Add(item);
        }

        popupRoot.SetActive(true);

        if (expGainBar != null)
            expGainBar.PlayPendingGain();

        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(RevealItems());
    }

    // Scale-punch each item in turn (unscaled time, so it works while paused).
    private IEnumerator RevealItems()
    {
        foreach (RewardItemUI item in spawnedItems)
        {
            if (item == null)
                continue;

            if (punchSfx != null)
                Audio.PlaySFX2D(punchSfx);

            StartCoroutine(Punch(item.transform));

            float wait = 0f;

            while (wait < punchInterval)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        revealRoutine = null;
    }

    private IEnumerator Punch(Transform target)
    {
        float elapsed = 0f;

        while (elapsed < punchDuration && target != null)
        {
            float t = elapsed / punchDuration;

            // 0 -> peak over the first 60%, peak -> 1 over the rest.
            float scale = t < 0.6f
                ? Mathf.Lerp(0f, punchPeak, t / 0.6f)
                : Mathf.Lerp(punchPeak, 1f, (t - 0.6f) / 0.4f);

            target.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (target != null)
            target.localScale = Vector3.one;
    }

    public void Close()
    {
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);

        ClearRewards();
    }

    private void ClearRewards()
    {
        foreach (RewardItemUI item in spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }
}
