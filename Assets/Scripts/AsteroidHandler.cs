using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEditor.Overlays;
using UnityEngine;
using static MiningLaserStats;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class AsteroidHandler : MonoBehaviour, IMineable
{
    [Header("Drop Settings")]
    [SerializeField] private List<LootEntry> lootTable;
    [SerializeField] private int minDropCount = 1;
    [SerializeField] private int maxDropCount = 3;
    [SerializeField] private float scatterForce = 20f;

    [SerializeField] private float health = 5f;
    [SerializeField] private AsteriodSize asteriodSize;
    [SerializeField] private CollectibleItem internalItem; 
    bool didCheckMinable = false;

    private float currentHealth;
    private bool isDead = false;
    
    [Header("Movement Settings")]
    public float maxDriftSpeed = 2f;
    public float maxTumbleSpeed = 15f;

    private Rigidbody2D rb;
    private float noiseOffset;
    public enum AsteriodSize
    {
        TinyAsteroids = 0,
        SmallAsteroids = 1,
        MediumAsteroids = 2,
        LargeAsteroids = 3
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void OnEnable()
    {
        currentHealth = health;
        isDead = false;

        // 2. Generate a unique seed for this specific "life" of the asteroid
        noiseOffset = Random.Range(0f, 1000f);

        // 3. Give it an initial unique nudge
        // We use a small random vector so they don't all drift the same way
        Vector2 randomNudge = Random.insideUnitCircle * maxDriftSpeed;
        rb.linearVelocity = randomNudge;

        // 4. Give it a random starting rotation speed (Tumble)
        rb.angularVelocity = Random.Range(-maxTumbleSpeed, maxTumbleSpeed);

    }
    void FixedUpdate()
    {
        // 5. Add a "Deep Space Drift"
        // This uses Perlin noise to make the asteroid's path curve slightly over time
        // so it isn't just a boring straight line.
        float noiseX = Mathf.PerlinNoise(Time.time * 0.1f, noiseOffset) - 0.5f;
        float noiseY = Mathf.PerlinNoise(noiseOffset, Time.time * 0.1f) - 0.5f;

        rb.AddForce(new Vector2(noiseX, noiseY) * 0.2f);
    }
    public bool OnMined(float damage, float yieldBonus, MiningLaserStats.MaxAsteriodSizeCapable maxAsteriodSize)
    {
        if (isDead) return false;

         
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

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            isDead = true;
            DropItems(yieldBonus);
            ItemPoolManager.Instance.ReleaseAsteroid(this.gameObject); // Clean up
            
            return true; // Tells the LaserController we are done
        }
        return false;
    }

    private void DropItems(float yieldBonus)
    {
        // 1. Determine how many items to drop
        // Larger asteroids could have higher base min/max values
        int baseCount = Random.Range(minDropCount, maxDropCount + 1);

        // Apply yield bonus (e.g., Tier 1 Soft Cut Laser)
        int finalCount = Mathf.FloorToInt(baseCount * (1f + yieldBonus));

        for (int i = 0; i < finalCount; i++)
        {
            CollectibleItem selectedItem = GetWeightedRandomItem();
            if (selectedItem != null)
            {
                SpawnDroppedItem(selectedItem);
            }
        }

    }

    private CollectibleItem GetWeightedRandomItem()
    {
        int totalWeight = 0;
        foreach (var entry in lootTable) totalWeight += entry.weight;

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var entry in lootTable)
        {
            currentWeight += entry.weight;
            if (randomValue < currentWeight) return entry.item;
        }
        return null;
    }

    private void SpawnDroppedItem(CollectibleItem itemData)
    {
        // Use your existing Pool Manager
        GameObject itemObj = ItemPoolManager.Instance.GetItem();
       
        if (itemObj == null)
        {
            Debug.LogError("SpawnDroppedItem: Pool returned null!");
            return;
        }

        itemObj.transform.position = transform.position;
        Debug.Log(itemObj.gameObject.name);
        // Setup visuals and data
        //if (itemObj.GetComponent<ItemWorldDisplay>() != null) 
        //{//error on mining here
        itemObj.GetComponent<ItemWorldDisplay>().Setup(itemData);
        //}

        // Add a "scatter" effect so they don't just stack in one spot
        Rigidbody2D rb = itemObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            rb.AddForce(randomDir * scatterForce, ForceMode2D.Impulse);
        }
    }

}
