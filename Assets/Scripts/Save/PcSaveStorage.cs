using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// PC saves: Application.persistentDataPath/saves/slot_&lt;n&gt;.json. Each write goes to a temp file
/// first and then replaces the slot, keeping the previous file as slot_&lt;n&gt;.json.bak.
/// </summary>
public sealed class PcSaveStorage : ISaveStorage
{
    private static readonly Regex SlotFile = new Regex(@"^slot_(\d+)\.json$", RegexOptions.IgnoreCase);
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public string Folder { get; }

    public PcSaveStorage() : this(Path.Combine(Application.persistentDataPath, "saves")) { }

    /// <summary>Use a specific folder (tests use a temp folder).</summary>
    public PcSaveStorage(string folder)
    {
        Folder = folder;
    }

    public string PathFor(int slot) => Path.Combine(Folder, $"slot_{slot}.json");
    public string BackupPathFor(int slot) => PathFor(slot) + ".bak";
    private string TempPathFor(int slot) => PathFor(slot) + ".tmp";

    public bool Exists(int slot) => File.Exists(PathFor(slot));

    public string Read(int slot)
    {
        string path = PathFor(slot);
        return File.Exists(path) ? File.ReadAllText(path, Utf8NoBom) : null;
    }

    public void Write(int slot, string text)
    {
        Directory.CreateDirectory(Folder);
        string path = PathFor(slot);
        string temp = TempPathFor(slot);

        File.WriteAllText(temp, text, Utf8NoBom);

        if (File.Exists(path))
            File.Replace(temp, path, BackupPathFor(slot)); // swaps in the new file, keeps the old as .bak
        else
            File.Move(temp, path);
    }

    public void Delete(int slot)
    {
        foreach (string path in new[] { PathFor(slot), BackupPathFor(slot), TempPathFor(slot) })
            if (File.Exists(path))
                File.Delete(path);
    }

    public IReadOnlyList<int> ListSlots()
    {
        var slots = new List<int>();
        if (!Directory.Exists(Folder))
            return slots;
        foreach (string file in Directory.GetFiles(Folder, "slot_*.json"))
        {
            Match m = SlotFile.Match(Path.GetFileName(file));
            if (m.Success && int.TryParse(m.Groups[1].Value, out int slot))
                slots.Add(slot);
        }
        slots.Sort();
        return slots;
    }

    public string Describe(int slot) => PathFor(slot);
}
