#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Slafurry.Systems.Audio;

public class SfxReferenceGenerator : EditorWindow
{
    private const string AudioRoot = "Assets/_Game/03_Audio";
    private const string OutputDir = "Assets/_Game/03_Audio/SFX";

    [MenuItem("Audio/Generate SFX References from Files")]
    public static void Generate()
    {
        EnsureOutputDir();

        var scanResults = new Dictionary<string, List<(string prefix, string path)>>();

        var scanFolders = new[]
        {
            (folder: "SFX/UI", category: "UI"),
            (folder: "SFX/Unit", category: "Unit"),
            (folder: "Music", category: "Music"),
        };

        foreach (var (folder, category) in scanFolders)
        {
            string fullPath = Path.Combine(AudioRoot, folder);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[SfxReferenceGenerator] Folder not found: {fullPath}");
                continue;
            }

            if (!scanResults.ContainsKey(category))
                scanResults[category] = new List<(string, string)>();

            foreach (var file in Directory.GetFiles(fullPath, "*.ogg"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                int underscoreIdx = fileName.IndexOf('_');
                string prefix = underscoreIdx > 0 ? fileName.Substring(0, underscoreIdx) : fileName;

                scanResults[category].Add((prefix, file));
            }
        }

        var categoryAssets = new SFXCategory[scanResults.Count];
        int catIdx = 0;

        foreach (var kvp in scanResults)
        {
            string categoryName = kvp.Key;
            var entries = kvp.Value;

            var category = ScriptableObject.CreateInstance<SFXCategory>();
            category.categoryName = categoryName;
            category.poolSize = 8;

            var effects = new SFXEffect[entries.Count];
            int effIdx = 0;
            foreach (var (prefix, path) in entries)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                    Debug.LogWarning($"[SfxReferenceGenerator] AudioClip not imported yet: {path}");

                var effect = new SFXEffect
                {
                    groupID = prefix,
                    clips = new[] { clip },
                    volume = 1f,
                    maxSimultaneous = 3,
                    fadeIn = 0f,
                    fadeOut = 0f,
                    distanceOnlyNoPanning = true,
                    rolloffMode = AudioRolloffMode.Linear,
                    minDistance = 1f,
                    maxDistance = 15f,
                };
                effects[effIdx++] = effect;
            }
            category.effects = effects;

            string catAssetPath = Path.Combine(OutputDir, $"SFX Category_{categoryName}.asset");
            AssetDatabase.CreateAsset(category, catAssetPath);
            categoryAssets[catIdx++] = category;
        }

        var sfxData = ScriptableObject.CreateInstance<SFXData>();
        sfxData.categories = categoryAssets;
        string dataPath = Path.Combine(OutputDir, "SFX Data.asset");
        AssetDatabase.CreateAsset(sfxData, dataPath);

        foreach (var kvp in scanResults)
        {
            string categoryName = kvp.Key;
            foreach (var (prefix, _) in kvp.Value)
            {
                var reference = ScriptableObject.CreateInstance<SFXReference>();
                string safeName = $"SFX_REF_{categoryName}_{prefix}".Replace(" ", "_");
                string refPath = Path.Combine(OutputDir, $"{safeName}.asset");

                using (var so = new SerializedObject(reference))
                {
                    so.FindProperty("category").stringValue = categoryName;
                    so.FindProperty("sfxName").stringValue = prefix;
                    so.ApplyModifiedProperties();
                }

                AssetDatabase.CreateAsset(reference, refPath);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int refCount = 0;
        foreach (var v in scanResults.Values) refCount += v.Count;

        Debug.Log($"[SfxReferenceGenerator] Generated {categoryAssets.Length} categories, {refCount} references in {OutputDir}");
    }

    private static void EnsureOutputDir()
    {
        if (!AssetDatabase.IsValidFolder(OutputDir))
        {
            string parent = Path.GetDirectoryName(OutputDir);
            string folderName = new DirectoryInfo(OutputDir).Name;
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
