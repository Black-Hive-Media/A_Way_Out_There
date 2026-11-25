using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Fills the ContentDatabase asset with every content asset that has an id.</summary>
public static class ContentDatabaseBuilder
{
    [MenuItem(ContentEditorUtility.MenuRoot + "Rebuild Content Database", priority = 2)]
    private static void RebuildMenu() => Rebuild();

    public static ContentDatabase Rebuild()
    {
        var db = AssetDatabase.LoadAssetAtPath<ContentDatabase>(ContentEditorUtility.DatabasePath);
        if (db == null)
        {
            string existing = AssetDatabase.FindAssets("t:" + nameof(ContentDatabase), new[] { "Assets" }).FirstOrDefault();
            if (existing != null)
                db = AssetDatabase.LoadAssetAtPath<ContentDatabase>(AssetDatabase.GUIDToAssetPath(existing));
        }
        if (db == null)
        {
            ContentEditorUtility.EnsureFolder(ContentEditorUtility.DataRoot);
            db = ScriptableObject.CreateInstance<ContentDatabase>();
            AssetDatabase.CreateAsset(db, ContentEditorUtility.DatabasePath);
        }

        var entries = ContentEditorUtility.FindAll<ContentDefinition>()
            .Where(d => !string.IsNullOrWhiteSpace(d.Id))
            .OrderBy(d => d.GetType().Name)
            .ThenBy(d => d.Id, System.StringComparer.Ordinal)
            .ToList();

        db.EditorSetEntries(entries);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Content] ContentDatabase rebuilt with {entries.Count} entries ({AssetDatabase.GetAssetPath(db)}).", db);
        return db;
    }
}
