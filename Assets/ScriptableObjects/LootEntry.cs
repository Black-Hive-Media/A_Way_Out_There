using UnityEngine;
[System.Serializable]
public class LootEntry
{
    public CollectibleItem item;
    [Range(1, 100)] public int weight; // Higher weight = more common
}
