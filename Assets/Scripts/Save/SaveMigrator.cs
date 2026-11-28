using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

/// <summary>One upgrade of the raw save JSON from one schema version to the next.</summary>
public sealed class MigrationStep
{
    public int From { get; }
    public int To { get; }
    public string Description { get; }
    public Action<JObject> Apply { get; }

    public MigrationStep(int from, int to, string description, Action<JObject> apply)
    {
        if (to <= from)
            throw new ArgumentException($"A migration must move forward ({from} -> {to}).");
        From = from;
        To = to;
        Description = description;
        Apply = apply ?? throw new ArgumentNullException(nameof(apply));
    }
}

/// <summary>A save that this build can't read.</summary>
public sealed class SaveVersionException : Exception
{
    public int FileVersion { get; }
    public int GameVersion { get; }

    public SaveVersionException(int fileVersion, int gameVersion, string message) : base(message)
    {
        FileVersion = fileVersion;
        GameVersion = gameVersion;
    }
}

/// <summary>
/// Brings an old save up to the current schema before it is deserialized, one step at a time.
/// To change the save format: bump CurrentVersion and add a step from the old version to it.
/// </summary>
public static class SaveMigrator
{
    /// <summary>The schema this build writes and reads.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Ordered steps. Each must start where the previous one ended.</summary>
    public static readonly IReadOnlyList<MigrationStep> Steps = new List<MigrationStep>
    {
        // Pattern for future steps. Files written before schemaVersion existed count as version 0;
        // their layout is already version 1's, so there is nothing to change.
        new MigrationStep(0, 1, "Pre-release saves without schemaVersion: no changes needed.", root => { }),
    };

    /// <summary>Migrates root in place to CurrentVersion and returns the version it ends at.</summary>
    public static int Migrate(JObject root) => Migrate(root, Steps, CurrentVersion);

    /// <summary>Migrates root in place with the given steps (tests pass their own).</summary>
    public static int Migrate(JObject root, IReadOnlyList<MigrationStep> steps, int targetVersion)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        int version = root.Value<int?>("schemaVersion") ?? 0;
        if (version > targetVersion)
            throw new SaveVersionException(version, targetVersion,
                $"This save was made by a newer version of A Way Out There (save format {version}; this build reads up to {targetVersion}). Update the game to load it. The file was left untouched.");

        while (version < targetVersion)
        {
            MigrationStep step = steps.FirstOrDefault(s => s.From == version);
            if (step == null)
                throw new SaveVersionException(version, targetVersion,
                    $"This save uses format {version}, and there is no migration from it to {targetVersion}. The file was left untouched.");
            step.Apply(root);
            version = step.To;
            root["schemaVersion"] = version;
        }
        return version;
    }
}
