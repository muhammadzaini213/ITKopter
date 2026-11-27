using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Exports every BundleData entry (productId, name, IDR price) to
/// payments/worker/src/catalog.json. The payment server charges these
/// prices, so re-export and redeploy the worker after changing bundles.
/// </summary>
public static class BundleCatalogExporter
{
    private const string OutputPath = "payments/worker/src/catalog.json";

    [MenuItem("Tools/Payments/Export Bundle Catalog")]
    public static void Export()
    {
        var products = new List<string>();
        var seen = new HashSet<string>();
        var problems = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:BundleData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<BundleData>(path);

            if (data == null || data.Bundles == null)
                continue;

            foreach (BundleEntry entry in data.Bundles)
            {
                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(entry.ProductId))
                {
                    problems.Add($"{path}: '{entry.BundleName}' has no productId");
                    continue;
                }

                if (!seen.Add(entry.ProductId))
                {
                    problems.Add($"Duplicate productId '{entry.ProductId}'");
                    continue;
                }

                if (entry.Price <= 0f || Mathf.Abs(entry.Price - Mathf.Round(entry.Price)) > 0.001f)
                {
                    problems.Add($"'{entry.ProductId}': IDR price must be a positive whole number");
                    continue;
                }

                products.Add(
                    "    { \"productId\": \"" + Escape(entry.ProductId.Trim()) +
                    "\", \"name\": \"" + Escape(entry.BundleName.Trim()) +
                    "\", \"category\": \"" + Escape(data.BundleCategory) +
                    "\", \"price\": " + Mathf.RoundToInt(entry.Price) + " }");
            }
        }

        if (problems.Count > 0)
        {
            Debug.LogError("[BundleCatalogExporter] Fix these first:\n" + string.Join("\n", problems));
            return;
        }

        var sb = new StringBuilder();
        sb.Append("{\n  \"currency\": \"IDR\",\n  \"products\": [\n");
        sb.Append(string.Join(",\n", products));
        sb.Append("\n  ]\n}\n");

        string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, sb.ToString());

        Debug.Log($"[BundleCatalogExporter] Wrote {products.Count} products to {OutputPath}. Redeploy the worker (wrangler deploy).");
    }

    private static string Escape(string value) =>
        (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
}
