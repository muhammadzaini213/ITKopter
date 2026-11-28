using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

[Serializable]
public class AccountSaveData
{
    public string token;
    public string email;
    public string accountId;
    public long expiresAt;          // unix ms
    public int syncedRevision;      // cloud revision this device last matched
    public bool localDirty;         // a file changed since the last successful upload
    public bool guestChosen;        // "Play as guest" picked: don't ask on every launch

    /// <summary>Content hash of each file as last synced: saves that write identical data don't count as changes.</summary>
    public Dictionary<string, string> syncedHashes = new Dictionary<string, string>();
}

/// <summary>What a save holds, shown when choosing between cloud and device progress.</summary>
[Serializable]
public class CloudSaveSummary
{
    public string playerName;
    public int playerLevel;
    public int stagesCleared;
    public int chaptersCleared;
    public long savedAt;            // unix ms

    public string Describe()
    {
        string name = string.IsNullOrWhiteSpace(playerName) ? "No name" : playerName;
        string when = savedAt > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(savedAt).ToLocalTime().ToString("dd MMM yyyy HH:mm")
            : "unknown time";

        return $"{name}\nLv {playerLevel}   Stages {stagesCleared}   Chapters {chaptersCleared}\n<size=80%><color=#8FA9B5>{when}</color></size>";
    }
}

public enum LoginOutcome
{
    Failed,
    /// <summary>New account (or empty cloud): this device's progress was uploaded.</summary>
    Uploaded,
    /// <summary>The account already has a cloud save: the player must choose.</summary>
    NeedsChoice
}

/// <summary>
/// Email-code login and cloud saves (Worker: payments/worker/src/account.js).
///
/// Runs first at boot (Priority -100): for a logged-in player it fetches the
/// cloud save and, when another device saved since, writes those files
/// BEFORE the other systems load them, so nothing has to reload. Afterwards
/// every local save is uploaded a few seconds later. When both this device
/// and the cloud changed, <see cref="PendingConflict"/> asks the player which
/// progress to keep.
/// </summary>
public class AccountSystem : GameSystem<AccountSystem>
{
    private const string SaveFileName = "account";

    // Rewritten with a fresh timestamp on every quit / pause: uploaded, but
    // never a reason to ask "which progress do you want to keep?".
    private static readonly HashSet<string> VolatileFiles = new HashSet<string> { "energy" };

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void PersistentStorage_SyncAndReload();
#endif

    [Header("Server")]
    [SerializeField, Tooltip("Game server URL. Empty = use BundleSystem's Payment Server Url.")]
    private string serverUrl = "";

    [SerializeField, Min(1f)] private float requestTimeout = 10f;

    [SerializeField, Min(1f), Tooltip("Boot waits at most this long for the cloud save before continuing offline.")]
    private float bootTimeout = 6f;

    [SerializeField, Min(0.5f), Tooltip("Upload this many seconds after the last local save.")]
    private float uploadDelay = 5f;

    private AccountSaveData data = new AccountSaveData();
    private float uploadAt = -1f;
    private bool uploading;
    private bool uploadAfterBoot;
    private JObject cachedCloudData;
    private int cachedCloudRevision;

    public override int Priority => -100;

    public bool IsLoggedIn =>
        !string.IsNullOrEmpty(data.token) &&
        data.expiresAt > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public string Email => IsLoggedIn ? data.email : null;
    public string AccountId => IsLoggedIn ? data.accountId : null;
    public bool GuestChosen => data.guestChosen;
    public bool HasServer => !string.IsNullOrWhiteSpace(ServerUrl);
    public bool HasUnsyncedChanges => data.localDirty;
    public bool IsUploading => uploading;
    public DateTime? LastSyncedAt { get; private set; }

    /// <summary>Cloud progress differs from this device's: set until the player chooses.</summary>
    public CloudSaveSummary PendingConflict { get; private set; }

    public event Action OnAccountChanged;
    public event Action OnSyncChanged;

    /// <summary>Editor only: EditorPrefs key that points the game at a local test server (wrangler dev).</summary>
    public const string EditorServerOverrideKey = "ConceptWars.AccountServerOverride";

    public string ServerUrl
    {
        get
        {
#if UNITY_EDITOR
            string editorOverride = UnityEditor.EditorPrefs.GetString(EditorServerOverrideKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(editorOverride))
                return editorOverride.TrimEnd('/');
#endif

            if (!string.IsNullOrWhiteSpace(serverUrl))
                return serverUrl.TrimEnd('/');

            BundleSystem bundles = BundleSystem.Instance;
            return bundles != null && !string.IsNullOrWhiteSpace(bundles.ServerUrl) ? bundles.ServerUrl.TrimEnd('/') : null;
        }
    }

    // ---- lifecycle ------------------------------------------------------------

    public override IEnumerator Initialize()
    {
        data = Save.From(SaveFileName, new AccountSaveData()) ?? new AccountSaveData();
        data.syncedHashes ??= new Dictionary<string, string>();

        if (!IsLoggedIn || !HasServer)
            yield break;

        // Bring this device up to date before the other systems read their files.
        bool done = false;
        long status = 0;
        string body = null;

        StartCoroutine(Send("GET", "/save", null, bootTimeout, (code, text) => { status = code; body = text; done = true; }));

        while (!done)
            yield return null;

        if (status == 404)
        {
            uploadAfterBoot = true;         // empty cloud: upload this device
        }
        else if (status == 200 && TryParse(body, out JObject cloud))
        {
            int revision = cloud.Value<int>("revision");

            bool changedHere = HasMeaningfulLocalChanges();

            if (revision == data.syncedRevision)
            {
                uploadAfterBoot = data.localDirty || changedHere;
            }
            else if (!changedHere)
            {
                ApplyCloudFiles(cloud);
                data.syncedRevision = revision;
                Persist();
                Debug.Log($"[AccountSystem] Loaded cloud save (revision {revision}).");
            }
            else
            {
                CacheCloud(cloud);
                PendingConflict = ReadSummary(cloud);
            }
        }
        else if (status == 401)
        {
            ClearSession();
        }
        else
        {
            Debug.LogWarning($"[AccountSystem] Cloud save unavailable at boot ({status}); playing offline.");
        }
    }

    public override void PostInitialize()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.OnSaved += HandleSaved;

        if (uploadAfterBoot)
            ScheduleUpload(1f);
    }

    protected override void OnSingletonDestroyed()
    {
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.OnSaved -= HandleSaved;
    }

    private void Update()
    {
        if (uploadAt < 0f || uploading || Time.unscaledTime < uploadAt)
            return;

        uploadAt = -1f;

        if (IsLoggedIn && PendingConflict == null)
            StartCoroutine(UploadRoutine(false, null));
    }

    private void HandleSaved(string fileName)
    {
        if (fileName == SaveFileName || !IsLoggedIn)
            return;

        // Same content as last synced (e.g. a quit-time save): nothing to upload.
        string raw = SaveSystem.Instance != null ? SaveSystem.Instance.ReadRaw(fileName) : null;
        if (raw != null && data.syncedHashes.TryGetValue(fileName, out string synced) && synced == Hash(raw))
            return;

        if (!data.localDirty)
        {
            data.localDirty = true;
            Persist();
        }

        ScheduleUpload(uploadDelay);
    }

    private void ScheduleUpload(float delay)
    {
        uploadAt = Time.unscaledTime + delay;
    }

    // ---- login ------------------------------------------------------------------

    /// <summary>Emails a login code. done(ok, error message, dev code in local testing).</summary>
    public void RequestCode(string email, Action<bool, string, string> done)
    {
        if (!HasServer)
        {
            done?.Invoke(false, "Login is not available right now.", null);
            return;
        }

        StartCoroutine(Send("POST", "/auth/request", Json(new { email }), requestTimeout, (code, text) =>
        {
            if (code == 200 && TryParse(text, out JObject ok))
                done?.Invoke(true, null, ok.Value<string>("devCode"));
            else
                done?.Invoke(false, ErrorMessage(code, text), null);
        }));
    }

    /// <summary>Checks the code; on success logs in and looks at the cloud save.</summary>
    public void VerifyCode(string email, string code, Action<LoginOutcome, string> done)
    {
        StartCoroutine(VerifyRoutine(email, code, done));
    }

    private IEnumerator VerifyRoutine(string email, string code, Action<LoginOutcome, string> done)
    {
        long status = 0;
        string body = null;
        yield return Send("POST", "/auth/verify", Json(new { email, code }), requestTimeout, (c, t) => { status = c; body = t; });

        if (status != 200 || !TryParse(body, out JObject session))
        {
            done?.Invoke(LoginOutcome.Failed, ErrorMessage(status, body));
            yield break;
        }

        data.token = session.Value<string>("token");
        data.email = session.Value<string>("email");
        data.accountId = session.Value<string>("accountId");
        data.expiresAt = session.Value<long>("expiresAt");
        data.guestChosen = false;
        data.syncedRevision = 0;
        Persist();
        OnAccountChanged?.Invoke();

        // What does the account already hold?
        yield return Send("GET", "/save", null, requestTimeout, (c, t) => { status = c; body = t; });

        if (status == 404)
        {
            bool uploaded = false;
            yield return UploadRoutine(true, ok => uploaded = ok);
            done?.Invoke(uploaded ? LoginOutcome.Uploaded : LoginOutcome.Failed,
                uploaded ? null : "Logged in, but uploading your progress failed. It will retry.");
            yield break;
        }

        if (status == 200 && TryParse(body, out JObject cloud))
        {
            CacheCloud(cloud);
            PendingConflict = ReadSummary(cloud);
            OnSyncChanged?.Invoke();
            done?.Invoke(LoginOutcome.NeedsChoice, null);
            yield break;
        }

        done?.Invoke(LoginOutcome.Failed, ErrorMessage(status, body));
    }

    /// <summary>Keep the cloud progress (reloads the game) or this device's (overwrites the cloud).</summary>
    public void ResolveConflict(bool useCloud, Action<bool, string> done = null)
    {
        StartCoroutine(ResolveRoutine(useCloud, done));
    }

    private IEnumerator ResolveRoutine(bool useCloud, Action<bool, string> done)
    {
        if (!useCloud)
        {
            bool ok = false;
            yield return UploadRoutine(true, r => ok = r);

            if (ok)
                PendingConflict = null;

            OnSyncChanged?.Invoke();
            done?.Invoke(ok, ok ? null : "Upload failed. Check your connection and try again.");
            yield break;
        }

        if (cachedCloudData == null)
        {
            long status = 0;
            string body = null;
            yield return Send("GET", "/save", null, requestTimeout, (c, t) => { status = c; body = t; });

            if (status != 200 || !TryParse(body, out JObject cloud))
            {
                done?.Invoke(false, ErrorMessage(status, body));
                yield break;
            }

            CacheCloud(cloud);
        }

        data.syncedRevision = cachedCloudRevision;
        data.localDirty = false;
        PendingConflict = null;
        Persist();

        // From here on, the old in-memory progress must not be saved over
        // the cloud files (Player/Energy save on quit and pause).
        SaveSystem.Instance.BlockSaves();
        ApplyCloudFiles(new JObject { ["data"] = cachedCloudData });
        Persist();      // the account file itself is still allowed (hashes of the applied files)

        done?.Invoke(true, null);
        RestartGame();
    }

    public void ChooseGuest()
    {
        data.guestChosen = true;
        Persist();
        OnAccountChanged?.Invoke();
    }

    /// <summary>Logs out on this device. Local progress stays.</summary>
    public void Logout()
    {
        ClearSession();
        data.guestChosen = true;
        Persist();
        OnAccountChanged?.Invoke();
    }

    public void SyncNow(Action<bool, string> done = null)
    {
        if (!IsLoggedIn)
        {
            done?.Invoke(false, "Not logged in.");
            return;
        }

        StartCoroutine(UploadRoutine(false, ok => done?.Invoke(ok, ok ? null : PendingConflict != null
            ? "Your cloud save changed on another device."
            : "Sync failed. It will retry.")));
    }

    // ---- cloud save ---------------------------------------------------------------

    private IEnumerator UploadRoutine(bool force, Action<bool> done)
    {
        if (uploading || !IsLoggedIn || SaveSystem.Instance == null)
        {
            done?.Invoke(false);
            yield break;
        }

        uploading = true;
        OnSyncChanged?.Invoke();

        var files = new JObject();
        var hashes = new Dictionary<string, string>();

        foreach (string name in SaveSystem.Instance.SaveFileNames())
        {
            if (name == SaveFileName)
                continue;

            string raw = SaveSystem.Instance.ReadRaw(name);
            if (raw == null)
                continue;

            files[name] = raw;
            hashes[name] = Hash(raw);
        }

        string payload = JsonConvert.SerializeObject(new
        {
            data = files,
            summary = DeviceSummary(),
            savedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            baseRevision = data.syncedRevision,
            force
        });

        long status = 0;
        string body = null;
        yield return Send("PUT", "/save", payload, requestTimeout, (c, t) => { status = c; body = t; });

        uploading = false;
        bool ok = false;

        if (status == 200 && TryParse(body, out JObject result))
        {
            data.syncedRevision = result.Value<int>("revision");
            data.localDirty = false;
            data.syncedHashes = hashes;
            Persist();
            LastSyncedAt = DateTime.Now;
            ok = true;
        }
        else if (status == 409 && TryParse(body, out JObject conflict))
        {
            cachedCloudData = null;
            PendingConflict = ReadSummary(conflict);
        }
        else if (status == 401)
        {
            ClearSession();
            Persist();
            OnAccountChanged?.Invoke();
        }
        else
        {
            ScheduleUpload(30f);    // offline: try again later
        }

        OnSyncChanged?.Invoke();
        done?.Invoke(ok);
    }

    /// <summary>This device's progress, for the cloud summary and the choice screen.</summary>
    public CloudSaveSummary DeviceSummary()
    {
        var summary = new CloudSaveSummary { savedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded)
            return summary;

        summary.playerName = player.PlayerName;
        summary.playerLevel = player.PlayerLevel;

        foreach (string id in player.CompletedLevelIds)
        {
            if (id.StartsWith("stage_")) summary.stagesCleared++;
            else if (id.StartsWith("story_main_")) summary.chaptersCleared++;
        }

        return summary;
    }

    private void ApplyCloudFiles(JObject cloud)
    {
        if (!(cloud["data"] is JObject files) || SaveSystem.Instance == null)
            return;

        foreach (var file in files)
        {
            if (file.Key == SaveFileName || file.Value == null)
                continue;

            string raw = file.Value.Type == JTokenType.String ? file.Value.Value<string>() : file.Value.ToString(Formatting.Indented);
            SaveSystem.Instance.WriteRaw(file.Key, raw);
            data.syncedHashes[file.Key] = Hash(raw);
        }
    }

    private void CacheCloud(JObject cloud)
    {
        cachedCloudData = cloud["data"] as JObject;
        cachedCloudRevision = cloud.Value<int>("revision");
    }

    private static CloudSaveSummary ReadSummary(JObject cloud)
    {
        var summary = cloud["summary"] is JObject s ? s.ToObject<CloudSaveSummary>() : new CloudSaveSummary();
        summary ??= new CloudSaveSummary();

        if (summary.savedAt == 0)
            summary.savedAt = cloud.Value<long?>("savedAt") ?? 0;

        return summary;
    }

    // ---- helpers ------------------------------------------------------------------

    private void ClearSession()
    {
        data.token = null;
        data.email = null;
        data.accountId = null;
        data.expiresAt = 0;
        data.syncedRevision = 0;
        data.localDirty = false;
        data.syncedHashes = new Dictionary<string, string>();
        PendingConflict = null;
        cachedCloudData = null;
    }

    private void Persist() => SaveSystem.Instance?.SaveAccountFile(data, SaveFileName);

    /// <summary>Any save file (other than volatile ones) that differs from what was last synced.</summary>
    private bool HasMeaningfulLocalChanges()
    {
        if (SaveSystem.Instance == null)
            return false;

        foreach (string name in SaveSystem.Instance.SaveFileNames())
        {
            if (name == SaveFileName || VolatileFiles.Contains(name))
                continue;

            string raw = SaveSystem.Instance.ReadRaw(name);
            if (raw == null)
                continue;

            if (!data.syncedHashes.TryGetValue(name, out string synced) || synced != Hash(raw))
                return true;
        }

        return false;
    }

    private static string Hash(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Reboots with the files on disk (after a cloud save was written over local progress).</summary>
    public static void RestartGame()
    {
        SaveSystem.Flush();

#if UNITY_WEBGL && !UNITY_EDITOR
        PersistentStorage_SyncAndReload();
#elif UNITY_EDITOR
        Debug.Log("[AccountSystem] Cloud save applied. Press Play again to load it.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator Send(string method, string path, string jsonBody, float timeout, Action<long, string> done)
    {
        using var request = new UnityWebRequest(ServerUrl + path, method);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.CeilToInt(timeout);

        if (jsonBody != null)
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.SetRequestHeader("Content-Type", "application/json");
        }

        if (!string.IsNullOrEmpty(data.token))
            request.SetRequestHeader("Authorization", "Bearer " + data.token);

        yield return request.SendWebRequest();

        done?.Invoke(request.responseCode, request.downloadHandler != null ? request.downloadHandler.text : null);
    }

    private static string Json(object value) => JsonConvert.SerializeObject(value);

    private static bool TryParse(string text, out JObject result)
    {
        result = null;

        try
        {
            result = string.IsNullOrEmpty(text) ? null : JObject.Parse(text);
        }
        catch (JsonException)
        {
        }

        return result != null;
    }

    private static string ErrorMessage(long status, string body)
    {
        string error = TryParse(body, out JObject json) ? json.Value<string>("error") : null;

        switch (error)
        {
            case "invalid_email": return "That email doesn't look right.";
            case "too_soon": return "Please wait a minute before asking for another code.";
            case "too_many_requests": return "Too many codes today. Try again tomorrow.";
            case "email_failed": return "We couldn't send the email. Try again later.";
            case "wrong_code": return $"Wrong code. {json?.Value<int?>("attemptsLeft") ?? 0} tries left.";
            case "invalid_code": return "Enter the 6-digit code.";
            case "code_expired": return "That code expired. Ask for a new one.";
            case "auth_not_configured": return "Login is not set up on the server yet.";
        }

        return status == 0 ? "No connection to the server." : $"Something went wrong ({status}).";
    }
}
