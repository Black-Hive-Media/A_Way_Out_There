using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Shared helpers for the content tools (validator, migration, CSV importer).</summary>
public static class ContentEditorUtility
{
    public const string MenuRoot = "Tools/Content/";

    public const string DataRoot = "Assets/Data";
    public const string ContentRoot = DataRoot + "/Content";
    public const string SourceRoot = DataRoot + "/Source";
    public const string DatabasePath = DataRoot + "/ContentDatabase.asset";

    /// <summary>All assets of type T (including subclasses) under Assets/.</summary>
    public static List<T> FindAll<T>() where T : ContentDefinition
    {
        var result = new List<T>();
        var seenPaths = new HashSet<string>();

        var types = TypeCache.GetTypesDerivedFrom<T>().Append(typeof(T)).Where(t => !t.IsAbstract);
        foreach (Type type in types)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name, new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!seenPaths.Add(path))
                    continue;
                foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (obj is T typed)
                        result.Add(typed);
            }
        }

        result.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
        return result;
    }

    /// <summary>First asset of type T per id (case-insensitive). Assets with an empty id are skipped.</summary>
    public static Dictionary<string, T> IndexById<T>(IEnumerable<T> assets) where T : ContentDefinition
    {
        var dict = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        foreach (T asset in assets)
            if (!string.IsNullOrWhiteSpace(asset.Id) && !dict.ContainsKey(asset.Id))
                dict.Add(asset.Id, asset);
        return dict;
    }

    /// <summary>Writes the private id field and marks the asset dirty.</summary>
    public static void SetId(ContentDefinition def, string id)
    {
        var so = new SerializedObject(def);
        so.FindProperty(ContentDefinition.IdFieldName).stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(def);
    }

    /// <summary>Creates a new content asset at folder/id.asset with its id set.</summary>
    public static T CreateAsset<T>(string folder, string id) where T : ContentDefinition
    {
        var asset = ScriptableObject.CreateInstance<T>();
        SaveNewAsset(asset, folder, id);
        return asset;
    }

    public static void SaveNewAsset(ContentDefinition asset, string folder, string id)
    {
        SetId(asset, id);
        EnsureFolder(folder);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{SafeFileName(id)}.asset");
        AssetDatabase.CreateAsset(asset, path);
    }

    /// <summary>Creates every missing folder in an "Assets/..." path.</summary>
    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    /// <summary>"CargoBayFull" -> "cargo_bay_full", "First Aid Kit" -> "first_aid_kit", "Tier1Laser" -> "tier_1_laser".</summary>
    public static string ToSnakeCase(string s)
    {
        var sb = new StringBuilder();
        if (string.IsNullOrEmpty(s))
            return "";

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (!char.IsLetterOrDigit(c))
            {
                AppendSeparator(sb);
                continue;
            }

            if (i > 0)
            {
                char p = s[i - 1];
                bool boundary =
                    (char.IsLower(p) && char.IsUpper(c)) ||
                    (char.IsLetter(p) && char.IsDigit(c)) ||
                    (char.IsDigit(p) && char.IsLetter(c)) ||
                    (char.IsUpper(p) && char.IsUpper(c) && i + 1 < s.Length && char.IsLower(s[i + 1]));
                if (boundary)
                    AppendSeparator(sb);
            }
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString().Trim('_');
    }

    private static void AppendSeparator(StringBuilder sb)
    {
        if (sb.Length > 0 && sb[sb.Length - 1] != '_')
            sb.Append('_');
    }

    /// <summary>Returns baseId, or baseId_2, baseId_3... if taken, and records it as taken.</summary>
    public static string UniqueId(string baseId, HashSet<string> taken)
    {
        string id = baseId;
        for (int n = 2; taken.Contains(id); n++)
            id = $"{baseId}_{n}";
        taken.Add(id);
        return id;
    }

    public static string SafeFileName(string id)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            id = id.Replace(c, '_');
        return id;
    }
}
