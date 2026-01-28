using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class CollectibleItemSpawner : MonoBehaviour
{
    public GameObject ship;
    [Header("Spawn Controls")]
    public float distanceBetweenSpawnChecks = 50f;
    public float minDistanceFromPlayer = 20f;
    public float maxDistanceFromPlayer = 40f;
    public float itemSpacingRadius = 15f; // "Poisson" spacing

    [Header("Databases")]
    public List<CollectibleItem> itemPool; // All possible ScriptableObjects
    public CollectibleItem.SolarSystemFound currentSystem;

    private Vector2 lastSpawnPosition;

    void Update()
    {
        float distanceMoved = Vector2.Distance(ship.transform.position, lastSpawnPosition);
        //Debug.Log(distanceMoved + " dist moved " + "  >= " + distanceBetweenSpawnChecks);
        if (distanceMoved >= distanceBetweenSpawnChecks)
        {
            TrySpawnItem();
            lastSpawnPosition = transform.position;
        }
    }

    void TrySpawnItem()
    {
        // 1. Filter items valid for THIS solar system
        var validItems = itemPool.FindAll(i => i.systemFoundAt == currentSystem);
        if (validItems.Count == 0) return;

        // 2. Calculate a random point in a "cone" or "ring" ahead of the player
        Vector2 spawnPos = (Vector2)transform.position + (Random.insideUnitCircle.normalized *
                           Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer));
        // ONLY spawn if it is both off-screen AND not overlapping existing objects
        if (IsPositionOffScreen(spawnPos))
        {
            // 3. Collision Check: Don't spawn if something is already there
            if (!Physics2D.OverlapCircle(spawnPos, itemSpacingRadius))
            {
                CollectibleItem selectedData = validItems[Random.Range(0, validItems.Count)];
                SpawnItemWorldObject(selectedData, spawnPos);
            }
        }
    }

    private bool IsPositionOffScreen(Vector2 worldPosition)
    {
        // Convert the world position (e.g., 50, 20) to a Viewport point (e.g., 1.2, 0.5)
        Vector3 viewportPoint = Camera.main.WorldToViewportPoint(worldPosition);

        // If X or Y are outside the 0-1 range, it is off-screen.
        // We add a small 'padding' (0.1f) so items don't pop in at the very edge.
        bool isOffScreen = viewportPoint.x < -0.1f || viewportPoint.x > 1.1f ||
                           viewportPoint.y < -0.1f || viewportPoint.y > 1.1f;

        return isOffScreen;
    }
    void SpawnItemWorldObject(CollectibleItem data, Vector2 position)
    {
        // Use Object Pooling here for performance in 2026
        /* GameObject itemObj = ItemPoolManager.Instance.GetFromPool();
         itemObj.transform.position = position;
         itemObj.GetComponent<ItemWorldDisplay>().Setup(data);*/
        // 1. Get the generic GameObject from the pool
        GameObject itemObj = ItemPoolManager.Instance.GetItem();
        itemObj.transform.position = position;

        // 2. Access the ItemWorldDisplay component to inject the ScriptableObject data
        ItemWorldDisplay display = itemObj.GetComponent<ItemWorldDisplay>();

        if (display != null)
        {
            display.Setup(data);
        }
    }
}

