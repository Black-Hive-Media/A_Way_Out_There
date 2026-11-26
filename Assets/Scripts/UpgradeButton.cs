using System;
using UnityEngine;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    [Tooltip("Upgrade tiers in order, cheapest first.")]
    public ShipUpgrade[] myUpgrade;

    private TextMeshProUGUI textObj;

    public ShipUpgrade.UpgradeType upgradeType;

    private Ship CurrentShip => StoreManager.Instance != null ? StoreManager.Instance.CurrentShip : null;

    private void OnEnable()
    {
        //set next upgrade's name and amount
        if (textObj == null)
        {
            textObj = GetComponentInChildren<TextMeshProUGUI>();
        }

        ShipUpgrade next = FindNextUpgrade(out _);
        if (next != null && textObj != null)
        {
            textObj.text = next.name + " Cost: " + next.cost;
        }
    }

    public void OnClick()
    {
        ShipUpgrade next = FindNextUpgrade(out bool ownsAll);
        if (next != null)
        {
            StoreManager.Instance.AttemptPurchase(next);
        }
        else if (ownsAll)
        {
            AI_Manager.Instance.TriggerMessage("AlreadyOwnAllUpgrades");
        }
    }

    /// <summary>
    /// The next tier the current ship can buy, or null. ownsAll is true when the ship already has
    /// the last tier. A button with no upgrades assigned does nothing.
    /// </summary>
    private ShipUpgrade FindNextUpgrade(out bool ownsAll)
    {
        ownsAll = false;
        Ship ship = CurrentShip;
        if (ship == null || myUpgrade == null || myUpgrade.Length == 0)
            return null;

        int ownedIndex;
        switch (upgradeType)
        {
            case ShipUpgrade.UpgradeType.MiningLaser:
                if (ship.Laser == null || ship.Laser.currentStats == null)
                    return null;
                // The starting laser isn't in the list; until step 8 tiers are matched by asset name.
                if (ship.Laser.currentStats.name.CompareTo("Tier1Laser") == 0)
                    return myUpgrade[0];
                string current = ship.Laser.currentStats.name;
                ownedIndex = Array.FindIndex(myUpgrade, u => u != null && u.upgradeData != null && u.upgradeData.name == current);
                if (ownedIndex < 0)
                    return null; // same as before: an unknown laser offers nothing
                break;

            case ShipUpgrade.UpgradeType.Cargo:
                // -1 = base hold, so the first tier is next.
                ownedIndex = ship.CargoUpgrade == null ? -1 : Array.IndexOf(myUpgrade, ship.CargoUpgrade);
                break;

            default:
                return null; // Retractor and Fuel aren't buyable yet
        }

        int nextIndex = ownedIndex + 1;
        if (nextIndex < myUpgrade.Length)
            return myUpgrade[nextIndex];

        ownsAll = true;
        return null;
    }
}
