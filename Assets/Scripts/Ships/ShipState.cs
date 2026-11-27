using System;
using System.Collections.Generic;

/// <summary>
/// Everything a ship owns that changes during play: wallet, fuel, hull, cargo.
/// Plain C# so it can be written to the save file later (step 5). Ship is the only class
/// that should change it; everything else reads it or goes through Ship's methods.
/// </summary>
[Serializable]
public class ShipState
{
    public string id;
    public string hullId;
    public string name;

    /// <summary>The crew's shared wallet.</summary>
    public int credits;
    public float fuel;

    /// <summary>0..1 (1 = undamaged).</summary>
    public float hullCondition = 1f;

    /// <summary>Hull base + installed Cargo upgrade.</summary>
    public int cargoUnitsMax;

    /// <summary>One entry per physical item aboard.</summary>
    public List<ItemInstance> cargo = new List<ItemInstance>();

    public int CargoUnitsUsed
    {
        get
        {
            int used = 0;
            foreach (ItemInstance item in cargo)
                if (item != null)
                    used += item.CargoUnits;
            return used;
        }
    }

    public int CargoUnitsFree => Math.Max(0, cargoUnitsMax - CargoUnitsUsed);
    public bool IsCargoFull => CargoUnitsUsed >= cargoUnitsMax;

    public ItemInstance FindCargo(string instanceId) =>
        string.IsNullOrEmpty(instanceId) ? null : cargo.Find(i => i != null && i.instanceId == instanceId);
}
