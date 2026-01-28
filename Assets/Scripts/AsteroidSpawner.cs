using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    public GameObject[] asteroidPrefabs;
    public float spawnRadius = 50f;
    public int maxAsteroids = 10;

    private List<GameObject> activeAsteroids = new List<GameObject>();

    void Start()
    {
        StartCoroutine(MaintainPopulation());
    }

    IEnumerator MaintainPopulation()
    {
        while (true)
        {
            // Remove any asteroids that were mined/returned to pool
            activeAsteroids.RemoveAll(a => a == null || !a.activeInHierarchy);

            if (activeAsteroids.Count < maxAsteroids)
            {
                SpawnAsteroid();
            }

            yield return new WaitForSeconds(2f); // Check every 2 seconds
        }
    }

    void SpawnAsteroid()
    {
        // 1. Get from your ItemPoolManager (ensure asteroids are in the pool array)
        GameObject asteroid = ItemPoolManager.Instance.GetAsteroid();

        // 2. Position it off-screen
        Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * spawnRadius;
        asteroid.transform.position = randomPos;

        activeAsteroids.Add(asteroid);
    }
}
