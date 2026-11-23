using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Slafurry.Systems.Pause;

public class TradyExchangeChoiceUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject popupRoot;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button defenseButton;
    [SerializeField] private Button attackButton;

    [Header("Settings")]
    [SerializeField] private string pauseKey = "Dialog";
    [SerializeField, Min(0f)] private float autoCloseTimeout = 10f;

    private Character owner;
    private float duration;
    private Action onDefense;
    private Action onAttack;
    private float timeoutTimer;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (defenseButton != null)
            defenseButton.onClick.AddListener(OnDefenseChosen);

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackChosen);
    }

    private void OnDestroy()
    {
        if (defenseButton != null)
            defenseButton.onClick.RemoveListener(OnDefenseChosen);

        if (attackButton != null)
            attackButton.onClick.RemoveListener(OnAttackChosen);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        // The popup pauses the game (timeScale 0), so count down in real time.
        timeoutTimer -= Time.unscaledDeltaTime;

        if (timeoutTimer <= 0f)
            Close();
    }

    public void Show(
        Character ownerCharacter,
        float durationSeconds,
        Action onDefenseChosen,
        Action onAttackChosen)
    {
        if (isOpen)
            return;

        owner = ownerCharacter;
        duration = durationSeconds;
        onDefense = onDefenseChosen;
        onAttack = onAttackChosen;
        timeoutTimer = autoCloseTimeout;
        isOpen = true;

        Pause.On(pauseKey);

        if (titleText != null)
            titleText.text = owner != null ? owner.CharacterName : "Trady";

        if (descriptionText != null)
            descriptionText.text =
                "Fair Exchange: choose one trade.\n" +
                "Defense: weaken your attack to greatly boost your defense.\n" +
                "Attack: weaken your defense to massively boost your attack.";

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        owner = null;
        onDefense = null;
        onAttack = null;

        Pause.Off(pauseKey);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDefenseChosen()
    {
        onDefense?.Invoke();
        Close();
    }

    private void OnAttackChosen()
    {
        onAttack?.Invoke();
        Close();
    }
}
