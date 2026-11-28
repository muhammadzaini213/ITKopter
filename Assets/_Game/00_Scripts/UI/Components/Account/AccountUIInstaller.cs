using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Hooks the account UI into existing scenes without editing them:
///   Main menu (has "StoryBtn"): asks guests to log in once (until they pick
///   "Play as guest"), and shows the cloud-vs-device choice when one is pending.
///   Settings (has "SFXVol"): adds an Account row under the volume sliders.
/// </summary>
public static class AccountUIInstaller
{
    private const string MainMenuMarker = "StoryBtn";
    private const string SettingsMarker = "SFXVol";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Transform settingsRow = null;
        bool mainMenu = false;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == MainMenuMarker) mainMenu = true;
                else if (t.name == SettingsMarker) settingsRow = t;
            }
        }

        if (mainMenu)
            new GameObject("AccountMenuPrompt").AddComponent<AccountMenuPrompt>();

        if (settingsRow != null && settingsRow.parent.Find("AccountRow") == null)
            AccountSettingsRow.Create(settingsRow);
    }
}

/// <summary>Main menu: one login prompt for guests, or the pending cloud choice.</summary>
public class AccountMenuPrompt : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.8f);

        AccountSystem account = AccountSystem.Instance;

        if (account == null || !account.HasServer || AccountPopupUI.IsOpen)
            yield break;

        if (account.IsLoggedIn && account.PendingConflict != null)
            AccountPopupUI.Show();
        else if (!account.IsLoggedIn && !account.GuestChosen)
            AccountPopupUI.Show(firstLaunch: true);
    }
}

/// <summary>Settings: "Account  you@example.com  [ACCOUNT]" or "Account  Guest  [LOG IN]".</summary>
public class AccountSettingsRow : MonoBehaviour
{
    private TMP_Text statusText;
    private Button button;

    public static void Create(Transform sfxRow)
    {
        Transform parent = sfxRow.parent;
        Transform musicRow = parent.Find("MusicVol");

        var go = new GameObject("AccountRow", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        var sfx = (RectTransform)sfxRow;
        rt.anchorMin = sfx.anchorMin;
        rt.anchorMax = sfx.anchorMax;
        rt.pivot = sfx.pivot;
        rt.sizeDelta = sfx.sizeDelta;

        // One row below SFX, same spacing as Music -> SFX.
        Vector3 step = musicRow != null ? sfxRow.localPosition - musicRow.localPosition : new Vector3(0f, -120f, 0f);
        rt.localPosition = sfxRow.localPosition + step;

        go.AddComponent<AccountSettingsRow>().Build(sfxRow);
    }

    private void Build(Transform sfxRow)
    {
        TMP_Text template = sfxRow.GetComponentInChildren<TMP_Text>(true);
        TMP_FontAsset font = template != null ? template.font : RuntimeUIKit.FindFont();
        float size = template != null ? template.fontSize : 32f;
        var rt = (RectTransform)transform;
        float width = Mathf.Max(600f, rt.rect.width);

        statusText = RuntimeUIKit.NewText("Status", transform, font, "", size * 0.8f, Color.white,
            new Vector2(-width * 0.12f, 0f), new Vector2(width * 0.7f, Mathf.Max(60f, rt.rect.height)));
        statusText.alignment = TextAlignmentOptions.Left;

        button = RuntimeUIKit.NewButton("AccountButton", transform, font, RuntimeUIKit.FindFrameSprite(), "LOG IN",
            size * 0.7f, new Vector2(width * 0.36f, 0f), new Vector2(240f, 80f), 1.5f);
        button.onClick.AddListener(() => AccountPopupUI.Show());

        if (AccountSystem.Instance != null)
        {
            AccountSystem.Instance.OnAccountChanged += Refresh;
            AccountSystem.Instance.OnSyncChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (AccountSystem.Instance != null)
        {
            AccountSystem.Instance.OnAccountChanged -= Refresh;
            AccountSystem.Instance.OnSyncChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        AccountSystem account = AccountSystem.Instance;
        bool loggedIn = account != null && account.IsLoggedIn;

        statusText.text = loggedIn
            ? $"Account  <size=75%><color=#19F0FF>{account.Email}</color></size>"
            : "Account  <size=75%><color=#8FA9B5>Guest (this browser only)</color></size>";

        RuntimeUIKit.SetLabel(button, loggedIn ? "ACCOUNT" : "LOG IN");
        button.interactable = account != null && account.HasServer;
    }
}
