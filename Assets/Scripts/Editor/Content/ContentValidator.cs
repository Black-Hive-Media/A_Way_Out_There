using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Reports content assets with empty or duplicate ids. Click a Console entry to select the asset.</summary>
public static class ContentValidator
{
    [MenuItem(ContentEditorUtility.MenuRoot + "Validate Content IDs", priority = 1)]
    private static void ValidateMenu()
    {
        Validate(out string summary);
        EditorUtility.DisplayDialog("Content ID check", summary, "OK");
    }

    /// <summary>Logs every problem and returns how many were found.</summary>
    public static int Validate(out string summary)
    {
        List<ContentDefinition> all = ContentEditorUtility.FindAll<ContentDefinition>();
        int problems = 0;

        // Case-insensitive: "itm_Gold" and "itm_gold" would be a trap in any lookup.
        var byId = new Dictionary<string, List<ContentDefinition>>(StringComparer.OrdinalIgnoreCase);

        foreach (ContentDefinition def in all)
        {
            string id = def.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogError($"[Content] Empty id: {Describe(def)}", def);
                problems++;
                continue;
            }
            if (id != id.Trim())
            {
                Debug.LogError($"[Content] Id '{id}' has leading or trailing spaces: {Describe(def)}", def);
                problems++;
            }

            string key = id.Trim();
            if (!byId.TryGetValue(key, out var list))
                byId[key] = list = new List<ContentDefinition>();
            list.Add(def);
        }

        foreach (var pair in byId.Where(p => p.Value.Count > 1))
        {
            string where = string.Join(", ", pair.Value.Select(Describe));
            foreach (ContentDefinition def in pair.Value)
                Debug.LogError($"[Content] Duplicate id '{pair.Key}' used by {pair.Value.Count} assets: {where}", def);
            problems++;
        }

        summary = problems == 0
            ? $"All {all.Count} content assets have unique, non-empty IDs."
            : $"{problems} ID problem(s) across {all.Count} content assets.\nSee the Console; click an entry to select its asset.";

        if (problems == 0)
            Debug.Log("[Content] " + summary);
        return problems;
    }

    private static string Describe(ContentDefinition def) =>
        $"{AssetDatabase.GetAssetPath(def)} ({def.GetType().Name})";
}
