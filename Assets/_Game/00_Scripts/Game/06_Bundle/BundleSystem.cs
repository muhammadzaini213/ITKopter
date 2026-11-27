using System;
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Top-up purchases. With a payment server URL set, purchases are real
/// money through Midtrans Snap (see payments/worker): create order on the
/// server → Snap checkout → poll until paid → claim once → grant rewards.
/// Without a URL, the Editor (or allowDummyPayments) simulates payment.
///
/// Rewards are granted exactly once per order: the server refuses a second
/// claim, and this device remembers which orders it already granted, so a
/// crash between claim and grant still grants once on the next launch.
/// </summary>
public class BundleSystem : GameSystem<BundleSystem>
{
    public enum PurchaseState
    {
        CreatingOrder,
        WaitingForPayment,
        Granted,
        PendingPayment,   // checkout closed, payment not done yet (e.g. VA transfer)
        Cancelled,
        Failed
    }

    private const string PlayerIdKey = "payments.playerId";
    private const string PendingKey = "payments.pending";
    private const string GrantedKey = "payments.granted";

    [Header("Catalog")]
    [Tooltip("Every BundleData sold in the game (needed to resume orders outside the top-up scene).")]
    [SerializeField] private BundleData[] catalogs = new BundleData[0];

    [Header("Real Payments (Midtrans)")]
    [Tooltip("Cloudflare Worker URL, e.g. https://itkopter-payments.<account>.workers.dev. Empty = no real payments.")]
    [SerializeField] private string paymentServerUrl = "";

    [Tooltip("Midtrans CLIENT key (public). Never put the server key here.")]
    [SerializeField] private string midtransClientKey = "";

    [SerializeField] private bool midtransProduction = false;

    [Tooltip("Allow free simulated purchases in builds when no server URL is set (testing only).")]
    [SerializeField] private bool allowDummyPayments = false;

    [SerializeField, Min(1f)] private float pollInterval = 3f;
    [SerializeField, Min(30f)] private float checkoutTimeoutSeconds = 900f;
    [SerializeField, Min(5f)] private float backgroundPollInterval = 20f;

    /// <summary>Raised when a paid order's rewards were granted (also for resumed orders).</summary>
    public event Action<BundleEntry> OnBundleGranted;

    private bool purchaseInProgress;
    private Coroutine resumeRoutine;

    public bool RealPaymentsEnabled => !string.IsNullOrWhiteSpace(paymentServerUrl);

    public bool DummyPaymentsAllowed => Application.isEditor || allowDummyPayments;

    public bool IsPurchaseInProgress => purchaseInProgress;

    public override IEnumerator Initialize()
    {
        yield return null;
    }

    public override void PostInitialize()
    {
        if (!RealPaymentsEnabled)
            return;

        MidtransBridge.Preload(midtransClientKey, midtransProduction);
        ResumePendingOrders();
    }

    // =====================================================================
    // Validation
    // =====================================================================

    public bool CanPurchaseBundle(BundleData bundleData, BundleEntry bundle, out string error)
    {
        error = null;

        if (bundleData == null) { error = "Bundle data is null."; return false; }
        if (bundle == null) { error = "Bundle entry is null."; return false; }
        if (bundle.Price <= 0f) { error = "Bundle price must be greater than zero."; return false; }
        if (bundle.Rewards == null || bundle.Rewards.Length == 0) { error = "Bundle has no rewards."; return false; }

        if (bundleData.Bundles == null || !Array.Exists(bundleData.Bundles, item => item == bundle))
        {
            error = "Bundle does not belong to this bundle data.";
            return false;
        }

        if (RewardSystem.Instance == null)
        {
            error = "Reward system is unavailable.";
            return false;
        }

        var requests = BuildRequests(bundle, out error);
        if (requests == null)
            return false;

        return RewardSystem.Instance.CanGrantRewards(requests, out error);
    }

    private static List<RewardGrantRequest> BuildRequests(BundleEntry bundle, out string error)
    {
        error = null;
        var requests = new List<RewardGrantRequest>();

        foreach (BundleReward bundleReward in bundle.Rewards)
        {
            if (bundleReward == null || bundleReward.Reward == null)
            {
                error = "Bundle contains invalid reward data.";
                return null;
            }

            if (bundleReward.Amount <= 0)
            {
                error = "Reward amount must be greater than zero.";
                return null;
            }

            requests.Add(new RewardGrantRequest(bundleReward.Reward, bundleReward.Amount));
        }

        return requests;
    }

    // =====================================================================
    // Purchase entry point
    // =====================================================================

    /// <summary>
    /// Starts a purchase. onUpdate reports progress; the final state is
    /// Granted, PendingPayment, Cancelled or Failed (with a message).
    /// </summary>
    public void BeginPurchase(BundleData bundleData, BundleEntry bundle, Action<PurchaseState, string> onUpdate)
    {
        if (purchaseInProgress)
        {
            onUpdate?.Invoke(PurchaseState.Failed, "Another purchase is in progress.");
            return;
        }

        if (!CanPurchaseBundle(bundleData, bundle, out string error))
        {
            onUpdate?.Invoke(PurchaseState.Failed, error);
            return;
        }

        if (!RealPaymentsEnabled)
        {
            if (!DummyPaymentsAllowed)
            {
                onUpdate?.Invoke(PurchaseState.Failed, "Top-up is not available right now.");
                return;
            }

            bool ok = PurchaseBundle(bundleData, bundle, out error);
            onUpdate?.Invoke(ok ? PurchaseState.Granted : PurchaseState.Failed, error);
            return;
        }

        if (string.IsNullOrWhiteSpace(bundle.ProductId))
        {
            onUpdate?.Invoke(PurchaseState.Failed, "This bundle has no product ID.");
            return;
        }

        StartCoroutine(RealPurchaseRoutine(bundle, onUpdate));
    }

    /// <summary>Simulated purchase (Editor / testing): grants without payment.</summary>
    public bool PurchaseBundle(BundleData bundleData, BundleEntry bundle, out string error)
    {
        if (!CanPurchaseBundle(bundleData, bundle, out error))
            return false;

        Debug.LogWarning($"[DUMMY TOP-UP] Simulating payment for {bundle.BundleName} ({bundle.Price}). " +
                         "Set a payment server URL on BundleSystem for real payments.");

        return GrantBundle(bundle, out error);
    }

    public bool PurchaseBundle(BundleData bundleData, BundleEntry bundle)
    {
        bool success = PurchaseBundle(bundleData, bundle, out string error);

        if (!success)
            Debug.LogWarning($"Top-up failed: {error}");

        return success;
    }

    // =====================================================================
    // Real payment flow
    // =====================================================================

    private IEnumerator RealPurchaseRoutine(BundleEntry bundle, Action<PurchaseState, string> onUpdate)
    {
        purchaseInProgress = true;
        onUpdate?.Invoke(PurchaseState.CreatingOrder, "Creating order...");

        // 1. Server creates the Midtrans transaction (price comes from the server catalog).
        var create = new CreateOrderRequest { productId = bundle.ProductId, playerId = PlayerId };
        CreateOrderResponse order = null;
        string requestError = null;

        yield return PostJson<CreateOrderResponse>("/orders", JsonUtility.ToJson(create),
            r => order = r, e => requestError = e);

        if (order == null || string.IsNullOrEmpty(order.orderId) || string.IsNullOrEmpty(order.token))
        {
            purchaseInProgress = false;
            onUpdate?.Invoke(PurchaseState.Failed, FriendlyError(requestError, "Could not create the order."));
            yield break;
        }

        AddPending(order.orderId, bundle.ProductId);

        // 2. Checkout (in-page Snap overlay on WebGL, browser tab elsewhere).
        onUpdate?.Invoke(PurchaseState.WaitingForPayment, "Complete the payment in the Midtrans window...");

        string checkoutResult = null;
        bool sawPending = false;
        MidtransBridge.Instance.Pay(order.token, order.redirectUrl, r =>
        {
            checkoutResult = r;
            if (r == "pending") sawPending = true;
        });

        // 3. Poll the server until paid / failed / checkout closed / timeout.
        float started = Time.realtimeSinceStartup;
        float nextPoll = 0f;
        string status = "pending";

        while (true)
        {
            if (Time.realtimeSinceStartup >= nextPoll)
            {
                nextPoll = Time.realtimeSinceStartup + pollInterval;
                OrderStatusResponse polled = null;

                yield return GetJson<OrderStatusResponse>(
                    $"/orders/{order.orderId}?playerId={PlayerId}", r => polled = r, _ => { });

                if (polled != null)
                    status = polled.status;

                if (status == "paid" || status == "claimed" || status == "failed")
                    break;
            }

            bool overlayClosed = checkoutResult == "close" || checkoutResult == "error";
            bool timedOut = Time.realtimeSinceStartup - started > checkoutTimeoutSeconds;

            // Closed in-page checkout: one last check, then leave it to the background resume.
            if (overlayClosed || timedOut)
            {
                OrderStatusResponse last = null;
                yield return GetJson<OrderStatusResponse>(
                    $"/orders/{order.orderId}?playerId={PlayerId}", r => last = r, _ => { });

                status = last != null ? last.status : status;
                break;
            }

            yield return null;
        }

        purchaseInProgress = false;

        if (status == "paid" || status == "claimed")
        {
            string claimError = null;
            BundleEntry granted = null;
            yield return ClaimAndGrant(order.orderId, e => claimError = e, g => granted = g);

            if (granted != null)
                onUpdate?.Invoke(PurchaseState.Granted, null);
            else
                onUpdate?.Invoke(PurchaseState.Failed, FriendlyError(claimError, "Payment received, but rewards could not be granted yet. They will be retried automatically."));

            yield break;
        }

        if (status == "failed")
        {
            RemovePending(order.orderId);
            onUpdate?.Invoke(PurchaseState.Failed, "Payment failed or expired.");
            yield break;
        }

        // Still pending: cancelled before paying, or waiting for a transfer.
        // A payment method was chosen (VA / QRIS instructions shown), the
        // browser tab was used, or Snap reported success before the server
        // saw it: keep waiting. A plain close/error = cancelled.
        bool maybePaying = sawPending ||
                           checkoutResult == "redirected" ||
                           checkoutResult == "success";

        if (maybePaying)
        {
            onUpdate?.Invoke(PurchaseState.PendingPayment,
                "Waiting for your payment. Rewards are added automatically once it is confirmed.");
        }
        else
        {
            onUpdate?.Invoke(PurchaseState.Cancelled, "Payment cancelled.");
        }

        ResumePendingOrders();
    }

    // Claims the order on the server (once) and grants it locally (once per device).
    private IEnumerator ClaimAndGrant(string orderId, Action<string> onError, Action<BundleEntry> onGranted)
    {
        if (IsGranted(orderId))
        {
            RemovePending(orderId);
            yield break;
        }

        ClaimResponse claim = null;
        string error = null;
        long code = 0;

        yield return PostJson<ClaimResponse>($"/orders/{orderId}/claim",
            JsonUtility.ToJson(new ClaimRequest { playerId = PlayerId }),
            r => claim = r, e => error = e, c => code = c);

        // 409 + alreadyClaimed: claimed earlier but this device never granted it
        // (e.g. crashed right after claiming), so grant now.
        bool claimedNow = claim != null && claim.status == "claimed";

        if (!claimedNow)
        {
            onError?.Invoke(error ?? "Claim failed.");
            yield break;
        }

        BundleEntry entry = FindEntry(claim.productId);

        if (entry == null)
        {
            onError?.Invoke($"Unknown product '{claim.productId}'.");
            yield break;
        }

        if (!GrantBundle(entry, out string grantError))
        {
            onError?.Invoke(grantError);
            yield break;
        }

        MarkGranted(orderId);
        RemovePending(orderId);

        OnBundleGranted?.Invoke(entry);
        onGranted?.Invoke(entry);
    }

    // =====================================================================
    // Resume unfinished orders (paid later, app closed mid-checkout, ...)
    // =====================================================================

    public void ResumePendingOrders()
    {
        if (!RealPaymentsEnabled || resumeRoutine != null || LoadPending().Count == 0)
            return;

        resumeRoutine = StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        while (true)
        {
            List<PendingOrder> pending = LoadPending();

            if (pending.Count == 0)
                break;

            foreach (PendingOrder p in pending)
            {
                if (purchaseInProgress)
                    break;

                // Abandoned checkouts: stop polling after 48h (Midtrans expires them sooner).
                if (p.createdAt > 0 && Now - p.createdAt > PendingMaxAgeSeconds)
                {
                    RemovePending(p.orderId);
                    continue;
                }

                OrderStatusResponse status = null;
                long code = 0;
                yield return GetJson<OrderStatusResponse>(
                    $"/orders/{p.orderId}?playerId={PlayerId}", r => status = r, _ => { }, c => code = c);

                if (code == 404 || (status != null && status.status == "failed"))
                {
                    RemovePending(p.orderId);
                    continue;
                }

                if (status != null && (status.status == "paid" || status.status == "claimed"))
                {
                    BundleEntry granted = null;
                    yield return ClaimAndGrant(p.orderId, _ => { }, g => granted = g);

                    if (granted != null)
                        ShowGrantedPopup(granted);
                }
            }

            yield return new WaitForSecondsRealtime(backgroundPollInterval);
        }

        resumeRoutine = null;
    }

    // =====================================================================
    // Granting
    // =====================================================================

    private bool GrantBundle(BundleEntry bundle, out string error)
    {
        var requests = BuildRequests(bundle, out error);
        if (requests == null)
            return false;

        List<RewardGrantResult> results = RewardSystem.Instance.GrantRewards(requests);

        if (results == null || results.Count != requests.Count)
        {
            error = "Reward granting failed.";
            Debug.LogError(error);
            return false;
        }

        foreach (RewardGrantResult result in results)
        {
            if (result == null || !result.Success)
            {
                error = "One or more rewards could not be granted. Check RewardSystem for partial grants.";
                Debug.LogError(error);
                return false;
            }
        }

        Debug.Log($"[TOP-UP] '{bundle.BundleName}' granted.");
        error = null;
        return true;
    }

    public void ShowGrantedPopup(BundleEntry entry)
    {
        if (entry == null || entry.Rewards == null || RewardSystem.Instance == null)
            return;

        var rewards = new List<RewardData>();
        var amounts = new List<int>();

        foreach (BundleReward r in entry.Rewards)
        {
            if (r == null || r.Reward == null)
                continue;

            rewards.Add(r.Reward);
            amounts.Add(r.Amount);
        }

        if (rewards.Count > 0)
            RewardSystem.Instance.ShowRewardPopup(rewards, amounts);
    }

    private BundleEntry FindEntry(string productId)
    {
        if (catalogs == null || string.IsNullOrEmpty(productId))
            return null;

        foreach (BundleData data in catalogs)
        {
            if (data == null || data.Bundles == null)
                continue;

            foreach (BundleEntry entry in data.Bundles)
            {
                if (entry != null && entry.ProductId == productId)
                    return entry;
            }
        }

        return null;
    }

    // =====================================================================
    // Local bookkeeping (PlayerPrefs → IndexedDB on WebGL)
    // =====================================================================

    public string PlayerId
    {
        get
        {
            string id = PlayerPrefs.GetString(PlayerIdKey, string.Empty);

            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PlayerIdKey, id);
                PlayerPrefs.Save();
            }

            return id;
        }
    }

    [Serializable] private class PendingOrder { public string orderId; public string productId; public long createdAt; }

    private const long PendingMaxAgeSeconds = 60 * 60 * 48;

    private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    [Serializable] private class PendingList { public List<PendingOrder> orders = new List<PendingOrder>(); }
    [Serializable] private class GrantedList { public List<string> orderIds = new List<string>(); }

    private static List<PendingOrder> LoadPending()
    {
        string raw = PlayerPrefs.GetString(PendingKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return new List<PendingOrder>();
        var list = JsonUtility.FromJson<PendingList>(raw);
        return list?.orders ?? new List<PendingOrder>();
    }

    private static void SavePending(List<PendingOrder> orders)
    {
        PlayerPrefs.SetString(PendingKey, JsonUtility.ToJson(new PendingList { orders = orders }));
        PlayerPrefs.Save();
    }

    private static void AddPending(string orderId, string productId)
    {
        var list = LoadPending();
        if (list.Exists(o => o.orderId == orderId)) return;
        list.Add(new PendingOrder { orderId = orderId, productId = productId, createdAt = Now });
        SavePending(list);
    }

    private static void RemovePending(string orderId)
    {
        var list = LoadPending();
        if (list.RemoveAll(o => o.orderId == orderId) > 0)
            SavePending(list);
    }

    private static bool IsGranted(string orderId)
    {
        string raw = PlayerPrefs.GetString(GrantedKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return false;
        var list = JsonUtility.FromJson<GrantedList>(raw);
        return list != null && list.orderIds.Contains(orderId);
    }

    private static void MarkGranted(string orderId)
    {
        string raw = PlayerPrefs.GetString(GrantedKey, string.Empty);
        var list = string.IsNullOrEmpty(raw) ? new GrantedList() : JsonUtility.FromJson<GrantedList>(raw) ?? new GrantedList();
        if (!list.orderIds.Contains(orderId)) list.orderIds.Add(orderId);
        if (list.orderIds.Count > 200) list.orderIds.RemoveRange(0, list.orderIds.Count - 200);
        PlayerPrefs.SetString(GrantedKey, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    // =====================================================================
    // HTTP
    // =====================================================================

    [Serializable] private class CreateOrderRequest { public string productId; public string playerId; }
    [Serializable] private class CreateOrderResponse { public string orderId; public string productId; public int amount; public string token; public string redirectUrl; public string error; }
    [Serializable] private class OrderStatusResponse { public string orderId; public string productId; public string status; public string error; }
    [Serializable] private class ClaimRequest { public string playerId; }
    [Serializable] private class ClaimResponse { public string orderId; public string productId; public string status; public bool alreadyClaimed; public string error; }
    [Serializable] private class ErrorResponse { public string error; }

    private string Url(string path) => paymentServerUrl.TrimEnd('/') + path;

    private IEnumerator PostJson<T>(string path, string body, Action<T> onOk, Action<string> onError, Action<long> onCode = null) where T : class
    {
        using (var request = new UnityWebRequest(Url(path), UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 20;

            yield return request.SendWebRequest();
            Handle(request, onOk, onError, onCode);
        }
    }

    private IEnumerator GetJson<T>(string path, Action<T> onOk, Action<string> onError, Action<long> onCode = null) where T : class
    {
        using (var request = UnityWebRequest.Get(Url(path)))
        {
            request.timeout = 20;
            yield return request.SendWebRequest();
            Handle(request, onOk, onError, onCode);
        }
    }

    private static void Handle<T>(UnityWebRequest request, Action<T> onOk, Action<string> onError, Action<long> onCode) where T : class
    {
        onCode?.Invoke(request.responseCode);
        string text = request.downloadHandler != null ? request.downloadHandler.text : null;

        // 409 on claim still carries a useful body (alreadyClaimed / not_paid).
        if (!string.IsNullOrEmpty(text) && (request.result == UnityWebRequest.Result.Success || request.responseCode == 409))
        {
            T parsed = null;
            try { parsed = JsonUtility.FromJson<T>(text); } catch { }

            if (parsed != null)
            {
                onOk?.Invoke(parsed);
                return;
            }
        }

        string error = request.error;

        if (!string.IsNullOrEmpty(text))
        {
            try
            {
                var e = JsonUtility.FromJson<ErrorResponse>(text);
                if (e != null && !string.IsNullOrEmpty(e.error)) error = e.error;
            }
            catch { }
        }

        onError?.Invoke(string.IsNullOrEmpty(error) ? "Network error." : error);
    }

    private static string FriendlyError(string code, string fallback)
    {
        switch (code)
        {
            case "payments_not_configured": return "Payments are not configured on the server yet.";
            case "unknown_product": return "This bundle is not available for purchase.";
            case "gateway_error": return "The payment gateway is unavailable. Please try again.";
            case null: case "": return fallback;
            default: return fallback + " (" + code + ")";
        }
    }
}
