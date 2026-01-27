using UnityEngine;
using static MiningLaserStats;

public class AsteroidHandler : MonoBehaviour, IMineable
{
    [SerializeField] private float health = 5f;
    [SerializeField] private AsteriodSize asteriodSize;
    [SerializeField] private CollectibleItem internalItem; // The Item we mentioned earlier!
    bool didCheckMinable = false;
    public enum AsteriodSize
    {
        TinyAsteroids = 0,
        SmallAsteroids = 1,
        MediumAsteroids = 2,
        LargeAsteroids = 3
    }

    public bool OnMined(float damage, float yieldBonus, MiningLaserStats.MaxAsteriodSizeCapable maxAsteriodSize)
    {
        // Tier 2 Check: Can this laser even cut this rock?
        if (didCheckMinable == false)
        {
            didCheckMinable = true;

            if ((int)maxAsteriodSize < (int)asteriodSize)
            {
                // Optional: Trigger AI snark about needing a better laser
                AI_Manager.Instance.TriggerMessage("GetBetterLaser");
                return false;
            }
        }

        health -= damage;

        if (health <= 0)
        {
            DropItems(yieldBonus);
            ItemPoolManager.Instance.ReturnToPool(this.gameObject); // Clean up
            return true; // Tells the LaserController we are done
        }
        return false;
    }

    private void DropItems(float yieldBonus)
    {
        // Logic to spawn items using the Methodical Spawner/Pool logic
        // If Random.value < yieldBonus, spawn 2 items instead of 1
    }

}
