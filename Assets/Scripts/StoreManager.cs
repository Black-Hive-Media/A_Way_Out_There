using UnityEngine;
public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    [Tooltip("Fallback ship when none is docked. Empty = the only ship in the scene.")]
    [SerializeField] private Ship ship;

    [Tooltip("Unused: the laser now comes from the ship (Ship.Laser). Kept until the move is confirmed.")]
    public LaserController playerLaser;
    //[SerializeField] private FuelSystem playerFuel; // Hypothetical fuel script

    private void Awake() => Instance = this;

    /// <summary>The ship being served: the one docked at the port, else the fallback.</summary>
    public Ship CurrentShip
    {
        get
        {
            Ship docked = SpacePortManager.Instance != null ? SpacePortManager.Instance.DockedShip : null;
            if (docked != null)
                return docked;
            ship = Ship.Resolve(this, ship);
            return ship;
        }
    }

    public void AttemptPurchase(ShipUpgrade upgrade)
    {
        Ship buyer = CurrentShip;
        if (buyer == null || upgrade == null)
            return;

        if (buyer.TrySpendCredits(upgrade.cost))
        {
            ApplyUpgrade(buyer, upgrade);
            AI_Manager.Instance.TriggerMessage("PurchaseSuccess");
        }
        else
        {
            AI_Manager.Instance.TriggerMessage("InsufficientFunds");
        }
    }

    private void ApplyUpgrade(Ship buyer, ShipUpgrade upgrade)
    {
        switch (upgrade.type)
        {
            case ShipUpgrade.UpgradeType.Cargo:
                // Capacity = ship base + this upgrade's numericValue
                buyer.InstallCargoUpgrade(upgrade);
                break;

            case ShipUpgrade.UpgradeType.MiningLaser:
                // Swap the LaserStats ScriptableObject in the controller
                if (upgrade.upgradeData is MiningLaserStats newStats && buyer.Laser != null)
                {
                    buyer.Laser.UpdateLaserTier(newStats);
                }
                break;

            case ShipUpgrade.UpgradeType.Retractor:
                // Increase the pull force/range of your retractor beam
                // RetractorBeam.Instance.UpdateStrength(upgrade.numericValue);
                break;

            case ShipUpgrade.UpgradeType.Fuel:
                // Increase the fuel tank maximum
                // playerFuel.UpgradeTank(upgrade.numericValue);
                break;
        }
    }
}
