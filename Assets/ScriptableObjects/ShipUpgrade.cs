using UnityEngine;
[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Systems/Upgrade")]
public class ShipUpgrade : ScriptableObject
{
    public string upgradeName;
    public int cost;
    public UpgradeType type;

    // For Tiers: The LaserStats or new Cargo capacity value
    public ScriptableObject upgradeData;
    public float numericValue;

    public enum UpgradeType { Cargo, MiningLaser, Retractor, Fuel }
}
