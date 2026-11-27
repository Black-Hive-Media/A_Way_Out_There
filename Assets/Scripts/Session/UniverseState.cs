using System;

/// <summary>
/// The shared universe of one save slot. Plain C#; step 5 writes it to the save file and adds
/// ships, markets and the rest of the Save data table.
/// </summary>
[Serializable]
public class UniverseState
{
    public int schemaVersion = 1;
    public SimClock clock = new SimClock();
    public ItemLedger ledger = new ItemLedger();
}
