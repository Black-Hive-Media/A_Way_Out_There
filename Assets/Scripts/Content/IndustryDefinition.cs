using UnityEngine;

/// <summary>
/// An industry (Mining, Agriculture, Medical, Tech, Salvage, Tourism). Drives what a system's
/// ports produce and consume. Category lists hold ItemCategory ids until that type exists.
/// </summary>
[CreateAssetMenu(fileName = "ind_new", menuName = "Content/Industry")]
public class IndustryDefinition : ContentDefinition
{
    public string displayName;

    [Tooltip("ItemCategory ids this industry produces (e.g. cat_refinable).")]
    public string[] producesCategories = new string[0];

    [Tooltip("ItemCategory ids this industry consumes.")]
    public string[] consumesCategories = new string[0];
}
