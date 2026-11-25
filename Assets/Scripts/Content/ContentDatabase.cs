using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One lookup from content id to asset, for every content type.
/// The entry list is filled by Tools > Content > Rebuild Content Database (the migration and
/// the CSV importer also rebuild it). Not wired into gameplay yet.
/// </summary>
[CreateAssetMenu(fileName = "ContentDatabase", menuName = "Content/Content Database")]
public class ContentDatabase : ScriptableObject
{
    [SerializeField] private List<ContentDefinition> entries = new List<ContentDefinition>();

    private Dictionary<string, ContentDefinition> byId;

    public IReadOnlyList<ContentDefinition> Entries => entries;

    /// <summary>Returns the asset with this id, or null (with a warning) if missing or of another type.</summary>
    public T Get<T>(string id) where T : ContentDefinition
    {
        if (TryGet(id, out T result))
            return result;

        Debug.LogWarning($"ContentDatabase: no {typeof(T).Name} with id '{id}'.", this);
        return null;
    }

    public bool TryGet<T>(string id, out T result) where T : ContentDefinition
    {
        result = null;
        if (string.IsNullOrEmpty(id))
            return false;

        EnsureIndex();
        if (byId.TryGetValue(id, out ContentDefinition def) && def is T typed)
        {
            result = typed;
            return true;
        }
        return false;
    }

    public List<T> GetAll<T>() where T : ContentDefinition
    {
        var list = new List<T>();
        foreach (var def in entries)
            if (def is T typed)
                list.Add(typed);
        return list;
    }

    private void EnsureIndex()
    {
        if (byId != null)
            return;

        byId = new Dictionary<string, ContentDefinition>(entries.Count);
        foreach (var def in entries)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
                continue;
            if (byId.ContainsKey(def.Id))
            {
                Debug.LogWarning($"ContentDatabase: duplicate id '{def.Id}' ({def.name}); keeping the first.", def);
                continue;
            }
            byId.Add(def.Id, def);
        }
    }

    private void OnEnable() => byId = null;

#if UNITY_EDITOR
    /// <summary>Editor only: replace the entry list and drop the cached index.</summary>
    public void EditorSetEntries(List<ContentDefinition> newEntries)
    {
        entries = newEntries;
        byId = null;
    }
#endif
}
