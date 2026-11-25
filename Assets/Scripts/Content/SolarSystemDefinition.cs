using UnityEngine;

/// <summary>
/// A solar system (replaced the old CollectibleItem.SolarSystemFound enum).
/// </summary>
[CreateAssetMenu(fileName = "sys_new", menuName = "Content/Solar System")]
public class SolarSystemDefinition : ContentDefinition
{
    public string displayName;

    [Tooltip("1 = easy, 2 = medium, 3 = hard. Every system is reachable at any tier.")]
    [Min(1)] public int difficultyTier = 1;

    public IndustryDefinition primaryIndustry;
    public IndustryDefinition secondaryIndustry;

    [Tooltip("Position on the galaxy map; jump distance is measured between these.")]
    public Vector2 mapPosition;

    [Min(0)] public float debrisDensity = 1f;
    [Min(0)] public float asteroidDensity = 1f;

    [Tooltip("Key for the system's music/ambience.")]
    public string ambienceKey;
}
