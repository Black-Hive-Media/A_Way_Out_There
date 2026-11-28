using System;
using System.Collections.Generic;

public enum LedgerEvent
{
    Collected,
    Mined,
    Sold,
    Jettisoned,
}

/// <summary>One thing that happened to one item instance.</summary>
[Serializable]
public class LedgerRow
{
    public string instanceId;
    /// <summary>Content id of the item, kept so rows can be rolled up after the instance is gone.</summary>
    public string itemId = "";
    public LedgerEvent eventType;
    public string fromShipId = "";
    public string toShipId = "";
    public string portId = "";
    public int price;
    public double simTime;

    public override string ToString()
    {
        string shortId = string.IsNullOrEmpty(instanceId) ? "?" : instanceId.Substring(0, Math.Min(8, instanceId.Length));
        string where = !string.IsNullOrEmpty(portId) ? $" at {portId}" : "";
        string who = !string.IsNullOrEmpty(fromShipId) ? fromShipId : "";
        if (!string.IsNullOrEmpty(toShipId))
            who += (who.Length > 0 ? " -> " : "-> ") + toShipId;
        string money = price != 0 ? $" for {price} CR" : "";
        return $"{simTime,7:0.0}s  {eventType,-10} {itemId} [{shortId}] {who}{where}{money}";
    }
}

/// <summary>Per-item totals for ledger rows that were rolled up (save table ItemHistoryTotals).</summary>
[Serializable]
public class ItemHistoryTotals
{
    public string itemId = "";
    public int countCollected;
    public int countMined;
    public int countSold;
    public int totalEarned;
    public int bestPrice;
}

/// <summary>
/// Every item event in the universe, oldest first. Full rows are kept only for items still owned;
/// RollUp folds the rest into per-item totals so the save stays small (Switch save space).
/// </summary>
[Serializable]
public class ItemLedger
{
    public List<LedgerRow> rows = new List<LedgerRow>();

    public LedgerRow Record(LedgerEvent eventType, string instanceId, string itemId, double simTime,
        string fromShipId = "", string toShipId = "", string portId = "", int price = 0)
    {
        var row = new LedgerRow
        {
            instanceId = instanceId,
            itemId = itemId ?? "",
            eventType = eventType,
            fromShipId = fromShipId ?? "",
            toShipId = toShipId ?? "",
            portId = portId ?? "",
            price = price,
            simTime = simTime,
        };
        rows.Add(row);
        return row;
    }

    /// <summary>The last count rows matching the filter (all rows if null), newest first.</summary>
    public List<LedgerRow> Latest(int count, Predicate<LedgerRow> filter = null)
    {
        var result = new List<LedgerRow>(count);
        for (int i = rows.Count - 1; i >= 0 && result.Count < count; i--)
            if (filter == null || filter(rows[i]))
                result.Add(rows[i]);
        return result;
    }

    /// <summary>
    /// Folds every row whose instance isn't in ownedInstanceIds into totals (one entry per itemId,
    /// added to any existing entry) and removes those rows. Returns how many rows were rolled up.
    /// </summary>
    public int RollUp(ICollection<string> ownedInstanceIds, List<ItemHistoryTotals> totals)
    {
        if (totals == null)
            throw new ArgumentNullException(nameof(totals));

        var byItem = new Dictionary<string, ItemHistoryTotals>();
        foreach (ItemHistoryTotals t in totals)
            if (t != null && !byItem.ContainsKey(t.itemId ?? ""))
                byItem[t.itemId ?? ""] = t;

        int rolled = 0;
        rows.RemoveAll(row =>
        {
            if (row == null)
                return true;
            if (ownedInstanceIds != null && ownedInstanceIds.Contains(row.instanceId))
                return false;

            string key = row.itemId ?? "";
            if (!byItem.TryGetValue(key, out ItemHistoryTotals t))
            {
                t = new ItemHistoryTotals { itemId = key };
                byItem[key] = t;
                totals.Add(t);
            }
            switch (row.eventType)
            {
                case LedgerEvent.Collected: t.countCollected++; break;
                case LedgerEvent.Mined: t.countMined++; break;
                case LedgerEvent.Sold:
                    t.countSold++;
                    t.totalEarned += row.price;
                    t.bestPrice = Math.Max(t.bestPrice, row.price);
                    break;
            }
            rolled++;
            return true;
        });
        return rolled;
    }
}
