using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns ids from a save back into content. An id the ContentDatabase doesn't know (content was
/// renamed or removed) is logged as a warning and skipped; loading never fails because of it.
/// </summary>
public static class SaveRestore
{
    /// <summary>Cargo instances whose itemId resolves; each keeps its resolved definition.</summary>
    public static List<ItemInstance> ResolveCargo(IEnumerable<ItemInstance> saved, ContentDatabase content, string shipId)
    {
        var kept = new List<ItemInstance>();
        if (saved == null)
            return kept;

        foreach (ItemInstance item in saved)
        {
            if (item == null || string.IsNullOrEmpty(item.instanceId))
            {
                Debug.LogWarning($"[Save] {shipId}: skipped a cargo entry with no instance id.");
                continue;
            }
            if (content == null)
            {
                Debug.LogWarning($"[Save] {shipId}: no ContentDatabase, so cargo item '{item.itemId}' [{item.ShortId}] can't be checked; skipped.");
                continue;
            }
            if (item.Resolve(content) == null)
            {
                Debug.LogWarning($"[Save] {shipId}: unknown item id '{item.itemId}' [{item.ShortId}] in cargo; skipped.");
                continue;
            }
            item.shipId = shipId;
            kept.Add(item);
        }
        return kept;
    }

    /// <summary>The ShipUpgrade for an id, or null (with a warning) if it isn't in the database.</summary>
    public static ShipUpgrade ResolveUpgrade(string upgradeId, ContentDatabase content, string shipId)
    {
        if (content != null && content.TryGet(upgradeId, out ShipUpgrade upgrade))
            return upgrade;
        Debug.LogWarning($"[Save] {shipId}: unknown upgrade id '{upgradeId}'; skipped.");
        return null;
    }
}
