using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Login / account popup, built at runtime with the project's 9-slice frame.
/// Views: email -> 6-digit code -> (cloud vs device choice) and, when logged
/// in, the account view (sync now / log out).
/// </summary>
public class AccountPopupUI : MonoBehaviour
{
    private static AccountPopupUI open;

    private TMP_FontAsset font;
    private Sprite frame;
    private RectTransform panel;
    private RectTransform content;
    private TMP_Text messageText;
    private bool firstLaunch;
    private string email;

    private AccountSystem Account => AccountSystem.Instance;

    public static bool IsOpen => open != null;

    /// <summary>Opens the right view: choice if a conflict is pending, account if logged in, else login.</summary>
    public static AccountPopupUI Show(bool firstLaunch = false)
    {
        if (AccountSystem.Instance == null)
            return null;

        if (open != null)
            Destroy(open.gameObject);

        GameObject root = RuntimeUIKit.NewOverlayCanvas("AccountPopup", 320);
        var ui = root.AddComponent<AccountPopupUI>();
        open = ui;
        ui.firstLaunch = firstLaunch;
        ui.Build();

        AccountSystem account = AccountSystem.Instance;

        if (account.IsLoggedIn && account.PendingConflict != null) ui.ShowChoice();
        else if (account.IsLoggedIn) ui.ShowAccount();
        else ui.ShowEmail();

        return ui;
    }

    private void OnDestroy()
    {
        if (open == this)
            open = null;
    }

    private void Build()
    {
        font = RuntimeUIKit.FindFont();
        frame = RuntimeUIKit.FindFrameSprite();

        var dim = RuntimeUIKit.NewImage("Dim", transform, RuntimeUIKit.Dim, null);
        RuntimeUIKit.Stretch(dim.rectTransform);

        panel = RuntimeUIKit.NewImage("Panel", transform, Color.white, frame).rectTransform;
        panel.sizeDelta = new Vector2(760f, 460f);

        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(panel, false);
        RuntimeUIKit.Stretch(content);

        messageText = RuntimeUIKit.NewText("Message", panel, font, "", 18f, new Color(1f, 0.5f, 0.5f), new Vector2(0f, -122f), new Vector2(700f, 28f));
    }

    private void Clear(string message = "")
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        messageText.text = message;
        messageText.color = new Color(1f, 0.5f, 0.5f);
    }

    private void Info(string message)
    {
        messageText.text = message;
        messageText.color = RuntimeUIKit.SubtleText;
    }

    private void Error(string message)
    {
        messageText.text = message;
        messageText.color = new Color(1f, 0.5f, 0.5f);
    }

    // ---- views --------------------------------------------------------------------

    private void ShowEmail()
    {
        Clear();
        Title("SAVE YOUR PROGRESS");
        Body("Log in with your email to keep your progress safe and continue on any device. We'll email you a 6-digit code: no password needed.");

        TMP_InputField input = NewInput("Email", "you@example.com", TMP_InputField.ContentType.EmailAddress, 64, new Vector2(0f, -10f));
        input.text = email ?? Account.Email ?? string.Empty;

        Button send = Button("SEND CODE", new Vector2(-150f, -180f), new Vector2(240f, 60f));
        Button other = Button(firstLaunch ? "PLAY AS GUEST" : "CANCEL", new Vector2(150f, -180f), new Vector2(240f, 60f));

        send.onClick.AddListener(() =>
        {
            email = input.text.Trim();

            if (email.Length < 5 || !email.Contains("@"))
            {
                Error("Enter your email address.");
                return;
            }

            send.interactable = false;
            Info("Sending code...");

            Account.RequestCode(email, (ok, error, devCode) =>
            {
                if (this == null) return;
                send.interactable = true;

                if (!ok)
                {
                    Error(error);
                    return;
                }

                ShowCode(devCode);
            });
        });

        other.onClick.AddListener(() =>
        {
            if (firstLaunch)
                Account.ChooseGuest();

            Close();
        });

        if (AccountSystem.BrowserClearsSaves)
            AddTemporaryStorageWarning();
        else if (!firstLaunch)
            AddGuestNote();
    }

    private void AddTemporaryStorageWarning()
    {
        RuntimeUIKit.NewText("StorageWarning", content, font,
            "<color=#FFD447>Brave deletes this game's saves when you close itch.io.</color>\n" +
            "<color=#8FA9B5>Log in to keep your progress, or allow cookies for itch.io in Brave Shields.</color>",
            15f, Color.white, new Vector2(0f, -80f), new Vector2(720f, 42f));
    }

    private void AddGuestNote()
    {
        RuntimeUIKit.NewText("GuestNote", content, font,
            "<color=#8FA9B5>Playing as guest: progress is saved only in this browser.</color>",
            16f, Color.white, new Vector2(0f, -84f), new Vector2(700f, 24f));
    }

    private void ShowCode(string devCode)
    {
        Clear();
        Title("ENTER YOUR CODE");
        Body($"We sent a 6-digit code to <color=#19F0FF>{email}</color>. It expires in 10 minutes. Check your spam folder if it doesn't arrive.");

        TMP_InputField input = NewInput("Code", "123456", TMP_InputField.ContentType.IntegerNumber, 6, new Vector2(0f, -10f));
        input.textComponent.characterSpacing = 30f;

        if (!string.IsNullOrEmpty(devCode))
        {
            input.text = devCode;
            Info("Test server: the code was filled in for you.");
        }

        Button login = Button("LOG IN", new Vector2(-190f, -180f), new Vector2(200f, 60f));
        Button resend = Button("RESEND", new Vector2(20f, -180f), new Vector2(180f, 60f));
        Button back = Button("BACK", new Vector2(210f, -180f), new Vector2(160f, 60f));

        login.onClick.AddListener(() =>
        {
            string code = input.text.Trim();

            if (code.Length != 6)
            {
                Error("Enter the 6-digit code.");
                return;
            }

            login.interactable = false;
            Info("Logging in...");

            Account.VerifyCode(email, code, (outcome, error) =>
            {
                if (this == null) return;
                login.interactable = true;

                switch (outcome)
                {
                    case LoginOutcome.Uploaded:
                        ShowDone("You're logged in. Your progress is now saved to your account.");
                        break;
                    case LoginOutcome.NeedsChoice:
                        ShowChoice();
                        break;
                    case LoginOutcome.LoadedCloud:
                        login.interactable = resend.interactable = back.interactable = false;
                        Info("Found your saved progress. Loading it... the game will restart.");
                        break;
                    default:
                        Error(error);
                        break;
                }
            });
        });

        resend.onClick.AddListener(() =>
        {
            resend.interactable = false;
            Info("Sending a new code...");

            Account.RequestCode(email, (ok, error, code) =>
            {
                if (this == null) return;
                resend.interactable = true;

                if (ok)
                {
                    Info("New code sent.");
                    if (!string.IsNullOrEmpty(code)) input.text = code;
                }
                else
                {
                    Error(error);
                }
            });
        });

        back.onClick.AddListener(ShowEmail);
    }

    private void ShowChoice()
    {
        Clear();
        Title("WHICH PROGRESS DO YOU WANT TO KEEP?");
        Body("Your account already has saved progress. Choose one; the other will be replaced.");

        CloudSaveSummary cloud = Account.PendingConflict ?? new CloudSaveSummary();
        CloudSaveSummary device = Account.DeviceSummary();

        Card("CLOUD SAVE", cloud.Describe(), new Vector2(-185f, -30f));
        Card("THIS DEVICE", device.Describe(), new Vector2(185f, -30f));

        Button useCloud = Button("USE CLOUD", new Vector2(-185f, -180f), new Vector2(260f, 60f));
        Button useDevice = Button("USE THIS DEVICE", new Vector2(185f, -180f), new Vector2(260f, 60f));

        useCloud.onClick.AddListener(() =>
        {
            useCloud.interactable = useDevice.interactable = false;
            Info("Loading your cloud save... the game will restart.");

            Account.ResolveConflict(true, (ok, error) =>
            {
                if (this == null) return;
                if (!ok) { Error(error); useCloud.interactable = useDevice.interactable = true; }
            });
        });

        useDevice.onClick.AddListener(() =>
        {
            useCloud.interactable = useDevice.interactable = false;
            Info("Uploading this device's progress...");

            Account.ResolveConflict(false, (ok, error) =>
            {
                if (this == null) return;

                if (ok) ShowDone("Saved. Your account now has this device's progress.");
                else { Error(error); useCloud.interactable = useDevice.interactable = true; }
            });
        });
    }

    private void ShowAccount()
    {
        Clear();
        Title("ACCOUNT");

        string status = Account.IsUploading ? "Saving..."
            : Account.HasUnsyncedChanges ? "Changes waiting to upload"
            : Account.LastSyncedAt.HasValue ? $"Saved to cloud at {Account.LastSyncedAt.Value:HH:mm}"
            : "Saved to cloud";

        Body($"Logged in as <color=#19F0FF>{Account.Email}</color>\n<color=#8FA9B5>{status}</color>");

        Button sync = Button("SYNC NOW", new Vector2(-230f, -180f), new Vector2(200f, 60f));
        Button logout = Button("LOG OUT", new Vector2(0f, -180f), new Vector2(200f, 60f));
        Button close = Button("CLOSE", new Vector2(230f, -180f), new Vector2(200f, 60f));

        sync.onClick.AddListener(() =>
        {
            sync.interactable = false;
            Info("Saving...");

            Account.SyncNow((ok, error) =>
            {
                if (this == null) return;
                sync.interactable = true;

                if (ok) Info("Saved to cloud.");
                else if (Account.PendingConflict != null) ShowChoice();
                else Error(error);
            });
        });

        logout.onClick.AddListener(() =>
        {
            Account.Logout();
            ShowDone("Logged out. Your progress stays in this browser; log in again to sync it.");
        });

        close.onClick.AddListener(Close);
    }

    private void ShowDone(string message)
    {
        Clear();
        Title("DONE");
        Body(message);

        Button ok = Button("OK", new Vector2(0f, -180f), new Vector2(200f, 60f));
        ok.onClick.AddListener(Close);
    }

    public void Close()
    {
        Destroy(gameObject);
    }

    // ---- builders -------------------------------------------------------------------

    private void Title(string text) =>
        RuntimeUIKit.NewText("Title", content, font, text, 30f, RuntimeUIKit.Cyan, new Vector2(0f, 180f), new Vector2(720f, 44f));

    private void Body(string text) =>
        RuntimeUIKit.NewText("Body", content, font, text, 19f, RuntimeUIKit.SubtleText, new Vector2(0f, 110f), new Vector2(680f, 80f));

    private Button Button(string label, Vector2 position, Vector2 size) =>
        RuntimeUIKit.NewButton(label, content, font, frame, label, 22f, position, size, 2f);

    private void Card(string heading, string body, Vector2 position)
    {
        RectTransform card = RuntimeUIKit.NewImage(heading, content, Color.white, frame, 2f).rectTransform;
        card.anchoredPosition = position;
        card.sizeDelta = new Vector2(340f, 150f);

        RuntimeUIKit.NewText("Heading", card, font, heading, 20f, RuntimeUIKit.Cyan, new Vector2(0f, 50f), new Vector2(320f, 30f));
        RuntimeUIKit.NewText("Body", card, font, body, 18f, Color.white, new Vector2(0f, -12f), new Vector2(320f, 90f));
    }

    private TMP_InputField NewInput(string name, string placeholder, TMP_InputField.ContentType type, int limit, Vector2 position)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(content, false);

        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(460f, 64f);

        var background = go.GetComponent<Image>();
        background.sprite = frame;
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 2f;
        background.color = Color.white;

        var input = go.GetComponent<TMP_InputField>();
        input.contentType = type;
        input.characterLimit = limit;

        foreach (TMP_Text text in go.GetComponentsInChildren<TMP_Text>(true))
        {
            if (font != null) text.font = font;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
        }

        input.textComponent.color = Color.white;

        if (input.placeholder is TMP_Text hint)
        {
            hint.text = placeholder;
            hint.color = new Color(0.55f, 0.66f, 0.7f, 0.8f);
            hint.fontStyle = FontStyles.Normal;
        }

        return input;
    }
}
