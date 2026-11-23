using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaComponentUI : MonoBehaviour
{
    private const int BannerCount = 3;

    [System.Serializable]
    public class GachaBannerSlot
    {
        public GachaData gachaData;
        public Button selectButton;
        [Tooltip("Shows the featured character's icon (the _1 face sprite).")]
        public Image iconImage;
        [Tooltip("Shows the banner name.")]
        public TMP_Text nameText;
    }

    [Header("Gacha Banners")]
    [SerializeField] private GachaBannerSlot[] banners =
        new GachaBannerSlot[BannerCount];

    [Header("Gacha UI")]
    [SerializeField] private TextMeshProUGUI gachaNameText;
    [Tooltip("Banner description (GachaData.Description), shown under the name.")]
    [SerializeField] private TMP_Text gachaDescriptionText;
    [SerializeField] private TextMeshProUGUI singlePullPriceText;
    [SerializeField] private TextMeshProUGUI tenPullPriceText;
    private TextMeshProUGUI gemsText;

    [Header("Featured Character")]
    [Tooltip("Shows the selected banner's featured (rate-up) character.")]
    [SerializeField] private Image featuredCharacterImage;
    [Tooltip("Frame drawn over the character; hidden when the banner has no featured character.")]
    [SerializeField] private GameObject featuredCharacterBorder;

    [Header("Pull Buttons")]
    [SerializeField] private Button singlePullButton;
    [SerializeField] private Button tenPullButton;

    [Header("Rates")]
    [SerializeField] private Button ratesButton;
    [SerializeField] private GachaRatesPopupUI ratesPopup;

    [Header("Reveal")]
    [Tooltip("Pull reveal overlay. Leave empty to show the reward popup straight away.")]
    [SerializeField] private GachaRevealUI revealOverlay;

    [Header("Confirmation")]
    [Tooltip("Asks before spending gems. Leave empty to pull immediately.")]
    [SerializeField] private GachaConfirmationPopupUI confirmationPopup;

    private GachaSystem gachaSystem;
    private CurrencySystem currencySystem;

    private GachaData currentGacha;

    public RewardData CurrentFeaturedReward =>
        currentGacha != null ? currentGacha.FeaturedReward : null;
    private int currentBannerIndex = -1;
    private int lastKnownGems = int.MinValue;

    private void Start()
    {
        gachaSystem = GachaSystem.Instance;
        currencySystem = CurrencySystem.Instance;

        if (gachaSystem == null)
        {
            Debug.LogError(
                "[GachaUI] GachaSystem is not available!"
            );

            SetPullButtonsInteractable(false);
            return;
        }

        if (currencySystem == null)
        {
            Debug.LogError(
                "[GachaUI] CurrencySystem is not available!"
            );

            SetPullButtonsInteractable(false);
            return;
        }

        RegisterBannerButtons();
        RefreshBannerButtons();

        if (ratesButton != null)
            ratesButton.onClick.AddListener(OpenRates);

        if (singlePullButton != null)
        {
            singlePullButton.onClick.AddListener(
                OnSinglePullClicked
            );
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.AddListener(
                OnTenPullClicked
            );
        }

        SelectInitialBanner();
    }

    private void Update()
    {
        if (currencySystem == null || currentGacha == null)
            return;

        if (currencySystem.Gems != lastKnownGems)
            RefreshCurrencyUI();
    }

    private void RegisterBannerButtons()
    {
        if (banners == null)
            return;

        for (int i = 0; i < banners.Length; i++)
        {
            int index = i;

            if (banners[i] == null)
                continue;

            if (banners[i].selectButton != null)
            {
                banners[i].selectButton.onClick.AddListener(
                    () => SelectBanner(index)
                );
            }
        }
    }

    // Label each banner button with its name and featured character icon.
    private void RefreshBannerButtons()
    {
        if (banners == null)
            return;

        foreach (GachaBannerSlot banner in banners)
        {
            if (banner == null)
                continue;

            GachaData data = banner.gachaData;

            if (banner.nameText != null)
                banner.nameText.text = data != null ? data.GachaName : string.Empty;

            if (banner.iconImage != null)
            {
                Sprite icon = data != null && data.FeaturedReward != null
                    ? data.FeaturedReward.Icon
                    : null;

                banner.iconImage.sprite = icon;
                banner.iconImage.preserveAspect = true;
                banner.iconImage.enabled = icon != null;
            }
        }
    }

    private void SelectInitialBanner()
    {
        if (banners == null || banners.Length == 0)
        {
            Debug.LogError(
                "[GachaUI] No gacha banners are configured!"
            );

            SetPullButtonsInteractable(false);
            return;
        }

        for (int i = 0; i < banners.Length; i++)
        {
            if (banners[i] != null &&
                banners[i].gachaData != null)
            {
                SelectBanner(i);
                return;
            }
        }

        Debug.LogError(
            "[GachaUI] No valid GachaData is assigned!"
        );

        SetPullButtonsInteractable(false);
    }

    public void SelectBanner(int index)
    {
        if (banners == null ||
            index < 0 ||
            index >= banners.Length)
        {
            Debug.LogWarning(
                $"[GachaUI] Invalid banner index: {index}"
            );

            return;
        }

        if (banners[index] == null ||
            banners[index].gachaData == null)
        {
            Debug.LogError(
                $"[GachaUI] GachaData is missing for banner {index + 1}!"
            );

            return;
        }

        currentBannerIndex = index;
        currentGacha = banners[index].gachaData;

        Debug.Log(
            $"[GachaUI] Selected banner: {currentGacha.GachaName}"
        );

        RefreshBannerUI();
    }

    private void RefreshBannerUI()
    {
        if (currentGacha == null)
            return;

        if (gachaNameText != null)
            gachaNameText.text = currentGacha.GachaName;

        if (gachaDescriptionText != null)
            gachaDescriptionText.text = currentGacha.Description;

        RefreshFeaturedCharacter();

        if (singlePullPriceText != null)
        {
            singlePullPriceText.text =
                $"{CalculateCost(1)} Gems";
        }

        if (tenPullPriceText != null)
        {
            tenPullPriceText.text =
                $"{CalculateCost(currentGacha.MultiPullCount)} Gems";
        }

        RefreshCurrencyUI();
    }

    public void OpenRates()
    {
        if (ratesPopup != null && currentGacha != null)
            ratesPopup.Open(currentGacha);
    }

    private void RefreshFeaturedCharacter()
    {
        if (featuredCharacterImage == null)
            return;

        RewardData featured = currentGacha != null ? currentGacha.FeaturedReward : null;

        // Characters use their full card art; any other featured reward falls back to its icon.
        Sprite sprite = featured is CharacterData character && character.CharacterPortrait != null
            ? character.CharacterPortrait
            : featured != null ? featured.Icon : null;

        featuredCharacterImage.sprite = sprite;
        featuredCharacterImage.type = Image.Type.Simple;
        featuredCharacterImage.preserveAspect = true;
        featuredCharacterImage.color = Color.white;
        featuredCharacterImage.enabled = sprite != null;

        if (featuredCharacterBorder != null)
            featuredCharacterBorder.SetActive(sprite != null);
    }

    private void RefreshCurrencyUI()
    {
        if (currencySystem == null)
            return;

        lastKnownGems = currencySystem.Gems;

        if (gemsText != null)
            gemsText.text = $"Gems: {lastKnownGems}";

        if (currentGacha == null)
        {
            SetPullButtonsInteractable(false);
            return;
        }

        if (singlePullButton != null)
        {
            singlePullButton.interactable =
                lastKnownGems >= CalculateCost(1);
        }

        if (tenPullButton != null)
        {
            tenPullButton.interactable =
                lastKnownGems >= CalculateCost(
                    currentGacha.MultiPullCount
                );
        }
    }

    private int CalculateCost(int pullCount)
    {
        if (currentGacha == null || pullCount <= 0)
            return 0;

        float discountMultiplier =
            1f - Mathf.Clamp(
                currentGacha.GachaDiscount,
                0f,
                100f
            ) / 100f;

        long rawCost =
            (long)currentGacha.GachaCost * pullCount;

        double discountedCost = System.Math.Round(
            rawCost * discountMultiplier,
            MidpointRounding.AwayFromZero
        );

        if (discountedCost >= int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(0, (int)discountedCost);
    }

    public void OnSinglePullClicked()
    {
        RequestPull(1);
    }

    public void OnTenPullClicked()
    {
        if (currentGacha == null)
            return;

        RequestPull(currentGacha.MultiPullCount);
    }

    private void RequestPull(int pullCount)
    {
        if (currentGacha == null || currencySystem == null)
            return;

        if (confirmationPopup == null)
        {
            PerformPull(pullCount);
            return;
        }

        // Lock in the banner chosen now, even if the player switches banners behind the popup.
        GachaData gacha = currentGacha;

        confirmationPopup.Open(
            gacha,
            pullCount,
            CalculateCost(pullCount),
            currencySystem.Gems,
            () =>
            {
                if (currentGacha != gacha)
                    return;

                PerformPull(pullCount);
            });
    }

    private void PerformPull(int pullCount)
    {
        if (currentGacha == null)
        {
            Debug.LogWarning(
                "[GachaUI] Please select a banner first."
            );

            return;
        }

        if (gachaSystem == null || currencySystem == null)
        {
            Debug.LogError(
                "[GachaUI] A required game system is unavailable!"
            );

            return;
        }

        int cost = CalculateCost(pullCount);

        if (currencySystem.Gems < cost)
        {
            Debug.LogWarning(
                "[GachaUI] Not enough Gems to perform this pull."
            );

            RefreshCurrencyUI();
            return;
        }

        GachaPullResult[] results;

        bool success = gachaSystem.Pull(
            currentGacha,
            pullCount,
            revealOverlay == null,
            out results
        );

        if (!success)
        {
            Debug.LogWarning(
                $"[GachaUI] Pull failed for banner: {currentGacha.GachaName}"
            );

            RefreshCurrencyUI();
            return;
        }

        Debug.Log(
            $"[GachaUI] Pull successful! " +
            $"Banner: {currentGacha.GachaName}, " +
            $"Pull count: {pullCount}"
        );

        LogPullResults(results);

        RefreshCurrencyUI();

        // Reveal one by one, then show the summary popup.
        if (revealOverlay != null)
        {
            GachaPullResult[] pulled = results;
            revealOverlay.Play(pulled, currentGacha.FeaturedReward, () => ShowSummary(pulled));
        }
    }

    private void ShowSummary(GachaPullResult[] results)
    {
        if (RewardSystem.Instance == null || results == null)
            return;

        List<RewardData> rewards = new List<RewardData>(results.Length);
        List<int> amounts = new List<int>(results.Length);

        foreach (GachaPullResult result in results)
        {
            if (result == null || result.Reward == null)
                continue;

            rewards.Add(result.Reward);
            amounts.Add(result.Amount);
        }

        RewardSystem.Instance.ShowRewardPopup(rewards, amounts);
    }

    private void LogPullResults(GachaPullResult[] results)
    {
        if (results == null || results.Length == 0)
        {
            Debug.LogWarning(
                "[GachaUI] No pull results were returned."
            );

            return;
        }

        for (int i = 0; i < results.Length; i++)
        {
            GachaPullResult result = results[i];

            if (result == null || result.Reward == null)
            {
                Debug.LogWarning(
                    $"[GachaUI] Pull result {i + 1} is invalid."
                );

                continue;
            }

            Debug.Log(
                $"[GachaUI] Pull {i + 1}: {result.Reward.name}"
            );
        }
    }

    private void SetPullButtonsInteractable(bool interactable)
    {
        if (singlePullButton != null)
            singlePullButton.interactable = interactable;

        if (tenPullButton != null)
            tenPullButton.interactable = interactable;
    }

    private void OnDestroy()
    {
        if (banners != null)
        {
            foreach (GachaBannerSlot banner in banners)
            {
                if (banner != null && banner.selectButton != null)
                {
                    banner.selectButton.onClick.RemoveAllListeners();
                }
            }
        }

        if (ratesButton != null)
            ratesButton.onClick.RemoveListener(OpenRates);

        if (singlePullButton != null)
        {
            singlePullButton.onClick.RemoveListener(
                OnSinglePullClicked
            );
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.RemoveListener(
                OnTenPullClicked
            );
        }
    }
}
