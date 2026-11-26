using System;
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
    private ShipUpgrade cargoUpgrade;
    private LaserController laser;

    /// <summary>(new balance, change). Change is negative when spending.</summary>
    public event Action<int, int> CreditsChanged;
    /// <summary>(item, index it was added at).</summary>
    public event Action<CollectibleItem, int> CargoAdded;
    /// <summary>(item, index it was removed from).</summary>
    public event Action<CollectibleItem, int> CargoRemoved;
    /// <summary>An item did not fit.</summary>
    public event Action<CollectibleItem> CargoRejected;
    /// <summary>(new cargo_units_max).</summary>
    public event Action<int> CargoCapacityChanged;

    public ShipState State
    {
        get
        {
            EnsureState();
            return state;
        }
    }

    public int Credits => State.credits;
    public int CargoUnitsUsed => State.CargoUnitsUsed;
    public int CargoUnitsMax => State.cargoUnitsMax;
    public bool IsCargoFull => State.IsCargoFull;

    /// <summary>The installed Cargo upgrade, or null for the base hold.</summary>
    public ShipUpgrade CargoUpgrade => cargoUpgrade;

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

    private void Awake() => EnsureState();

    private void EnsureState()
    {
        if (state != null)
            return;
        state = new ShipState
        {
            id = shipId,
            hullId = hullId,
            name = shipName,
            credits = startingCredits,
            fuel = startingFuel,
            hullCondition = 1f,
            cargoUnitsMax = baseCargoUnits,
        };
    }

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

    public bool CanFit(CollectibleItem item) =>
        item != null && State.CargoUnitsUsed + item.cargoUnitsRequired <= state.cargoUnitsMax;

    /// <summary>Adds the item if it fits. Raises CargoAdded, or CargoRejected if it doesn't fit.</summary>
    public bool AddCargo(CollectibleItem item)
    {
        if (item == null)
            return false;
        if (!CanFit(item))
        {
            CargoRejected?.Invoke(item);
            return false;
        }
        state.cargo.Add(item);
        CargoAdded?.Invoke(item, state.cargo.Count - 1);
        return true;
    }

    /// <summary>Removes the cargo entry at index and returns it, or null if the index is out of range.</summary>
    public CollectibleItem RemoveCargoAt(int index)
    {
        if (index < 0 || index >= State.cargo.Count)
            return null;
        CollectibleItem item = state.cargo[index];
        state.cargo.RemoveAt(index);
        CargoRemoved?.Invoke(item, index);
        return item;
    }

    /// <summary>Removes the last cargo entry for this item. Returns false if the ship isn't carrying it.</summary>
    public bool RemoveCargo(CollectibleItem item)
    {
        int index = State.cargo.LastIndexOf(item);
        return index >= 0 && RemoveCargoAt(index) != null;
    }

    // ---------------------------------------------------------------- Upgrades

    /// <summary>
    /// Installs a Cargo upgrade: capacity = base + the upgrade's amount (numericValue).
    /// It replaces the previous Cargo upgrade; it doesn't stack.
    /// </summary>
    public void InstallCargoUpgrade(ShipUpgrade upgrade)
    {
        if (upgrade != null && upgrade.type != ShipUpgrade.UpgradeType.Cargo)
        {
            Debug.LogWarning($"{upgrade.name} is not a Cargo upgrade.", upgrade);
            return;
        }
        cargoUpgrade = upgrade;
        State.cargoUnitsMax = baseCargoUnits + (upgrade != null ? Mathf.RoundToInt(upgrade.numericValue) : 0);
        CargoCapacityChanged?.Invoke(state.cargoUnitsMax);
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
