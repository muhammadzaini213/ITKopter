using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;
using Slafurry.Core.Abstract;

namespace Slafurry.Systems.Save
{
    public static class Save
    {
        // Null-safe: systems save in OnApplicationQuit, when SaveSystem may already be gone.
        public static void To<T>(T data, string fileName) => SaveSystem.Instance?.Save(data, fileName);
        public static T From<T>(string fileName, T fallback = default) => SaveSystem.Instance != null ? SaveSystem.Instance.Load(fileName, fallback) : fallback;
        public static bool Exists(string fileName) => SaveSystem.Instance != null && SaveSystem.Instance.HasSave(fileName);
    }
    
    public class SaveSystem : GameSystem<SaveSystem>
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void PersistentStorage_Sync();
#endif

        // WebGL: Application.persistentDataPath is "/idbfs/<hash of the page URL>",
        // and every itch.io upload gets a new URL, so each deploy started from an
        // empty save. Use a fixed folder under /idbfs instead.
        private const string WebSaveFolderName = "ConceptWars";

        private string saveFolder;

        /// <summary>Folder holding the save files (WebGL: a fixed IndexedDB-backed folder).</summary>
        public string SaveFolder
        {
            get
            {
                if (saveFolder == null)
                    saveFolder = ResolveSaveFolder();

                return saveFolder;
            }
        }

        private static string ResolveSaveFolder()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string legacy = Application.persistentDataPath;
            string root = Path.GetDirectoryName(legacy.TrimEnd('/'));
            string folder = string.IsNullOrEmpty(root) ? legacy : root.Replace('\\', '/') + "/" + WebSaveFolderName;

            try
            {
                Directory.CreateDirectory(folder);
                MigrateLegacySaves(legacy, folder);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Can't use '{folder}', falling back to '{legacy}': {e}");
                return legacy;
            }

            return folder;
#else
            return Application.persistentDataPath;
#endif
        }

        // Copy saves from the old URL-hashed folder the first time only.
        private static void MigrateLegacySaves(string from, string to)
        {
            if (from == to || !Directory.Exists(from) || Directory.GetFiles(to, "*.json").Length > 0)
                return;

            bool copied = false;

            foreach (string file in Directory.GetFiles(from, "*.json"))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
                copied = true;
            }

            if (copied)
                Flush();
        }

        /// <summary>Persists pending file writes (WebGL: to IndexedDB). No-op elsewhere.</summary>
        public static void Flush()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PersistentStorage_Sync();
#endif
        }

        public event Action<string> OnSaved;
        public event Action<string> OnLoaded;

        public override IEnumerator Initialize() { yield return null; }
        public override void PostInitialize() { }

        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
        }

        /// <summary>
        /// Set after a cloud save was written for a restart: the systems still
        /// hold the old progress in memory, and their quit/pause saves must not
        /// overwrite the files the game is about to boot with.
        /// </summary>
        public bool SavesBlocked { get; private set; }

        public void BlockSaves() => SavesBlocked = true;

        /// <summary>The account file is still written while saves are blocked (it records what was just applied).</summary>
        public void SaveAccountFile<T>(T data, string fileName)
        {
            bool blocked = SavesBlocked;
            SavesBlocked = false;
            Save(data, fileName);
            SavesBlocked = blocked;
        }

        public void Save<T>(T data, string fileName)
        {
            if (SavesBlocked)
            {
                Debug.Log($"[SaveSystem] Skipped saving '{fileName}': restarting with a cloud save.");
                return;
            }

            try
            {
                string path = GetPath(fileName);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
                Flush();
                OnSaved?.Invoke(fileName);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save '{fileName}': {e}");
            }
        }

        public T Load<T>(string fileName, T fallback = default)
        {
            try
            {
                string path = GetPath(fileName);
                if (!File.Exists(path)) return fallback;

                string json = File.ReadAllText(path);
                T result = JsonConvert.DeserializeObject<T>(json);
                OnLoaded?.Invoke(fileName);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load '{fileName}': {e}");
                return fallback;
            }
        }

        public bool HasSave(string fileName) => File.Exists(GetPath(fileName));

        /// <summary>Names (without .json) of all save files on this device.</summary>
        public System.Collections.Generic.List<string> SaveFileNames()
        {
            var names = new System.Collections.Generic.List<string>();

            if (!Directory.Exists(SaveFolder))
                return names;

            foreach (string file in Directory.GetFiles(SaveFolder, "*.json"))
                names.Add(Path.GetFileNameWithoutExtension(file));

            return names;
        }

        /// <summary>Raw JSON of a save file, or null (cloud sync).</summary>
        public string ReadRaw(string fileName)
        {
            string path = GetPath(fileName);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        /// <summary>Writes a save file's raw JSON as-is (cloud sync). Does not raise OnSaved.</summary>
        public void WriteRaw(string fileName, string json)
        {
            try
            {
                File.WriteAllText(GetPath(fileName), json);
                Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to write '{fileName}': {e}");
            }
        }

        public void DeleteSave(string fileName)
        {
            string path = GetPath(fileName);
            if (File.Exists(path)) File.Delete(path);
            Flush();
        }

        private string GetPath(string fileName) => Path.Combine(SaveFolder, $"{fileName}.json");
    }
}