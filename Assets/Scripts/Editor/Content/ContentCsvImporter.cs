using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Describes how one CSV file maps onto one content type. Add a subclass to support a new sheet;
/// the importer finds it automatically.
/// </summary>
public abstract class ContentTable
{
    /// <summary>File name inside Assets/Data/Source, e.g. "ai_messages.csv".</summary>
    public abstract string FileName { get; }
    /// <summary>Sub-folder of Assets/Data/Content that new assets are created in.</summary>
    public abstract string ContentFolder { get; }
    public abstract Type AssetType { get; }
    /// <summary>Known columns, in export order. "id" must be first.</summary>
    public abstract string[] Columns { get; }

    public abstract ContentDefinition CreateInstance();
    /// <summary>Copy the row onto the asset. Only touch fields whose column is present (row.Has).</summary>
    public abstract void Apply(ContentDefinition asset, CsvRow row, ContentImportReport report);
    public abstract List<string[]> ExportRows(ContentImportReport report);
    public virtual void AfterImport(List<ContentDefinition> imported, ContentImportReport report) { }
}

public abstract class ContentTable<T> : ContentTable where T : ContentDefinition
{
    public override Type AssetType => typeof(T);
    public override ContentDefinition CreateInstance() => ScriptableObject.CreateInstance<T>();
    public override void Apply(ContentDefinition asset, CsvRow row, ContentImportReport report) => Apply((T)asset, row, report);
    public override void AfterImport(List<ContentDefinition> imported, ContentImportReport report) =>
        AfterImport(imported.Cast<T>().ToList(), report);

    public override List<string[]> ExportRows(ContentImportReport report)
    {
        var rows = new List<string[]>();
        foreach (T asset in ContentEditorUtility.FindAll<T>().OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(asset.Id))
            {
                report.Warn($"{AssetDatabase.GetAssetPath(asset)} has no id; not exported. Run the migration first.", asset);
                continue;
            }
            rows.Add(ToRow(asset, report));
        }
        return rows;
    }

    protected abstract void Apply(T asset, CsvRow row, ContentImportReport report);
    protected abstract string[] ToRow(T asset, ContentImportReport report);
    protected virtual void AfterImport(List<T> imported, ContentImportReport report) { }

    protected static string JoinList(string[] values, T asset, string column, ContentImportReport report)
    {
        if (values == null)
            return "";
        if (values.Any(v => v != null && v.Contains(CsvRow.ListSeparator)))
            report.Warn($"{asset.Id}.{column} contains '{CsvRow.ListSeparator}', which the importer reads as a separator.", asset);
        return string.Join(" " + CsvRow.ListSeparator + " ", values);
    }
}

public sealed class ContentImportReport
{
    public int Errors { get; private set; }
    public int Warnings { get; private set; }
    private readonly StringBuilder lines = new StringBuilder();

    public void Info(string message) => lines.AppendLine(message);

    public void Warn(string message, UnityEngine.Object context = null)
    {
        Warnings++;
        lines.AppendLine("Warning: " + message);
        Debug.LogWarning("[Content] " + message, context);
    }

    public void Error(string message, UnityEngine.Object context = null)
    {
        Errors++;
        lines.AppendLine("Error: " + message);
        Debug.LogError("[Content] " + message, context);
    }

    public override string ToString() => lines.ToString();
}

/// <summary>
/// Reads Assets/Data/Source/*.csv and creates or updates content assets, matched by id.
/// Existing assets are updated where they are (they are not moved); new ones go to
/// Assets/Data/Content/&lt;table folder&gt;/. Rows missing from a sheet are never deleted.
/// </summary>
public static class ContentCsvImporter
{
    private static readonly HashSet<string> IgnoredColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "notes", "comment", "comments" };

    public static List<ContentTable> Tables =>
        TypeCache.GetTypesDerivedFrom<ContentTable>()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .Select(t => (ContentTable)Activator.CreateInstance(t))
            .OrderBy(t => t.FileName)
            .ToList();

    [MenuItem(ContentEditorUtility.MenuRoot + "Import All CSVs", priority = 40)]
    public static void ImportAll()
    {
        if (!Directory.Exists(ContentEditorUtility.SourceRoot))
        {
            EditorUtility.DisplayDialog("Import CSVs", $"No {ContentEditorUtility.SourceRoot} folder yet. Export a sheet first (Tools > Content > Export CSV).", "OK");
            return;
        }

        var tables = Tables.ToDictionary(t => t.FileName, StringComparer.OrdinalIgnoreCase);
        var report = new ContentImportReport();

        foreach (string file in Directory.GetFiles(ContentEditorUtility.SourceRoot, "*.csv").OrderBy(f => f, StringComparer.Ordinal))
        {
            string name = Path.GetFileName(file);
            if (tables.TryGetValue(name, out ContentTable table))
                ImportFile(table, file, report);
            else
                report.Warn($"{name}: no importer handles this file yet; skipped. Known files: {string.Join(", ", tables.Keys)}.");
        }

        Finish("Import CSVs", report);
    }

    [MenuItem(ContentEditorUtility.MenuRoot + "Import CSV/AI Messages", priority = 41)]
    private static void ImportAIMessages() => ImportSingle(new AIMessageTable());

    [MenuItem(ContentEditorUtility.MenuRoot + "Export CSV/AI Messages", priority = 60)]
    private static void ExportAIMessages() => Export(new AIMessageTable());

    public static void ImportSingle(ContentTable table)
    {
        string path = $"{ContentEditorUtility.SourceRoot}/{table.FileName}";
        var report = new ContentImportReport();
        if (File.Exists(path))
            ImportFile(table, path, report);
        else
            report.Error($"{path} not found. Use Tools > Content > Export CSV to create it from the current assets.");
        Finish("Import " + table.FileName, report);
    }

    private static void ImportFile(ContentTable table, string path, ContentImportReport report)
    {
        string fileName = Path.GetFileName(path);
        List<List<string>> records;
        try
        {
            records = Csv.Parse(File.ReadAllText(path, Encoding.UTF8));
        }
        catch (IOException e)
        {
            report.Error($"{fileName}: can't read ({e.Message}). Close it in Excel and try again.");
            return;
        }
        if (records.Count == 0)
        {
            report.Warn($"{fileName}: empty; skipped.");
            return;
        }

        // Header
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int c = 0; c < records[0].Count; c++)
        {
            string header = records[0][c].Trim();
            if (header.Length == 0)
                continue;
            if (columns.ContainsKey(header))
                report.Warn($"{fileName}: column '{header}' appears twice; using the first.");
            else
                columns.Add(header, c);
        }
        if (!columns.ContainsKey("id"))
        {
            report.Error($"{fileName}: no 'id' column; skipped.");
            return;
        }
        var known = new HashSet<string>(table.Columns, StringComparer.OrdinalIgnoreCase);
        foreach (string header in columns.Keys.Where(h => !known.Contains(h) && !IgnoredColumns.Contains(h) && !h.StartsWith("_")))
            report.Warn($"{fileName}: unknown column '{header}' ignored (typo?). Known: {string.Join(", ", table.Columns)}.");

        // Every content asset by id, so an id taken by another type is caught.
        Dictionary<string, ContentDefinition> byId = ContentEditorUtility.IndexById(ContentEditorUtility.FindAll<ContentDefinition>());
        string folder = $"{ContentEditorUtility.ContentRoot}/{table.ContentFolder}";
        var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var imported = new List<ContentDefinition>();
        int created = 0, updated = 0, unchanged = 0;

        for (int r = 1; r < records.Count; r++)
        {
            var row = new CsvRow(columns, records[r], r + 1);
            string id = row.Text("id").Trim();

            if (row.IsEmpty || id.StartsWith("#"))
                continue;
            if (id.Length == 0)
            {
                report.Warn($"{fileName} row {row.RowNumber}: no id; skipped.");
                continue;
            }
            if (!seenInFile.Add(id))
            {
                report.Error($"{fileName} row {row.RowNumber}: id '{id}' appears more than once; only the first row was used.");
                continue;
            }

            if (byId.TryGetValue(id, out ContentDefinition asset))
            {
                if (!table.AssetType.IsInstanceOfType(asset))
                {
                    report.Error($"{fileName} row {row.RowNumber}: id '{id}' already belongs to {AssetDatabase.GetAssetPath(asset)} ({asset.GetType().Name}); skipped.", asset);
                    continue;
                }

                string before = EditorJsonUtility.ToJson(asset);
                table.Apply(asset, row, report);
                if (EditorJsonUtility.ToJson(asset) != before)
                {
                    EditorUtility.SetDirty(asset);
                    updated++;
                }
                else
                    unchanged++;
            }
            else
            {
                asset = table.CreateInstance();
                table.Apply(asset, row, report);
                ContentEditorUtility.SaveNewAsset(asset, folder, id);
                byId[id] = asset;
                created++;
            }
            imported.Add(asset);
        }

        table.AfterImport(imported, report);

        int notInSheet = byId.Values.Count(a => table.AssetType.IsInstanceOfType(a) && !seenInFile.Contains(a.Id));
        report.Info($"{fileName}: {created} created, {updated} updated, {unchanged} unchanged" +
                    (notInSheet > 0 ? $", {notInSheet} existing asset(s) not in the sheet left as is." : "."));
    }

    private static void Finish(string title, ContentImportReport report)
    {
        AssetDatabase.SaveAssets();
        ContentDatabaseBuilder.Rebuild();
        int problems = ContentValidator.Validate(out string validation);

        string text = report + "\n" + validation;
        if (report.Errors > 0 || problems > 0)
            Debug.LogError($"[Content] {title} finished with problems.\n{text}");
        else
            Debug.Log($"[Content] {title} finished.\n{text}");
        EditorUtility.DisplayDialog(title, text, "OK");
    }

    public static void Export(ContentTable table)
    {
        string path = $"{ContentEditorUtility.SourceRoot}/{table.FileName}";
        if (File.Exists(path) && !EditorUtility.DisplayDialog("Export CSV",
                $"{path} already exists. Overwrite it with the current assets?\n\nAny unimported edits in the file will be lost.",
                "Overwrite", "Cancel"))
            return;

        var report = new ContentImportReport();
        var rows = new List<string[]> { table.Columns };
        rows.AddRange(table.ExportRows(report));

        ContentEditorUtility.EnsureFolder(ContentEditorUtility.SourceRoot);
        Csv.Write(path, rows);
        AssetDatabase.ImportAsset(path);

        report.Info($"Wrote {rows.Count - 1} row(s) to {path}.");
        Debug.Log("[Content] " + report);
        EditorUtility.DisplayDialog("Export CSV", report.ToString(), "OK");
    }
}

/// <summary>
/// ai_messages.csv. Replaces AIDataImporter.
/// Columns: id, message_id (the trigger key AI_Manager.TriggerMessage uses), helpful, snarky
/// (variations separated by |), snark_threshold.
/// </summary>
public sealed class AIMessageTable : ContentTable<AI_Message>
{
    public override string FileName => "ai_messages.csv";
    public override string ContentFolder => "AIMessages";
    public override string[] Columns => new[] { "id", "message_id", "helpful", "snarky", "snark_threshold" };

    protected override void Apply(AI_Message msg, CsvRow row, ContentImportReport report)
    {
        if (row.Has("message_id"))
            msg.messageID = row.Text("message_id").Trim();
        if (row.Has("helpful"))
            msg.helpfulVariations = row.List("helpful");
        if (row.Has("snarky"))
            msg.snarkyVariations = row.List("snarky");
        if (row.Has("snark_threshold") && row.Text("snark_threshold").Trim().Length > 0)
        {
            if (row.TryFloat("snark_threshold", out float t))
                msg.snarkThreshold = t;
            else
                report.Warn($"{FileName} row {row.RowNumber}: snark_threshold '{row.Text("snark_threshold")}' is not a number; kept {msg.snarkThreshold}.");
        }
    }

    protected override string[] ToRow(AI_Message msg, ContentImportReport report) => new[]
    {
        msg.Id,
        msg.messageID,
        JoinList(msg.helpfulVariations, msg, "helpful", report),
        JoinList(msg.snarkyVariations, msg, "snarky", report),
        msg.snarkThreshold.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>Register each message under its trigger key so AI_Manager can find it.</summary>
    protected override void AfterImport(List<AI_Message> imported, ContentImportReport report)
    {
        if (AIMessageDatabaseEditorUtility.FindDatabases().Count == 0)
        {
            report.Warn("No AIMessageDatabase asset found; imported messages are not registered for AI_Manager.");
            return;
        }

        int registered = 0;
        foreach (AI_Message msg in imported)
        {
            string key = string.IsNullOrWhiteSpace(msg.messageID) ? msg.Id : msg.messageID;
            if (AIMessageDatabaseEditorUtility.Register(msg, key) > 0)
            {
                report.Info($"Registered trigger '{key}' -> {msg.name}");
                registered++;
            }
        }
        if (registered > 0)
            report.Info($"{registered} new trigger key(s) added to the AI message database.");
    }
}
