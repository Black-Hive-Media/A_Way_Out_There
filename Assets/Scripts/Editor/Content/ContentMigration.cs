using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using PortType = SpacePortDefinition.PortType;
using PortServices = SpacePortDefinition.PortServices;

/// <summary>
/// Creates the starting solar system and space port assets if they are missing, and assigns an id
/// to every content asset that has none. Safe to run any number of times: it never overwrites or
/// deletes anything.
/// (The one-time move off the SolarSystemFound / DestinationPort enums is done; those enums are gone.)
/// </summary>
public static class ContentMigration
{
    private const string SystemsFolder = ContentEditorUtility.ContentRoot + "/Systems";
    private const string PortsFolder = ContentEditorUtility.ContentRoot + "/Ports";

    private static readonly (string id, string name, int tier)[] Systems =
    {
        ("sys_sol",   "Sol System",   1),
        ("sys_blue",  "Blue System",  1),
        ("sys_green", "Green System", 2),
        ("sys_star",  "Star System",  3),
        ("sys_red",   "Red System",   3),
    };

    private const string PortHomeSystemId = "sys_sol";
    private const PortServices DefaultPortServices = PortServices.Sell | PortServices.Upgrade;

    private static readonly (string id, string name, PortType type)[] Ports =
    {
        ("port_bounty",     "Bounty Port",     PortType.Bounty),
        ("port_settlement", "Settlement Port", PortType.Settlement),
        ("port_refinery",   "Refinery Port",   PortType.Refinery),
        ("port_city",       "City Port",       PortType.City),
    };

    [MenuItem(ContentEditorUtility.MenuRoot + "Assign Missing IDs", priority = 20)]
    public static void Run()
    {
        var log = new StringBuilder();
        int changes = 0;

        var taken = new HashSet<string>(
            ContentEditorUtility.FindAll<ContentDefinition>().Select(d => d.Id).Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);

        // 1. Solar systems
        var systems = ContentEditorUtility.IndexById(ContentEditorUtility.FindAll<SolarSystemDefinition>());
        foreach (var spec in Systems)
        {
            if (systems.ContainsKey(spec.id))
                continue;
            if (taken.Contains(spec.id))
            {
                Debug.LogError($"[Content] Can't create {spec.id}: that id is used by an asset that isn't a SolarSystemDefinition.");
                continue;
            }
            var sys = ContentEditorUtility.CreateAsset<SolarSystemDefinition>(SystemsFolder, spec.id);
            sys.displayName = spec.name;
            sys.difficultyTier = spec.tier;
            EditorUtility.SetDirty(sys);
            systems[spec.id] = sys;
            taken.Add(spec.id);
            log.AppendLine($"Created system {spec.id}");
            changes++;
        }

        // 2. Space ports
        systems.TryGetValue(PortHomeSystemId, out SolarSystemDefinition homeSystem);
        var ports = ContentEditorUtility.IndexById(ContentEditorUtility.FindAll<SpacePortDefinition>());
        foreach (var spec in Ports)
        {
            if (ports.ContainsKey(spec.id))
                continue;
            if (taken.Contains(spec.id))
            {
                Debug.LogError($"[Content] Can't create {spec.id}: that id is used by an asset that isn't a SpacePortDefinition.");
                continue;
            }
            var port = ContentEditorUtility.CreateAsset<SpacePortDefinition>(PortsFolder, spec.id);
            port.displayName = spec.name;
            port.portType = spec.type;
            port.services = DefaultPortServices;
            port.system = homeSystem;
            EditorUtility.SetDirty(port);
            taken.Add(spec.id);
            log.AppendLine($"Created port {spec.id}");
            changes++;
        }

        // 3. IDs for everything else
        changes += AssignIds<CollectibleItem>("itm_", d => d.name, taken, log);
        changes += AssignIds<ShipUpgrade>("upg_", d => d.name, taken, log);
        changes += AssignIds<MiningLaserStats>("lsr_", d => d.name, taken, log);
        changes += AssignIds<IndustryDefinition>("ind_", d => d.name, taken, log);
        changes += AssignIds<SolarSystemDefinition>("sys_", d => d.name, taken, log);
        changes += AssignIds<SpacePortDefinition>("port_", d => d.name, taken, log);
        changes += AssignAIMessageIds(taken, log);

        AssetDatabase.SaveAssets();
        ContentDatabaseBuilder.Rebuild();
        ContentValidator.Validate(out string validation);

        string summary = changes == 0 ? "Nothing to do: every content asset already has an ID." : $"{changes} change(s) made.";
        Debug.Log($"[Content] Assign Missing IDs finished. {summary}\n{log}");
        EditorUtility.DisplayDialog("Assign missing IDs", $"{summary}\n\n{validation}", "OK");
    }

    private static int AssignIds<T>(string prefix, Func<T, string> nameOf, HashSet<string> taken, StringBuilder log)
        where T : ContentDefinition
    {
        int count = 0;
        foreach (T def in ContentEditorUtility.FindAll<T>())
        {
            if (!string.IsNullOrWhiteSpace(def.Id))
                continue;
            string name = ContentEditorUtility.ToSnakeCase(nameOf(def));
            if (name.StartsWith(prefix))
                name = name.Substring(prefix.Length);
            string id = ContentEditorUtility.UniqueId(prefix + name, taken);
            ContentEditorUtility.SetId(def, id);
            log.AppendLine($"{AssetDatabase.GetAssetPath(def)}: id = {id}");
            count++;
        }
        return count;
    }

    /// <summary>
    /// AI messages are triggered by their key in AIDatabaseObj (e.g. "CargoBayFull"). An empty
    /// messageID is filled from that key, and the id is msg_ + snake_case(key).
    /// </summary>
    private static int AssignAIMessageIds(HashSet<string> taken, StringBuilder log)
    {
        Dictionary<AI_Message, string> triggerKeys = AIMessageDatabaseEditorUtility.GetTriggerKeys();
        int count = 0;

        foreach (AI_Message msg in ContentEditorUtility.FindAll<AI_Message>())
        {
            if (!triggerKeys.TryGetValue(msg, out string trigger))
                trigger = string.IsNullOrWhiteSpace(msg.messageID) ? msg.name : msg.messageID;

            if (string.IsNullOrWhiteSpace(msg.messageID))
            {
                msg.messageID = trigger;
                EditorUtility.SetDirty(msg);
                log.AppendLine($"{msg.name}: messageID = {trigger}");
                count++;
            }

            if (string.IsNullOrWhiteSpace(msg.Id))
            {
                string id = ContentEditorUtility.UniqueId("msg_" + ContentEditorUtility.ToSnakeCase(trigger), taken);
                ContentEditorUtility.SetId(msg, id);
                log.AppendLine($"{AssetDatabase.GetAssetPath(msg)}: id = {id}");
                count++;
            }
        }
        return count;
    }
}
