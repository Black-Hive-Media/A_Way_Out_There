using System.Collections;
using UnityEngine;

/// <summary>
/// The running game: owns the UniverseState (sim clock, item ledger, item history), the save
/// service, and points at the content database. One per scene, on the GameSession object.
/// </summary>
[DisallowMultipleComponent]
public class GameSession : MonoBehaviour
{
    [Tooltip("Assets/Data/ContentDatabase.asset. Used to look items up by id.")]
    [SerializeField] private ContentDatabase content;

    [Tooltip("The system this scene is. Empty = CollectibleItemSpawner's Current System.")]
    [SerializeField] private SolarSystemDefinition currentSystem;

    [Header("Saving")]
    [Tooltip("Slot used for autosave and for loading on start.")]
    [SerializeField, Min(0)] private int autosaveSlot = 0;
    [Tooltip("On start, load the autosave slot if it exists, otherwise start a new game in it.")]
    [SerializeField] private bool loadOnStart = true;

    private UniverseState universe = new UniverseState();
    private bool autosavePending;
    private bool autosaveBlocked;

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
    public SaveService Saves { get; private set; }
    public int AutosaveSlot => autosaveSlot;

    /// <summary>True when autosave is off because the slot couldn't be loaded (so it isn't overwritten).</summary>
    public bool AutosaveBlocked => autosaveBlocked;

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
        quitting = false;
        Saves = new SaveService(this, new PcSaveStorage());
        if (content == null)
            Debug.LogWarning("[GameSession] No Content Database assigned. Items still work, but ids can't be looked up.", this);
    }

    private void Start()
    {
        if (!loadOnStart)
            return;

        if (Saves.Storage.Exists(autosaveSlot))
        {
            if (!Saves.Load(autosaveSlot))
            {
                autosaveBlocked = true;
                Debug.LogError($"[GameSession] Slot {autosaveSlot} couldn't be loaded, so autosave is off to keep that file safe. {Saves.LastError}", this);
            }
        }
        else
        {
            Saves.NewGame(autosaveSlot);
        }
    }

    private void Update() => universe.clock.Advance(Time.deltaTime);

    /// <summary>Replaces the universe (load / new game).</summary>
    public void SetUniverse(UniverseState state) => universe = state ?? new UniverseState();

    // ---------------------------------------------------------------- Autosave

    /// <summary>Queue an autosave of the session's slot; written at the end of this frame.</summary>
    public static void Autosave(string reason)
    {
        GameSession session = Current;
        if (session != null)
            session.RequestAutosave(reason);
    }

    public void RequestAutosave(string reason)
    {
        if (autosaveBlocked || autosavePending || !isActiveAndEnabled)
            return;
        autosavePending = true;
        StartCoroutine(AutosaveAtEndOfFrame(reason));
    }

    private IEnumerator AutosaveAtEndOfFrame(string reason)
    {
        yield return new WaitForEndOfFrame(); // after physics, scripts and rendering: never mid-physics
        autosavePending = false;
        Debug.Log($"[Save] Autosave ({reason}).");
        Saves.Save(autosaveSlot);
    }

    private void SaveNow(string reason)
    {
        if (autosaveBlocked || Saves == null || !enabled)
            return;
        Debug.Log($"[Save] Autosave ({reason}).");
        Saves.Save(autosaveSlot);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveNow("app paused"); // the app may be suspended before the frame ends
    }

    private void OnApplicationQuit()
    {
        SaveNow("quit");
        quitting = true;
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    // ---------------------------------------------------------------- Ledger

    /// <summary>Writes a ledger row stamped with the current sim time.</summary>
    public LedgerRow Record(LedgerEvent eventType, ItemInstance item, string fromShipId = "", string toShipId = "", string portId = "", int price = 0) =>
        universe.ledger.Record(eventType, item != null ? item.instanceId : "", item != null ? item.itemId : "", universe.clock.Now, fromShipId, toShipId, portId, price);
}
