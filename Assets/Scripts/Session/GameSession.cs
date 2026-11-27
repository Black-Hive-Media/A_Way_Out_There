using UnityEngine;

/// <summary>
/// The running game: owns the UniverseState (sim clock, item ledger) and points at the content
/// database. One per scene; put it on an empty GameObject in GameScene.
/// </summary>
[DisallowMultipleComponent]
public class GameSession : MonoBehaviour
{
    [Tooltip("Assets/Data/ContentDatabase.asset. Used to look items up by id.")]
    [SerializeField] private ContentDatabase content;

    [Tooltip("The system this scene is. Empty = CollectibleItemSpawner's Current System.")]
    [SerializeField] private SolarSystemDefinition currentSystem;

    private UniverseState universe = new UniverseState();

    private static GameSession current;
    private static bool quitting;

    /// <summary>The session in the scene. If none was placed, one is created (with a warning).</summary>
    public static GameSession Current
    {
        get
        {
            if (current != null || quitting || !Application.isPlaying)
                return current;
            current = FindAnyObjectByType<GameSession>();
            if (current == null)
            {
                Debug.LogWarning("[GameSession] None in the scene, so one was created. Add a GameSession object to GameScene and assign its Content Database.");
                current = new GameObject("GameSession (auto)").AddComponent<GameSession>();
            }
            return current;
        }
    }

    public UniverseState Universe => universe;
    public SimClock Clock => universe.clock;
    public ItemLedger Ledger => universe.ledger;
    public ContentDatabase Content => content;

    public SolarSystemDefinition CurrentSystem
    {
        get
        {
            if (currentSystem == null)
            {
                var spawner = FindAnyObjectByType<CollectibleItemSpawner>();
                if (spawner != null)
                    currentSystem = spawner.currentSystem;
            }
            return currentSystem;
        }
    }

    public string CurrentSystemId => CurrentSystem != null ? CurrentSystem.Id : "";

    private void Awake()
    {
        if (current != null && current != this)
        {
            Debug.LogWarning("[GameSession] More than one in the scene; this one is disabled.", this);
            enabled = false;
            return;
        }
        current = this;
        if (content == null)
            Debug.LogWarning("[GameSession] No Content Database assigned. Items still work, but ids can't be looked up.", this);
    }

    private void Update() => universe.clock.Advance(Time.deltaTime);

    private void OnApplicationQuit() => quitting = true;

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    /// <summary>Writes a ledger row stamped with the current sim time.</summary>
    public LedgerRow Record(LedgerEvent eventType, ItemInstance item, string fromShipId = "", string toShipId = "", string portId = "", int price = 0) =>
        universe.ledger.Record(eventType, item != null ? item.instanceId : "", universe.clock.Now, fromShipId, toShipId, portId, price);
}
