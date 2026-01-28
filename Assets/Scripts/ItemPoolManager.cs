using UnityEngine;
using UnityEngine.Pool; // 2026 standard API for pooling

public class ItemPoolManager : MonoBehaviour
{
    public static ItemPoolManager Instance { get; private set; }

    private ObjectPool<GameObject> asteroidPool;
    private ObjectPool<GameObject> itemPool; 

    [SerializeField] private GameObject[] asteroidPrefabs;
    [SerializeField] private GameObject itemPrefab; 
     
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 50;

    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize the built-in Unity pool
        // Initialize Asteroid Pool
        asteroidPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(asteroidPrefabs[UnityEngine.Random.Range(0, asteroidPrefabs.Length)]),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false)
        );

      

        // Initialize Item Pool
        itemPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(itemPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false)
        );
    }
    public GameObject GetAsteroid() => asteroidPool.Get();
    public GameObject GetItem() => itemPool.Get(); 


    // Specific release methods so objects go to the right internal pool
    public void ReleaseAsteroid(GameObject obj) => asteroidPool.Release(obj);
    public void ReleaseItem(GameObject obj) => itemPool.Release(obj); 

    /*
        // Public method to access the pool
        //public GameObject GetFromPool() => pool.Get();
        public GameObject GetFromPool()
        {
            // If the pool is at max capacity and all are in use, 
            // some implementations might return null or throw an error.
            try
            {
                return pool.Get();
            }
            catch (System.Exception)
            {
                Debug.LogWarning("Pool Exhausted! AI is going to be snarky about this...");
                return null;
            }
        }
        // Method for items to "despawn" themselves back to the pool
        //public void ReturnToPool(GameObject obj) => pool.Release(obj);

        public void ReturnToPool(GameObject obj)
        {
            // Safety check for 2026: don't release if already in the pool
            if (!obj.activeInHierarchy) return;

            pool.Release(obj);
        }*/
}
