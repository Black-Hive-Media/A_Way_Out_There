using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject ship;
    public GameObject[] asteroidPrefabs;
    public float spawnRadius = 50f;
    public int maxAsteroids = 10;

    [Header("Spawn Controls")]
    public float distanceBetweenSpawnChecks = 50f;
    public float minDistanceFromPlayer = 20f;
    public float maxDistanceFromPlayer = 40f;
    public float itemSpacingRadius = 15f; // "Poisson" spacing
    public LayerMask layersToIgnore;

    private List<GameObject> activeAsteroids = new List<GameObject>();

    [Header("Safe Zone Settings")]
    public Collider2D mapBoundary; // Drag your Polygon/Composite Collider here
    public int maxSpawnAttempts = 10; // Failsafe to prevent infinite loops

    private Vector2 lastSpawnPosition;
    void Start()
    {
        //StartCoroutine(MaintainPopulation());
    }
    void Update()
    {
        float distanceMoved = Vector2.Distance(ship.transform.position, lastSpawnPosition);
        if (distanceMoved >= distanceBetweenSpawnChecks)
        {
            TrySpawnItem();
            lastSpawnPosition = transform.position;
        }
         
    }
    /*IEnumerator MaintainPopulation()
    {
        while (true)
        {
            // Remove any asteroids that were mined/returned to pool
            activeAsteroids.RemoveAll(a => a == null || !a.activeInHierarchy);

            if (activeAsteroids.Count < maxAsteroids)
            {
                TrySpawnItem();
            }

            yield return new WaitForSeconds(2f); // Check every 2 seconds
        }
    }*/

   /* void SpawnAsteroid()
    {
        // 1. Get from your ItemPoolManager (ensure asteroids are in the pool array)
        GameObject asteroid = ItemPoolManager.Instance.GetAsteroid();

        // 2. Position it off-screen
        Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnRadius;
        asteroid.transform.position = randomPos;

        activeAsteroids.Add(asteroid);
    }*/
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
            //   ShipItem selectedData = itemPool[Random.Range(0, itemPool.Count)];
            //  SpawnItemWorldObject(selectedData, spawnPos);
            GameObject asteroid = ItemPoolManager.Instance.GetAsteroid();

            asteroid.transform.position = spawnPos;

            activeAsteroids.Add(asteroid);
        }
        else
        {
            // AI Snark: "Sector density too high. Deployment failed."
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        // Draws the inner "No-Spawn" zone
        Gizmos.DrawWireSphere(ship.transform.position, minDistanceFromPlayer);

        Gizmos.color = Color.red;
        // Draws the outer "Spawn Limit" zone
        Gizmos.DrawWireSphere(ship.transform.position, maxDistanceFromPlayer);
    }
}
