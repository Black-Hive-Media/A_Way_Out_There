using UnityEngine;

public abstract class CollectibleItem : ScriptableObject//ShipItem
{
    [Header("Basic Info")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public SolarSystemFound systemFoundAt;

    [Header("Trade Info")]
    [Range(5, 400)]
    public int baseValue;
    public DestinationPort targetPort;

    [Header("Ship Cargo Info")]
    [Range(1,5)]
    public int cargoUnitsRequired;

    public enum DestinationPort
    {
        BountyPort,
        SettlementPort,
        RefineryPort,
        CityPort
    }

    public enum SolarSystemFound
    {
        SolSystem, //easy
        BlueSystem, //easy
        GreenSystem,//medium
        StarSystem, //hard
        RedSystem //hard
    }
}