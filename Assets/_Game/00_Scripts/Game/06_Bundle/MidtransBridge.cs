using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// C# side of MidtransSnap.jslib. On WebGL it opens the Snap checkout overlay
/// in the page; elsewhere (Editor, desktop) it opens Snap's redirect URL in
/// the browser. Lives on its own persistent GameObject so the jslib can
/// SendMessage the result back by name.
/// </summary>
public class MidtransBridge : MonoBehaviour
{
    public const string ObjectName = "[MidtransBridge]";

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void MidtransSnap_Load(string clientKey, bool isProduction);
    [DllImport("__Internal")] private static extern int MidtransSnap_IsReady();
    [DllImport("__Internal")] private static extern void MidtransSnap_Pay(string token, string gameObjectName);
#endif

    private static MidtransBridge instance;
    private Action<string> pendingCallback;

    public static MidtransBridge Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(ObjectName);
                DontDestroyOnLoad(go);
                instance = go.AddComponent<MidtransBridge>();
            }

            return instance;
        }
    }

    /// <summary>True when the in-page Snap overlay can be used (WebGL build with snap.js loaded).</summary>
    public static bool InPageCheckoutAvailable
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return MidtransSnap_IsReady() == 1;
#else
            return false;
#endif
        }
    }

    public static void Preload(string clientKey, bool isProduction)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(clientKey))
            MidtransSnap_Load(clientKey, isProduction);
#endif
    }

    /// <summary>
    /// Opens checkout. onResult gets "success", "pending", "error" or "close"
    /// (in-page overlay), or "redirected" when a browser tab was opened instead.
    /// The payment itself is always confirmed by the server, never by this result.
    /// </summary>
    public void Pay(string token, string redirectUrl, Action<string> onResult)
    {
        pendingCallback = onResult;

#if UNITY_WEBGL && !UNITY_EDITOR
        if (MidtransSnap_IsReady() == 1)
        {
            MidtransSnap_Pay(token, ObjectName);
            return;
        }
#endif

        Application.OpenURL(redirectUrl);
        Deliver("redirected");
    }

    // Called from the jslib via SendMessage.
    public void OnSnapResult(string result)
    {
        Deliver(result);
    }

    private void Deliver(string result)
    {
        Action<string> callback = pendingCallback;

        // "pending" (VA / QRIS instructions shown) is followed by "close"
        // later, so keep listening until a final result.
        if (result != "pending")
            pendingCallback = null;

        callback?.Invoke(result);
    }
}
