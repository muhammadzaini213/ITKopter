using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// First login: asks the player to enter their name (max <see cref="PlayerSystem.MaxNameLength"/>
/// characters) before they can use the main menu. Shown only while
/// <see cref="PlayerSystem.NeedsNameEntry"/> is true.
/// </summary>
public class FirstLoginNamePopupUI : MonoBehaviour
{
    [Header("Popup")]
    [Tooltip("Full-screen root (dim backdrop + panel). Blocks the menu while open.")]
    [SerializeField] private GameObject popupRoot;

    [Header("Input")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private Button confirmButton;

    [Header("Colors")]
    [SerializeField] private Color counterColor = new Color(0.55f, 0.95f, 1f, 1f);
    [SerializeField] private Color counterFullColor = new Color(1f, 0.82f, 0.25f, 1f);

    private PlayerSystem playerSystem;

    private void Awake()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (nameInput != null)
        {
            nameInput.characterLimit = PlayerSystem.MaxNameLength;
            nameInput.lineType = TMP_InputField.LineType.SingleLine;
            nameInput.richText = false;
            nameInput.onValidateInput += ValidateChar;
            nameInput.onValueChanged.AddListener(HandleValueChanged);
            nameInput.onSubmit.AddListener(HandleSubmit);
        }

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
    }

    private void Start()
    {
        playerSystem = PlayerSystem.Instance;

        if (playerSystem == null)
        {
            Debug.LogError("[FirstLoginNamePopupUI] PlayerSystem is not available.", this);
            return;
        }

        playerSystem.OnPlayerDataReady += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (playerSystem != null)
            playerSystem.OnPlayerDataReady -= Refresh;

        if (nameInput != null)
        {
            nameInput.onValidateInput -= ValidateChar;
            nameInput.onValueChanged.RemoveListener(HandleValueChanged);
            nameInput.onSubmit.RemoveListener(HandleSubmit);
        }

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
    }

    private void Refresh()
    {
        if (playerSystem == null || !playerSystem.NeedsNameEntry)
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);
            return;
        }

        Open();
    }

    private void Open()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (nameInput != null)
        {
            nameInput.text = string.Empty;
            nameInput.ActivateInputField();
        }

        HandleValueChanged(string.Empty);
    }

    // Letters, digits, space and a few name-friendly symbols; no control chars or markup.
    private static char ValidateChar(string text, int index, char c)
    {
        if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
            return c;

        // No leading or double spaces.
        if (c == ' ' && index > 0 && text.Length > 0 && text[Mathf.Min(index, text.Length) - 1] != ' ')
            return c;

        return '\0';
    }

    private void HandleValueChanged(string value)
    {
        // characterLimit only guards typing; also clamp text set from code.
        if (nameInput != null && value != null && value.Length > PlayerSystem.MaxNameLength)
        {
            nameInput.SetTextWithoutNotify(value.Substring(0, PlayerSystem.MaxNameLength));
            value = nameInput.text;
        }

        int length = string.IsNullOrEmpty(value) ? 0 : value.Length;

        if (counterText != null)
        {
            counterText.text = $"{length}/{PlayerSystem.MaxNameLength}";
            counterText.color = length >= PlayerSystem.MaxNameLength ? counterFullColor : counterColor;
        }

        if (confirmButton != null)
            confirmButton.interactable = !string.IsNullOrWhiteSpace(value);
    }

    private void HandleSubmit(string value)
    {
        Confirm();
    }

    private void Confirm()
    {
        if (playerSystem == null || nameInput == null)
            return;

        string value = nameInput.text;

        if (string.IsNullOrWhiteSpace(value))
            return;

        playerSystem.SetPlayerName(value);

        if (!playerSystem.NeedsNameEntry && popupRoot != null)
            popupRoot.SetActive(false);
    }
}
