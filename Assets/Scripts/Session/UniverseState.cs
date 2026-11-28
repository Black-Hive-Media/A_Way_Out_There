using System;
using System.Collections.Generic;

/// <summary>
/// The shared universe of one save slot: sim clock, item ledger and rolled-up item history.
/// Saved inside SaveData; markets, NPCs and the rest of the Save data table join it later.
/// </summary>
[Serializable]
public class UniverseState
{
    public SimClock clock = new SimClock();
    public ItemLedger ledger = new ItemLedger();
    /// <summary>Totals for ledger rows of items no longer owned (see ItemLedger.RollUp).</summary>
    public List<ItemHistoryTotals> itemHistory = new List<ItemHistoryTotals>();
}
