using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Saves and loads the GameSession's universe and every Ship in the scene through ISaveStorage.
/// Owned by GameSession (GameSession.Saves).
/// </summary>
public sealed class SaveService
{
    private readonly GameSession session;

    public ISaveStorage Storage { get; }

    /// <summary>Why the last Save or Load failed, or null.</summary>
    public string LastError { get; private set; }

    public SaveService(GameSession session, ISaveStorage storage)
    {
        this.session = session;
        Storage = storage;
    }

    private static Ship[] SceneShips() =>
        UnityEngine.Object.FindObjectsByType<Ship>().OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();

    // ---------------------------------------------------------------- Save

    /// <summary>Rolls up the ledger, then writes the universe and every ship to the slot.</summary>
    public bool Save(int slot)
    {
        LastError = null;
        try
        {
            Ship[] ships = SceneShips();

            // Bound the ledger: rows for items no ship owns any more become per-item totals.
            var owned = new HashSet<string>(ships.SelectMany(s => s.State.cargo).Where(i => i != null).Select(i => i.instanceId));
            int rolled = session.Ledger.RollUp(owned, session.Universe.itemHistory);

            var data = new SaveData
            {
                schemaVersion = SaveMigrator.CurrentVersion,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                universe = session.Universe,
                ships = ships.Select(s => s.CaptureSave(session.CurrentSystemId)).ToList(),
            };

            string json = SaveJson.Serialize(data);
            Storage.Write(slot, json);

            float kb = System.Text.Encoding.UTF8.GetByteCount(json) / 1024f;
            Debug.Log($"[Save] Slot {slot} saved: {kb:0.0} KB, {data.ships.Count} ship(s), {session.Ledger.rows.Count} ledger row(s)" +
                      (rolled > 0 ? $", {rolled} rolled up into item totals" : "") + $". {Storage.Describe(slot)}");
            return true;
        }
        catch (Exception e)
        {
            LastError = $"Saving slot {slot} failed: {e.Message}";
            Debug.LogError("[Save] " + LastError + "\n" + e);
            return false;
        }
    }

    // ---------------------------------------------------------------- Load

    /// <summary>
    /// Reads, migrates and applies the slot. Returns false (and changes nothing) if the slot is
    /// missing, unreadable, or newer than this build.
    /// </summary>
    public bool Load(int slot)
    {
        LastError = null;
        if (!Storage.Exists(slot))
        {
            LastError = $"Slot {slot} has no save.";
            return false;
        }

        SaveData data;
        try
        {
            data = SaveJson.Deserialize(Storage.Read(slot));
        }
        catch (SaveVersionException e)
        {
            LastError = e.Message;
            Debug.LogError($"[Save] Slot {slot}: {e.Message}");
            return false;
        }
        catch (Exception e) when (e is JsonException || e is FormatException || e is InvalidCastException || e is ArgumentException)
        {
            LastError = $"Slot {slot} couldn't be read ({e.Message}). The file was left untouched.";
            Debug.LogError("[Save] " + LastError);
            return false;
        }

        Apply(data);
        Debug.Log($"[Save] Slot {slot} loaded (saved {data.savedAtUtc}, sim time {session.Clock.Now:0.0}s).");
        return true;
    }

    /// <summary>Puts loaded data into the session and the scene's ships.</summary>
    public void Apply(SaveData data)
    {
        session.SetUniverse(data.universe ?? new UniverseState());

        ContentDatabase content = session.Content;
        if (content == null)
            Debug.LogWarning("[Save] GameSession has no Content Database, so saved items and upgrades can't be restored.");

        var savedById = new Dictionary<string, ShipSave>(StringComparer.Ordinal);
        foreach (ShipSave s in data.ships ?? new List<ShipSave>())
            if (s?.state?.id != null && !savedById.ContainsKey(s.state.id))
                savedById[s.state.id] = s;

        foreach (Ship ship in SceneShips())
        {
            if (savedById.TryGetValue(ship.Id, out ShipSave save))
            {
                ship.RestoreSave(save, content);
                savedById.Remove(ship.Id);
            }
            else
            {
                Debug.LogWarning($"[Save] No saved state for ship '{ship.Id}'; it starts fresh.", ship);
                ship.ResetToStart();
            }
        }

        foreach (string id in savedById.Keys)
            Debug.LogWarning($"[Save] The save has a ship '{id}' that isn't in the scene; skipped.");
    }

    // ---------------------------------------------------------------- New game

    /// <summary>Fresh universe and starting ships, written to the slot.</summary>
    public bool NewGame(int slot)
    {
        session.SetUniverse(new UniverseState());
        foreach (Ship ship in SceneShips())
            ship.ResetToStart();
        Debug.Log($"[Save] New game in slot {slot}.");
        return Save(slot);
    }
}
