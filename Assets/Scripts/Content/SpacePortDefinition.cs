using System;
using UnityEngine;

/// <summary>
/// A space port inside a solar system (replaced the old CollectibleItem.DestinationPort enum).
/// </summary>
[CreateAssetMenu(fileName = "port_new", menuName = "Content/Space Port")]
public class SpacePortDefinition : ContentDefinition
{
    public enum PortType { Bounty, Settlement, Refinery, City }

    [Flags]
    public enum PortServices
    {
        None = 0,
        Sell = 1 << 0,
        Buy = 1 << 1,
        Upgrade = 1 << 2,
        Refuel = 1 << 3,
        Repair = 1 << 4,
        Cosmetics = 1 << 5,
        Deliveries = 1 << 6,
    }

    public SolarSystemDefinition system;
    public string displayName;
    public PortType portType;

    [Tooltip("Position of the port inside its system.")]
    public Vector2 position;

    public PortServices services = PortServices.Sell;

    [Tooltip("Key for the port's comms personality (dialog tone, NPC replies).")]
    public string personalityKey;

    [Min(1)] public int dockingCapacity = 4;
}
