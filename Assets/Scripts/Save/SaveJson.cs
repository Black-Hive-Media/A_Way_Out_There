using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

/// <summary>
/// Turns SaveData into JSON and back. Only public fields are written (computed properties such
/// as ItemInstance.Definition are skipped), enums are written as names, and loading runs the
/// SaveMigrator on the raw JSON before it becomes objects.
/// </summary>
public static class SaveJson
{
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ContractResolver = new PublicFieldsResolver(),
        Converters = { new StringEnumConverter() },
        Formatting = Formatting.Indented,
        ObjectCreationHandling = ObjectCreationHandling.Replace, // don't append to lists a constructor created
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Include,
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
    };

    public static string Serialize(SaveData data) => JsonConvert.SerializeObject(data, Settings);

    /// <summary>Parses, migrates to the current schema, then builds the objects.</summary>
    /// <exception cref="SaveVersionException">The file is newer than this game, or can't be migrated.</exception>
    /// <exception cref="JsonException">The file isn't valid save JSON.</exception>
    public static SaveData Deserialize(string json)
    {
        JObject root = JObject.Parse(json);
        SaveMigrator.Migrate(root);
        SaveData data = root.ToObject<SaveData>(JsonSerializer.Create(Settings));
        if (data == null)
            throw new JsonSerializationException("The save file is empty.");
        data.universe ??= new UniverseState();
        data.ships ??= new List<ShipSave>();
        return data;
    }

    /// <summary>Serializes public instance fields only (not properties), skipping [NonSerialized].</summary>
    private sealed class PublicFieldsResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType) =>
            objectType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsNotSerialized && !f.IsInitOnly && !f.IsLiteral)
                .Cast<MemberInfo>()
                .ToList();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            property.Readable = true;
            property.Writable = true;
            return property;
        }
    }
}
