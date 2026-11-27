
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Slafurry.Systems.Audio;

public class CurrencyComponentUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI gemsText;
    [SerializeField] private TextMeshProUGUI energyText;

    [Header("Energy Recovery Popup")]
    [SerializeField, Tooltip("Tap target that opens the energy recovery popup. Empty = the energy bar (energy text's parent) becomes the button.")]
    private Button energyButton;

    [SerializeField] private SFXReference energyClickSfx;

    [SerializeField, Tooltip("Slider asset for the popup's energy bar (the profile EXP slider, from PlayerExpBar.prefab).")]
    private Slider energyBarTemplate;

    private CurrencySystem currencySystem;
    private EnergySystem energySystem;

    private void Start()
    {
        currencySystem = CurrencySystem.Instance;
        energySystem = EnergySystem.Instance;

        if (currencySystem == null || energySystem == null)
        {
            Debug.LogError(
                "[CurrencyComponentUI] CurrencySystem or EnergySystem is not available yet."
            );
            enabled = false;
            return;
        }

        SetupEnergyButton();

        currencySystem.OnCoinsChanged += UpdateCoins;
        currencySystem.OnGemsChanged += UpdateGems;
        energySystem.OnEnergyChanged += UpdateEnergy;

        // Sync the initial values after subscribing.
        UpdateCoins(currencySystem.Coins);
        UpdateGems(currencySystem.Gems);
        UpdateEnergy(energySystem.Energy);
    }

    private void SetupEnergyButton()
    {
        if (energyButton == null && energyText != null)
        {
            // Prefer the bar (parent) so the whole bar is tappable.
            GameObject target = energyText.transform.parent != null &&
                                energyText.transform.parent.GetComponent<Graphic>() != null
                ? energyText.transform.parent.gameObject
                : energyText.gameObject;

            energyButton = target.GetComponent<Button>();

            if (energyButton == null)
            {
                energyButton = target.AddComponent<Button>();
                energyButton.transition = Selectable.Transition.ColorTint;
            }

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                energyButton.targetGraphic = graphic;
            }
        }

        if (energyButton != null)
            energyButton.onClick.AddListener(OpenEnergyPopup);
    }

    private void OpenEnergyPopup()
    {
        EnergyRecoveryPopupUI.Show(energyClickSfx, energyBarTemplate);
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
        if (energyButton != null)
            energyButton.onClick.RemoveListener(OpenEnergyPopup);

        if (currencySystem != null)
        {
            currencySystem.OnCoinsChanged -= UpdateCoins;
            currencySystem.OnGemsChanged -= UpdateGems;
        }

        if (energySystem != null)
            energySystem.OnEnergyChanged -= UpdateEnergy;
    }
}
