using UnityEngine;
public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    // References to your system controllers
    public LaserController playerLaser;
    //[SerializeField] private FuelSystem playerFuel; // Hypothetical fuel script

    private void Awake() => Instance = this;

    public void AttemptPurchase(ShipUpgrade upgrade)
    {
        // 1. Check with WalletManager
        if (WalletManager.Instance.TrySpendCredits(upgrade.cost))
        {
            ApplyUpgrade(upgrade);
            AI_Manager.Instance.TriggerMessage("PurchaseSuccess");
        }
        else
        {
            // WalletManager already triggers "InsufficientFunds" via its internal logic
        }
    }

    private void ApplyUpgrade(ShipUpgrade upgrade)
    {
        switch (upgrade.type)
        {
            case ShipUpgrade.UpgradeType.Cargo:
                // Cast numericValue to int for cargo capacity
                CargoManager.Instance.UpgradeCargoCapacity((int)upgrade.numericValue);
                break;

            case ShipUpgrade.UpgradeType.MiningLaser:
                // Swap the LaserStats ScriptableObject in the controller
                if (upgrade.upgradeData is MiningLaserStats newStats)
                {
                    playerLaser.UpdateLaserTier(newStats);
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
