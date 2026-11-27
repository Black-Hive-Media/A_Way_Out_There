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
        return $"{simTime,7:0.0}s  {eventType,-10} [{shortId}] {who}{where}{money}";
    }
}

/// <summary>Every item event in the universe, oldest first. Roll-up of old rows comes later.</summary>
[Serializable]
public class ItemLedger
{
    public List<LedgerRow> rows = new List<LedgerRow>();

    public LedgerRow Record(LedgerEvent eventType, string instanceId, double simTime,
        string fromShipId = "", string toShipId = "", string portId = "", int price = 0)
    {
        var row = new LedgerRow
        {
            instanceId = instanceId,
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
}
