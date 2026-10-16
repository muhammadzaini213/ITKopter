
using System.Collections.Generic;
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

    private readonly List<RewardItemUI> spawnedItems = new();

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

    public void ShowRewards(IReadOnlyList<RewardData> rewards)
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

        if (rewards == null || rewards.Count == 0)
            return;

        Dictionary<RewardData, int> amounts = new();
        List<RewardData> orderedRewards = new();

        foreach (RewardData reward in rewards)
        {
            if (reward == null)
                continue;

            if (amounts.ContainsKey(reward))
            {
                amounts[reward]++;
            }
            else
            {
                amounts.Add(reward, 1);
                orderedRewards.Add(reward);
            }
        }

        foreach (RewardData reward in orderedRewards)
        {
            RewardItemUI item = Instantiate(
                rewardItemPrefab,
                contentRoot);

            item.Setup(reward, amounts[reward]);
            spawnedItems.Add(item);
        }

        popupRoot.SetActive(true);
    }

    public void Close()
    {
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
