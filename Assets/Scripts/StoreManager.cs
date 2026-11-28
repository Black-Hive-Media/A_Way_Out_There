using UnityEngine;
public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    [Tooltip("Fallback ship when none is docked. Empty = the only ship in the scene.")]
    [SerializeField] private Ship ship;

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
            // Installs into its slot and applies it (capacity, laser stats); keeps an InstalledUpgrade record
            if (!buyer.InstallUpgrade(upgrade))
            {
                buyer.AddCredits(upgrade.cost); // nothing was installed, so give the money back
                return;
            }
            // Cargo and laser upgrades say their own line (CargoUpgraded / LaserUpgraded);
            // a second message here would replace it before it's shown.
            if (upgrade.type != ShipUpgrade.UpgradeType.Cargo && upgrade.type != ShipUpgrade.UpgradeType.MiningLaser)
                AI_Manager.Instance.TriggerMessage("PurchaseSuccess");
            GameSession.Autosave("purchase");
        }
        else
        {
            AI_Manager.Instance.TriggerMessage("InsufficientFunds");
        }
    }
}
