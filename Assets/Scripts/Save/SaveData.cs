using System;
using System.Collections.Generic;

/// <summary>
/// Root of a save file. Holds content only by id (never asset references), so content can
/// change without breaking saves. Serialized with Newtonsoft through SaveJson.
/// </summary>
[Serializable]
public class SaveData
{
    public int schemaVersion = SaveMigrator.CurrentVersion;
    /// <summary>ISO 8601 UTC time of the save.</summary>
    public string savedAtUtc = "";
    public UniverseState universe = new UniverseState();
    public List<ShipSave> ships = new List<ShipSave>();
}

/// <summary>A ship's state plus the parts of the Ship record that aren't in ShipState yet.</summary>
[Serializable]
public class ShipSave
{
    public ShipState state = new ShipState();
    public string systemId = "";
    public SavedVector2 position;
    /// <summary>Z rotation in degrees.</summary>
    public float rotation;
    public List<InstalledUpgrade> upgrades = new List<InstalledUpgrade>();
}

/// <summary>Plain x/y pair (UnityEngine.Vector2 has computed properties that don't serialize cleanly).</summary>
[Serializable]
public struct SavedVector2
{
    public float x;
    public float y;

    public SavedVector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}

/// <summary>Upgrade slots from the design doc (UpgradeDefinition.slot).</summary>
public enum UpgradeSlot
{
    Cargo,
    MiningLaser,
    Tractor,
    Thrusters,
    Hyperdrive,
    FuelTank,
    Cosmetic,
}

public enum PartSource
{
    Bought,
    Salvaged,
}

/// <summary>One installed part (save table InstalledUpgrade). One per slot per ship.</summary>
[Serializable]
public class InstalledUpgrade
{
    public string shipId = "";
    public UpgradeSlot slot;
    public string upgradeId = "";
    /// <summary>0..1.</summary>
    public float condition = 1f;
    /// <summary>What the crew paid (or list cost x salvage rate for salvaged parts).</summary>
    public int tradeInBasis;
    public PartSource source = PartSource.Bought;
    /// <summary>Sim time it was installed.</summary>
    public double installedAt;

    /// <summary>The design-doc slot for one of today's ShipUpgrade types.</summary>
    public static UpgradeSlot SlotFor(ShipUpgrade.UpgradeType type) => type switch
    {
        ShipUpgrade.UpgradeType.Cargo => UpgradeSlot.Cargo,
        ShipUpgrade.UpgradeType.MiningLaser => UpgradeSlot.MiningLaser,
        ShipUpgrade.UpgradeType.Retractor => UpgradeSlot.Tractor,
        ShipUpgrade.UpgradeType.Fuel => UpgradeSlot.FuelTank,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
