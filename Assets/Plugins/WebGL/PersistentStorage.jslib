// Flushes Unity's in-memory file system to the browser's IndexedDB.
// On WebGL, File.WriteAllText only changes memory; without FS.syncfs the
// save is gone when the tab is closed or reloaded.

mergeInto(LibraryManager.library, {

  PersistentStorage_Sync: function () {
    if (typeof FS === "undefined" || typeof FS.syncfs !== "function") return;

    var state = Module.__persistentSync || (Module.__persistentSync = { running: false, again: false });

    // Only one syncfs may run at a time; coalesce saves made meanwhile.
    if (state.running) { state.again = true; return; }

    function run() {
      state.running = true;
      state.again = false;
      FS.syncfs(false, function (err) {
        state.running = false;
        if (err) console.error("[PersistentStorage] sync failed", err);
        if (state.again) run();
      });
    }

    run();
  },

  // Saves the file system to IndexedDB, then reloads the page (a cloud
  // save was written over the local one; the game must boot with it).
  PersistentStorage_SyncAndReload: function () {
    function reload() { try { window.location.reload(); } catch (e) { console.error(e); } }
    if (typeof FS === "undefined" || typeof FS.syncfs !== "function") { reload(); return; }
    FS.syncfs(false, function (err) {
      if (err) console.error("[PersistentStorage] sync before reload failed", err);
      reload();
    });
  }
});
