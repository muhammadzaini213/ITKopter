
using UnityEngine;
using TMPro;

public class CurrencyComponentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI energyText;

    private CurrencySystem currencySystem;
    private EnergySystem energySystem;

    private void Start()
    {
        currencySystem = CurrencySystem.Instance;
        energySystem = EnergySystem.Instance;

        if (currencySystem == null || energySystem == null)
        {
            Debug.LogError(
                "[CurrencyComponentUI] CurrencySystem atau EnergySystem belum tersedia."
            );
            enabled = false;
            return;
        }

        currencySystem.OnCoinsChanged += UpdateCoins;
        currencySystem.OnGemsChanged += UpdateGems;
        energySystem.OnEnergyChanged += UpdateEnergy;

        // Sinkronisasi nilai awal setelah subscribe.
        UpdateCoins(currencySystem.Coins);
        UpdateGems(currencySystem.Gems);
        UpdateEnergy(energySystem.Energy);
    }

    private void UpdateCoins(int value)
    {
        if (coinsText != null)
            coinsText.text = $"Coins: {value}";
    }

    private void UpdateGems(int value)
    {
        if (gemsText != null)
            gemsText.text = $"Gems: {value}";
    }

    private void UpdateEnergy(int value)
    {
        if (energyText != null && energySystem != null)
            energyText.text = $"Energy: {value}/{energySystem.MaxEnergy}";
    }

    private void OnDestroy()
    {
        if (currencySystem != null)
        {
            currencySystem.OnCoinsChanged -= UpdateCoins;
            currencySystem.OnGemsChanged -= UpdateGems;
        }

        if (energySystem != null)
            energySystem.OnEnergyChanged -= UpdateEnergy;
    }
}
