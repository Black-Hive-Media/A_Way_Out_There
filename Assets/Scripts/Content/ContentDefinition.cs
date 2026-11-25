using UnityEngine;

/// <summary>
/// Base class for every piece of authored content (items, upgrades, systems, ports, dialog...).
/// The id is the stable key that save data and the CSV importer use. Never rename an id once
/// it has shipped: saves refer to content only by id.
/// Convention: lower_snake_case with a type prefix, e.g. sys_sol, port_bounty, itm_gold.
/// </summary>
public abstract class ContentDefinition : ScriptableObject
{
    [Tooltip("Stable content ID used by saves and the CSV importer. Do not change after release.")]
    [SerializeField] private string id;

    public string Id => id;

    /// <summary>Serialized field name, for editor tools that write it through SerializedObject.</summary>
    public const string IdFieldName = nameof(id);
}
