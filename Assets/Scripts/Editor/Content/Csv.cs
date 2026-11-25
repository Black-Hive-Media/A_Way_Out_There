using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Minimal RFC 4180 CSV reader/writer: quoted fields, "" escapes, commas and line breaks inside
/// quotes, UTF-8 with or without BOM. Matches what Excel and Google Sheets export.
/// </summary>
public static class Csv
{
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;
        int i = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

        void EndField()
        {
            row.Add(field.ToString().Replace("\r\n", "\n"));
            field.Clear();
        }
        void EndRow()
        {
            EndField();
            bool blank = row.Count == 1 && row[0].Length == 0;
            if (!blank)
                rows.Add(row);
            row = new List<string>();
        }

        for (; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
                continue;
            }

            switch (c)
            {
                case '"': inQuotes = true; break;
                case ',': EndField(); break;
                case '\r':
                    if (i + 1 < text.Length && text[i + 1] == '\n') i++;
                    EndRow();
                    break;
                case '\n': EndRow(); break;
                default: field.Append(c); break;
            }
        }
        if (field.Length > 0 || row.Count > 0)
            EndRow();
        return rows;
    }

    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        bool quote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 || value[0] == ' ' || value[value.Length - 1] == ' ';
        return quote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }

    /// <summary>Writes UTF-8 with BOM so Excel shows non-ASCII text (e.g. "…") correctly.</summary>
    public static void Write(string path, IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
            sb.Append(string.Join(",", row.Select(Escape))).Append("\r\n");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}

/// <summary>One data row of a CSV, read by column name (case-insensitive).</summary>
public sealed class CsvRow
{
    public const char ListSeparator = '|';

    private readonly Dictionary<string, int> columns;
    private readonly List<string> cells;

    public int RowNumber { get; }

    public CsvRow(Dictionary<string, int> columns, List<string> cells, int rowNumber)
    {
        this.columns = columns;
        this.cells = cells;
        RowNumber = rowNumber;
    }

    /// <summary>True if the sheet has this column. Absent columns leave the asset's field untouched.</summary>
    public bool Has(string column) => columns.ContainsKey(column);

    /// <summary>Raw cell text, or "" if the column or cell is missing.</summary>
    public string Text(string column) =>
        columns.TryGetValue(column, out int i) && i < cells.Count ? cells[i] : "";

    public bool IsEmpty => cells.All(string.IsNullOrWhiteSpace);

    /// <summary>"a | b | c" -> ["a","b","c"]; empty cell -> empty array.</summary>
    public string[] List(string column) =>
        Text(column).Split(ListSeparator).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

    public bool TryFloat(string column, out float value) =>
        float.TryParse(Text(column).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    public bool TryInt(string column, out int value) =>
        int.TryParse(Text(column).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
