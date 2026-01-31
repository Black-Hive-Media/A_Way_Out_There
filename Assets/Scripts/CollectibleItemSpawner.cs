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

    public LayerMask layersToIgnore; // Set "MapBoundary" layer here in Inspector
    [Header("Safe Zone Settings")]
    public Collider2D mapBoundary; // Drag your Polygon/Composite Collider here
    public int maxSpawnAttempts = 10; // Failsafe to prevent infinite loops

    private Vector2 lastSpawnPosition;

    void Update()
    {
        float distanceMoved = Vector2.Distance(ship.transform.position, lastSpawnPosition);
        if (distanceMoved >= distanceBetweenSpawnChecks)
        {
            TrySpawnItem();
            lastSpawnPosition = transform.position;
        }
    }

    void TrySpawnItem()
    {
        Vector2 spawnPos = Vector2.zero;
        bool isValid = false;
        int attempts = 0;

        // 1. Loop until we find a spot inside the boundary
        while (!isValid && attempts < maxSpawnAttempts)
        {
            attempts++;

            // Pick a random spot (using your existing circular/distance logic)
            spawnPos = (Vector2)ship.transform.position + (Random.insideUnitCircle.normalized *
                       Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer));

            // 2. CHECK: Is this specific point inside the map's collider?
            // Note: For CompositeColliders, set "Geometry Type" to "Polygons" for this to work.
            if (mapBoundary.OverlapPoint(spawnPos))
            {
                // 3. SECOND CHECK: Is it clear of other asteroids/items?
                if (!Physics2D.OverlapCircle(spawnPos, itemSpacingRadius, ~layersToIgnore))
                {
                    isValid = true;
                }
            }
        }

        // 4. Success? Spawn it.
        if (isValid)
        {
            CollectibleItem selectedData = itemPool[Random.Range(0, itemPool.Count)];
            SpawnItemWorldObject(selectedData, spawnPos);
        }
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
        //Debug.Log(itemObj.name + "  " + data.visualPrefab.ToString());

        if (display != null)
        {
            display.Setup(data, itemObj);
        }
    }
}

