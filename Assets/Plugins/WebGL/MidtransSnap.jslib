// Midtrans Snap bridge for WebGL builds.
// Loads snap.js into the page and opens the Snap checkout overlay inside the
// game page (no new tab, so browser popup blockers don't interfere).
// Results come back through SendMessage(gameObjectName, "OnSnapResult", result).

mergeInto(LibraryManager.library, {

  MidtransSnap_Load: function (clientKeyPtr, isProduction) {
    if (typeof document === "undefined") return;
    if (document.getElementById("midtrans-snap-js")) return;

    var script = document.createElement("script");
    script.id = "midtrans-snap-js";
    script.src = isProduction
      ? "https://app.midtrans.com/snap/snap.js"
      : "https://app.sandbox.midtrans.com/snap/snap.js";
    script.setAttribute("data-client-key", UTF8ToString(clientKeyPtr));
    script.async = true;
    document.head.appendChild(script);
  },

  MidtransSnap_IsReady: function () {
    return (typeof window !== "undefined" && window.snap && typeof window.snap.pay === "function") ? 1 : 0;
  },

  MidtransSnap_Pay: function (tokenPtr, gameObjectPtr) {
    var token = UTF8ToString(tokenPtr);
    var target = UTF8ToString(gameObjectPtr);

    function send(result) {
      try { SendMessage(target, "OnSnapResult", result); } catch (e) { console.error(e); }
    }

    if (!window.snap || typeof window.snap.pay !== "function") {
      send("error");
      return;
    }

    function open() {
      try {
        window.snap.pay(token, {
          onSuccess: function () { send("success"); },
          onPending: function () { send("pending"); },
          onError: function () { send("error"); },
          onClose: function () { send("close"); }
        });
      } catch (e) {
        console.error(e);
        send("error");
      }
    }

    // In fullscreen only the game canvas is visible, so the Snap window
    // (added to the page) stays hidden until the player leaves fullscreen.
    // Leave fullscreen first, then open Snap.
    var fullscreen = document.fullscreenElement || document.webkitFullscreenElement;
    var exit = document.exitFullscreen || document.webkitExitFullscreen;

    if (fullscreen && exit) {
      try {
        var result = exit.call(document);
        if (result && typeof result.then === "function") result.then(open, open);
        else setTimeout(open, 250);
      } catch (e) {
        open();
      }
    } else {
      open();
    }
  }
});
