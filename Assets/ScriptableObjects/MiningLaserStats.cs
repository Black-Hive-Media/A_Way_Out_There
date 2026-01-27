using UnityEngine;


[CreateAssetMenu(fileName = "MiningLaserStats", menuName = "Mining/LaserStats")]
public class MiningLaserStats : ScriptableObject
{
    public float miningSpeed = 1f;
    public float beamWobble = 0.5f;
    public float bonusYieldChance = 0f;
    public MaxAsteriodSizeCapable maxAsteriodSizeCapable;// = new MaxAsteriodSizeCapable();
    public LayerMask validTargets; // For Tier 2: Bio-Safe/Organic

    public enum MaxAsteriodSizeCapable
    {
        TinyAsteroids = 0,
        SmallAsteroids = 1,
        MediumAsteroids = 2,
        LargeAsteroids = 3
    }

}