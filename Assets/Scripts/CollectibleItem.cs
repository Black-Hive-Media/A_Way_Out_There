using UnityEngine;

public abstract class CollectibleItem : ContentDefinition//ShipItem
{
    public enum MovementStyle { HeavyDrift, UnstableTumble, OrganicBob, StableStatic, ErraticThruster, Anomalous }

    // Inside your ShipItem ScriptableObject:
    public MovementStyle movementStyle;
    public float driftSpeed = 1f;
    public float rotationSpeed = 20f;
    public float jitterIntensity = 0.5f;

    [Header("Basic Info")]
    public string itemName;
    [TextArea] public string description;
    public GameObject visualPrefab;
    public SolarSystemDefinition foundInSystem;

    [Header("Trade Info")]
    [Range(5, 400)]
    public int baseValue;
    public SpacePortDefinition targetSpacePort;

    [Header("Ship Cargo Info")]
    [Range(1,5)]
    public int cargoUnitsRequired;

    [Header("Hud Info")]
    public GameObject prefabForHud;
}