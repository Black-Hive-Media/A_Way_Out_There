using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ship in the world. Owns its ShipState (credits, fuel, hull, cargo) and is the only way
/// to change it. HUD and port scripts bind to a Ship and listen to its events.
/// Goes on the root of the player ship (PlayerShip in GameScene).
/// </summary>
[DisallowMultipleComponent]
public class Ship : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string shipId = "ship_1";
    [SerializeField] private string shipName = "Ship 1";
    [Tooltip("ShipHull content id; hull assets come with upgrades (step 8).")]
    [SerializeField] private string hullId = "";

    [Header("Starting values")]
    [Tooltip("Credits the crew starts with. Replaces the PlayerWallet asset at runtime.")]
    [SerializeField, Min(0)] private int startingCredits = 40;
    [SerializeField, Min(0)] private float startingFuel = 100f;

    [Header("Cargo")]
    [Tooltip("Cargo units before any Cargo upgrade.")]
    [SerializeField, Min(0)] private int baseCargoUnits = 3;

    private ShipState state;
    private LaserController laser;
    private MiningLaserStats startingLaserStats;

    /// <summary>Installed upgrade per slot: the content asset and its save record.</summary>
    private readonly Dictionary<UpgradeSlot, (ShipUpgrade upgrade, InstalledUpgrade record)> installed =
        new Dictionary<UpgradeSlot, (ShipUpgrade, InstalledUpgrade)>();

    /// <summary>(new balance, change). Change is negative when spending.</summary>
    public event Action<int, int> CreditsChanged;
    /// <summary>An item instance came aboard.</summary>
    public event Action<ItemInstance> CargoAdded;
    /// <summary>An item instance left the ship (sold, jettisoned...).</summary>
    public event Action<ItemInstance> CargoRemoved;
    /// <summary>An item did not fit; it was not added.</summary>
    public event Action<ItemInstance> CargoRejected;
    /// <summary>(new cargo_units_max).</summary>
    public event Action<int> CargoCapacityChanged;
    /// <summary>The whole state was replaced (load or new game); views should redraw everything.</summary>
    public event Action StateReplaced;

    public ShipState State
    {
        get
        {
            EnsureState();
            return state;
        }
    }

    public string Id => State.id;
    public int Credits => State.credits;
    public int CargoUnitsUsed => State.CargoUnitsUsed;
    public int CargoUnitsMax => State.cargoUnitsMax;
    public bool IsCargoFull => State.IsCargoFull;

    /// <summary>The installed Cargo upgrade, or null for the base hold.</summary>
    public ShipUpgrade CargoUpgrade => GetInstalled(UpgradeSlot.Cargo);

    /// <summary>The upgrade installed in a slot, or null.</summary>
    public ShipUpgrade GetInstalled(UpgradeSlot slot) => installed.TryGetValue(slot, out var entry) ? entry.upgrade : null;

    /// <summary>Save records for every installed upgrade (InstalledUpgrade rows).</summary>
    public List<InstalledUpgrade> InstalledUpgrades
    {
        get
        {
            var list = new List<InstalledUpgrade>();
            foreach (var entry in installed.Values)
                list.Add(entry.record);
            list.Sort((a, b) => a.slot.CompareTo(b.slot));
            return list;
        }
    }

    /// <summary>This ship's mining laser (on a child object).</summary>
    public LaserController Laser
    {
        get
        {
            if (laser == null)
                laser = GetComponentInChildren<LaserController>(true);
            return laser;
        }
    }

    private void Awake()
    {
        EnsureState();
        if (Laser != null)
            startingLaserStats = Laser.currentStats;
    }

    private void EnsureState()
    {
        if (state != null)
            return;
        state = NewStartingState();
    }

    private ShipState NewStartingState() => new ShipState
    {
        id = shipId,
        hullId = hullId,
        name = shipName,
        credits = startingCredits,
        fuel = startingFuel,
        hullCondition = 1f,
        cargoUnitsMax = baseCargoUnits,
    };

    // ---------------------------------------------------------------- Credits

    public void AddCredits(int amount)
    {
        if (amount <= 0)
            return;
        State.credits += amount;
        CreditsChanged?.Invoke(state.credits, amount);
    }

    /// <summary>Spends the credits if the wallet has enough. Returns false (and spends nothing) otherwise.</summary>
    public bool TrySpendCredits(int cost)
    {
        if (cost < 0 || State.credits < cost)
            return false;
        state.credits -= cost;
        CreditsChanged?.Invoke(state.credits, -cost);
        return true;
    }

    // ---------------------------------------------------------------- Cargo

    public bool CanFit(ItemInstance item) =>
        item != null && State.CargoUnitsUsed + item.CargoUnits <= state.cargoUnitsMax;

    public bool CanFit(CollectibleItem item) =>
        item != null && State.CargoUnitsUsed + item.cargoUnitsRequired <= state.cargoUnitsMax;

    /// <summary>
    /// Puts the instance aboard if it fits and isn't already aboard; sets its shipId.
    /// Raises CargoAdded, or CargoRejected if it doesn't fit.
    /// </summary>
    public bool AddCargo(ItemInstance item)
    {
        if (item == null || State.FindCargo(item.instanceId) != null)
            return false;
        if (!CanFit(item))
        {
            CargoRejected?.Invoke(item);
            return false;
        }
        item.shipId = state.id;
        state.cargo.Add(item);
        CargoAdded?.Invoke(item);
        return true;
    }

    /// <summary>
    /// Convenience: makes a new instance of a content item (current system, current sim time)
    /// and puts it aboard. Returns the instance, or null if it didn't fit.
    /// </summary>
    public ItemInstance AddCargo(CollectibleItem item, AcquisitionSource source)
    {
        if (item == null)
            return null;
        ItemInstance instance = ItemInstance.Create(item, source);
        return AddCargo(instance) ? instance : null;
    }

    /// <summary>Takes the instance with this id off the ship and returns it, or null if it isn't aboard.</summary>
    public ItemInstance RemoveCargo(string instanceId)
    {
        ItemInstance item = State.FindCargo(instanceId);
        if (item == null)
            return null;
        state.cargo.Remove(item);
        item.shipId = "";
        CargoRemoved?.Invoke(item);
        return item;
    }

    public ItemInstance FindCargo(string instanceId) => State.FindCargo(instanceId);

    // ---------------------------------------------------------------- Upgrades

    /// <summary>
    /// Installs an upgrade into its slot, replacing what was there, and applies it (cargo capacity,
    /// laser stats). announce = false skips the AI line and HUD events (used when loading).
    /// restoreRecord keeps a loaded InstalledUpgrade row instead of making a new one.
    /// Returns false for upgrade types that can't be installed yet.
    /// </summary>
    public bool InstallUpgrade(ShipUpgrade upgrade, bool announce = true, InstalledUpgrade restoreRecord = null)
    {
        if (upgrade == null)
            return false;

        UpgradeSlot slot = InstalledUpgrade.SlotFor(upgrade.type);
        switch (upgrade.type)
        {
            case ShipUpgrade.UpgradeType.Cargo:
                SetCargoCapacity(upgrade, announce);
                break;

            case ShipUpgrade.UpgradeType.MiningLaser:
                if (!(upgrade.upgradeData is MiningLaserStats stats) || Laser == null)
                {
                    Debug.LogWarning($"{upgrade.name}: no laser stats or no laser on {name}; not installed.", upgrade);
                    return false;
                }
                Laser.UpdateLaserTier(stats, announce);
                break;

            default: // Retractor and Fuel have nothing to apply yet
                Debug.LogWarning($"{upgrade.name}: {upgrade.type} upgrades can't be installed yet.", upgrade);
                return false;
        }

        GameSession session = GameSession.Current;
        InstalledUpgrade record = restoreRecord ?? new InstalledUpgrade
        {
            condition = 1f,
            tradeInBasis = upgrade.cost,
            source = PartSource.Bought,
            installedAt = session != null ? session.Clock.Now : 0,
        };
        record.shipId = State.id;
        record.slot = slot;
        record.upgradeId = upgrade.Id;
        installed[slot] = (upgrade, record);
        return true;
    }

    /// <summary>
    /// Installs a Cargo upgrade: capacity = base + the upgrade's amount (numericValue).
    /// It replaces the previous Cargo upgrade; it doesn't stack. Null goes back to the base hold.
    /// </summary>
    public void InstallCargoUpgrade(ShipUpgrade upgrade)
    {
        if (upgrade != null && upgrade.type != ShipUpgrade.UpgradeType.Cargo)
        {
            Debug.LogWarning($"{upgrade.name} is not a Cargo upgrade.", upgrade);
            return;
        }
        if (upgrade == null)
        {
            installed.Remove(UpgradeSlot.Cargo);
            SetCargoCapacity(null, announce: true);
            return;
        }
        InstallUpgrade(upgrade);
    }

    private void SetCargoCapacity(ShipUpgrade upgrade, bool announce)
    {
        State.cargoUnitsMax = baseCargoUnits + (upgrade != null ? Mathf.RoundToInt(upgrade.numericValue) : 0);
        if (announce)
            CargoCapacityChanged?.Invoke(state.cargoUnitsMax);
    }

    // ---------------------------------------------------------------- Save / load

    /// <summary>This ship as a save record: its state, where it is, and its installed upgrades.</summary>
    public ShipSave CaptureSave(string systemId)
    {
        var rb = GetComponent<Rigidbody2D>();
        Vector2 position = rb != null ? rb.position : (Vector2)transform.position;
        float rotation = rb != null ? rb.rotation : transform.eulerAngles.z;
        return new ShipSave
        {
            state = State,
            systemId = systemId ?? "",
            position = new SavedVector2(position.x, position.y),
            rotation = rotation,
            upgrades = InstalledUpgrades,
        };
    }

    /// <summary>
    /// Replaces this ship's state with a loaded one. Cargo and upgrade ids are resolved through the
    /// content database; unknown ids are logged and skipped. Upgrades are re-applied through
    /// InstallUpgrade so stats match a ship that bought them.
    /// </summary>
    public void RestoreSave(ShipSave save, ContentDatabase content)
    {
        ShipState saved = save != null && save.state != null ? save.state : NewStartingState();

        ResetUpgrades();
        state = new ShipState
        {
            id = shipId, // the scene ship keeps its id
            hullId = saved.hullId ?? hullId,
            name = string.IsNullOrEmpty(saved.name) ? shipName : saved.name,
            credits = Mathf.Max(0, saved.credits),
            fuel = saved.fuel,
            hullCondition = Mathf.Clamp01(saved.hullCondition),
            cargoUnitsMax = baseCargoUnits,
            cargo = SaveRestore.ResolveCargo(saved.cargo, content, shipId),
        };

        if (save != null && save.upgrades != null)
        {
            foreach (InstalledUpgrade record in save.upgrades)
            {
                if (record == null)
                    continue;
                ShipUpgrade upgrade = SaveRestore.ResolveUpgrade(record.upgradeId, content, shipId);
                if (upgrade != null)
                    InstallUpgrade(upgrade, announce: false, restoreRecord: record);
            }
        }

        if (save != null)
            PlaceAt(new Vector2(save.position.x, save.position.y), save.rotation);

        StateReplaced?.Invoke();
    }

    /// <summary>Back to the Inspector's starting values (new game). Position is left as it is.</summary>
    public void ResetToStart()
    {
        ResetUpgrades();
        state = NewStartingState();
        StateReplaced?.Invoke();
    }

    private void ResetUpgrades()
    {
        installed.Clear();
        if (Laser != null && startingLaserStats != null)
            Laser.UpdateLaserTier(startingLaserStats, announce: false);
    }

    private void PlaceAt(Vector2 position, float rotation)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = position;
            rb.rotation = rotation;
        }
        transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.Euler(0f, 0f, rotation));
    }

    // ---------------------------------------------------------------- Binding

    private static bool warnedFallback;

    /// <summary>
    /// The ship a HUD or port component should use: the one assigned in the Inspector, else a Ship
    /// on a parent, else the first Ship in the scene (fine while there is one ship; split-screen in
    /// step 11 assigns them explicitly).
    /// </summary>
    public static Ship Resolve(Component requester, Ship assigned)
    {
        if (assigned != null)
            return assigned;

        Ship ship = requester != null ? requester.GetComponentInParent<Ship>() : null;
        if (ship != null)
            return ship;

        ship = FindAnyObjectByType<Ship>();
        if (ship == null)
            Debug.LogError($"{(requester ? requester.name : "?")}: no Ship component in the scene. Add Ship to the PlayerShip object.", requester);
        else if (!warnedFallback)
        {
            warnedFallback = true;
            Debug.Log($"[Ship] Components without an assigned Ship are using '{ship.name}'.", ship);
        }
        return ship;
    }
}
