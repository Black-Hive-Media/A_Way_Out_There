using UnityEngine;
using UnityEngine.Pool; // 2026 standard API for pooling

public class ItemPoolManager : MonoBehaviour
{
    public static ItemPoolManager Instance { get; private set; }

    [SerializeField] private GameObject[] itemPrefab;
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 50;

    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize the built-in Unity pool
        pool = new ObjectPool<GameObject>(
            createFunc: () => {
                // Select a random prefab from your array each time a new one is needed
                GameObject randomPrefab = itemPrefab[Random.Range(0, itemPrefab.Length)];
                return Instantiate(randomPrefab);
            },            
            //createFunc: () => Instantiate(itemPrefab),
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxPoolSize
        );
    }

    // Public method to access the pool
    public GameObject GetFromPool() => pool.Get();

    // Method for items to "despawn" themselves back to the pool
    public void ReturnToPool(GameObject obj) => pool.Release(obj);
}
