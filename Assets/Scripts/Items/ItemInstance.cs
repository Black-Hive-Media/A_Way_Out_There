using System;
using UnityEngine;

/// <summary>How a crew got an item. More sources (Bought, Salvaged, Gifted...) come with later steps.</summary>
public enum AcquisitionSource
{
    Debris,
    Mined,
}

/// <summary>
/// One physical item: a row in the save's ItemInstance table. Refers to its content only by id
/// (itemId), so content can change without breaking saves. Plain C# so step 5 can serialize it.
/// </summary>
[Serializable]
public class ItemInstance
{
    /// <summary>Value of perishAt when the item doesn't perish (perishing is step 7).</summary>
    public const double NoPerish = -1;

    public string instanceId;
    public string itemId;
    /// <summary>Empty until item variants exist.</summary>
    public string variantId = "";
    public string shipId = "";
    /// <summary>0..1, 1 = perfect.</summary>
    public float condition = 1f;
    public AcquisitionSource source;
    public string acquiredSystemId = "";
    /// <summary>Empty until split-screen players exist (step 11).</summary>
    public string acquiredByPlayerId = "";
    /// <summary>Sim time (seconds of play) when the item was acquired.</summary>
    public double acquiredAt;
    /// <summary>Port the item was bought at; empty until markets (step 6).</summary>
    public string purchasedPortId = "";
    /// <summary>Price paid; 0 until markets (step 6).</summary>
    public int purchasePrice;
    /// <summary>Sim time the item perishes, or NoPerish.</summary>
    public double perishAt = NoPerish;

    [NonSerialized] private CollectibleItem definition;

    /// <summary>
    /// A new instance of a content item. systemId and simTime default to the current system and
    /// the GameSession clock.
    /// </summary>
    public static ItemInstance Create(CollectibleItem item, AcquisitionSource source, string systemId = null, double? simTime = null)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        if (string.IsNullOrEmpty(item.Id))
            Debug.LogWarning($"{item.name} has no content id; run Tools > Content > Assign Missing IDs.", item);

        GameSession session = GameSession.Current;
        return new ItemInstance
        {
            instanceId = Guid.NewGuid().ToString("N"),
            itemId = item.Id,
            source = source,
            acquiredSystemId = systemId ?? (session != null ? session.CurrentSystemId : ""),
            acquiredAt = simTime ?? (session != null ? session.Clock.Now : 0),
            definition = item,
        };
    }

    /// <summary>The content item this instance is, looked up by itemId in the database (cached).</summary>
    public CollectibleItem Resolve(ContentDatabase database)
    {
        if (definition == null && database != null)
            database.TryGet(itemId, out definition);
        return definition;
    }

    /// <summary>The content item, resolved through the GameSession's ContentDatabase if needed.</summary>
    public CollectibleItem Definition => definition != null ? definition : Resolve(GameSession.Current != null ? GameSession.Current.Content : null);

    public int CargoUnits => Definition != null ? Definition.cargoUnitsRequired : 0;

    /// <summary>First 8 characters of the id, for logs and inspectors.</summary>
    public string ShortId => string.IsNullOrEmpty(instanceId) ? "?" : instanceId.Substring(0, Math.Min(8, instanceId.Length));

    public override string ToString() => $"{itemId} [{ShortId}] {source} in {acquiredSystemId} at {acquiredAt:0.0}s";
}
