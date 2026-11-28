using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Edit Mode tests for the save system (Window > General > Test Runner > EditMode).</summary>
public class SaveSystemTests
{
    private readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object o in created)
            if (o != null)
                Object.DestroyImmediate(o);
        created.Clear();
    }

    // ---------------------------------------------------------------- helpers

    private T MakeContent<T>(string id) where T : ContentDefinition
    {
        var asset = ScriptableObject.CreateInstance<T>();
        asset.name = id;
        var so = new SerializedObject(asset);
        so.FindProperty(ContentDefinition.IdFieldName).stringValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
        created.Add(asset);
        return asset;
    }

    private ContentDatabase MakeDatabase(params ContentDefinition[] entries)
    {
        var db = ScriptableObject.CreateInstance<ContentDatabase>();
        db.EditorSetEntries(new List<ContentDefinition>(entries));
        created.Add(db);
        return db;
    }

    private static ItemInstance Instance(string itemId, string instanceId, AcquisitionSource source = AcquisitionSource.Debris) =>
        new ItemInstance { instanceId = instanceId, itemId = itemId, source = source, acquiredSystemId = "sys_sol", acquiredAt = 12.5 };

    // ---------------------------------------------------------------- tests

    [Test]
    public void SaveData_RoundTrips_ThroughJson()
    {
        var universe = new UniverseState();
        universe.clock.seconds = 123.25;
        universe.ledger.Record(LedgerEvent.Mined, "aaaa1111", "itm_gold", 10, toShipId: "ship_1");
        universe.ledger.Record(LedgerEvent.Sold, "bbbb2222", "itm_bandages", 20, fromShipId: "ship_1", portId: "port_refinery", price: 15);
        universe.itemHistory.Add(new ItemHistoryTotals { itemId = "itm_gold", countSold = 3, totalEarned = 120, bestPrice = 40 });

        var ship = new ShipSave
        {
            state = new ShipState { id = "ship_1", hullId = "hull_starter", name = "Ship 1", credits = 77, fuel = 50.5f, hullCondition = 0.8f, cargoUnitsMax = 5 },
            systemId = "sys_sol",
            position = new SavedVector2(3.5f, -2f),
            rotation = 90f,
            upgrades = { new InstalledUpgrade { shipId = "ship_1", slot = UpgradeSlot.MiningLaser, upgradeId = "upg_laser_upgrade_1", tradeInBasis = 50, installedAt = 4 } },
        };
        ship.state.cargo.Add(Instance("itm_gold", "aaaa1111", AcquisitionSource.Mined));

        var data = new SaveData { savedAtUtc = "2026-11-27T10:00:00Z", universe = universe, ships = { ship } };

        string json = SaveJson.Serialize(data);
        SaveData back = SaveJson.Deserialize(json);

        StringAssert.Contains("\"Mined\"", json, "enums are written as names");
        StringAssert.DoesNotContain("Definition", json, "computed properties are not saved");
        StringAssert.DoesNotContain("fileID", json, "no asset references");

        Assert.AreEqual(SaveMigrator.CurrentVersion, back.schemaVersion);
        Assert.AreEqual(123.25, back.universe.clock.seconds);
        Assert.AreEqual(2, back.universe.ledger.rows.Count);
        Assert.AreEqual("itm_bandages", back.universe.ledger.rows[1].itemId);
        Assert.AreEqual(15, back.universe.ledger.rows[1].price);
        Assert.AreEqual(LedgerEvent.Sold, back.universe.ledger.rows[1].eventType);
        Assert.AreEqual(120, back.universe.itemHistory[0].totalEarned);

        ShipSave s = back.ships[0];
        Assert.AreEqual("ship_1", s.state.id);
        Assert.AreEqual(77, s.state.credits);
        Assert.AreEqual(50.5f, s.state.fuel);
        Assert.AreEqual(0.8f, s.state.hullCondition);
        Assert.AreEqual(3.5f, s.position.x);
        Assert.AreEqual(-2f, s.position.y);
        Assert.AreEqual(90f, s.rotation);
        Assert.AreEqual(1, s.state.cargo.Count, "the cargo list isn't duplicated by the constructor");
        Assert.AreEqual("aaaa1111", s.state.cargo[0].instanceId);
        Assert.AreEqual(AcquisitionSource.Mined, s.state.cargo[0].source);
        Assert.AreEqual(12.5, s.state.cargo[0].acquiredAt);
        Assert.AreEqual(ItemInstance.NoPerish, s.state.cargo[0].perishAt);
        Assert.AreEqual(UpgradeSlot.MiningLaser, s.upgrades[0].slot);
        Assert.AreEqual("upg_laser_upgrade_1", s.upgrades[0].upgradeId);
        Assert.AreEqual(50, s.upgrades[0].tradeInBasis);
        Assert.AreEqual(PartSource.Bought, s.upgrades[0].source);
    }

    [Test]
    public void Load_SkipsUnknownItemId_WithWarning()
    {
        var gold = MakeContent<RefinableItem>("itm_test_gold");
        var db = MakeDatabase(gold);
        var saved = new List<ItemInstance> { Instance("itm_test_gold", "known0001"), Instance("itm_removed_thing", "gone00002") };

        LogAssert.Expect(LogType.Warning, new Regex("unknown item id 'itm_removed_thing'"));
        List<ItemInstance> kept = SaveRestore.ResolveCargo(saved, db, "ship_1");

        Assert.AreEqual(1, kept.Count);
        Assert.AreEqual("known0001", kept[0].instanceId);
        Assert.AreSame(gold, kept[0].Resolve(db));
        Assert.AreEqual("ship_1", kept[0].shipId);
    }

    [Test]
    public void Load_SkipsUnknownUpgradeId_WithWarning()
    {
        var db = MakeDatabase(MakeContent<RefinableItem>("itm_test_gold"));
        LogAssert.Expect(LogType.Warning, new Regex("unknown upgrade id 'upg_gone'"));
        Assert.IsNull(SaveRestore.ResolveUpgrade("upg_gone", db, "ship_1"));
    }

    [Test]
    public void Migrator_RunsStepsInOrder_AndRefusesNewerFiles()
    {
        // The real chain: a file without schemaVersion is version 0 and ends at CurrentVersion.
        var legacy = JObject.Parse("{\"universe\":{}, \"ships\":[]}");
        Assert.AreEqual(SaveMigrator.CurrentVersion, SaveMigrator.Migrate(legacy));
        Assert.AreEqual(SaveMigrator.CurrentVersion, (int)legacy["schemaVersion"]);
        Assert.DoesNotThrow(() => SaveJson.Deserialize("{\"universe\":{}, \"ships\":[]}"));

        // A custom chain given out of order still runs 0->1->2 in order.
        var trail = new List<string>();
        var steps = new List<MigrationStep>
        {
            new MigrationStep(1, 2, "second", root => { trail.Add("1->2"); root["b"] = (int)root["a"] + 1; }),
            new MigrationStep(0, 1, "first", root => { trail.Add("0->1"); root["a"] = 1; }),
        };
        var file = JObject.Parse("{}");
        Assert.AreEqual(2, SaveMigrator.Migrate(file, steps, 2));
        CollectionAssert.AreEqual(new[] { "0->1", "1->2" }, trail);
        Assert.AreEqual(2, (int)file["b"]);
        Assert.AreEqual(2, (int)file["schemaVersion"]);

        // Already current: no steps run.
        trail.Clear();
        SaveMigrator.Migrate(file, steps, 2);
        Assert.IsEmpty(trail);

        // Newer than this build: refused, file untouched.
        var newer = JObject.Parse("{\"schemaVersion\": 99}");
        var e = Assert.Throws<SaveVersionException>(() => SaveMigrator.Migrate(newer));
        StringAssert.Contains("newer version", e.Message);
        Assert.AreEqual(99, e.FileVersion);
        Assert.AreEqual(99, (int)newer["schemaVersion"]);

        // A gap in the chain is refused too.
        Assert.Throws<SaveVersionException>(() => SaveMigrator.Migrate(JObject.Parse("{\"schemaVersion\": 1}"), steps.GetRange(1, 1), 2));
    }

    [Test]
    public void Ledger_RollsUpRowsOfItemsNoLongerOwned()
    {
        var ledger = new ItemLedger();
        ledger.Record(LedgerEvent.Collected, "owned", "itm_gold", 1, toShipId: "ship_1");
        ledger.Record(LedgerEvent.Collected, "b", "itm_gold", 2, toShipId: "ship_1");
        ledger.Record(LedgerEvent.Sold, "b", "itm_gold", 3, fromShipId: "ship_1", portId: "port_refinery", price: 40);
        ledger.Record(LedgerEvent.Mined, "c", "itm_gold", 4, toShipId: "ship_1");
        ledger.Record(LedgerEvent.Sold, "c", "itm_gold", 5, fromShipId: "ship_1", portId: "port_refinery", price: 55);
        ledger.Record(LedgerEvent.Collected, "d", "itm_bandages", 6, toShipId: "ship_1");
        ledger.Record(LedgerEvent.Sold, "d", "itm_bandages", 7, fromShipId: "ship_1", portId: "port_refinery", price: 15);

        var totals = new List<ItemHistoryTotals> { new ItemHistoryTotals { itemId = "itm_gold", countSold = 1, totalEarned = 30, bestPrice = 30 } };
        int rolled = ledger.RollUp(new HashSet<string> { "owned" }, totals);

        Assert.AreEqual(6, rolled);
        Assert.AreEqual(1, ledger.rows.Count, "only the owned item's rows stay");
        Assert.AreEqual("owned", ledger.rows[0].instanceId);

        ItemHistoryTotals gold = totals.Find(t => t.itemId == "itm_gold");
        Assert.AreEqual(1, gold.countCollected);
        Assert.AreEqual(1, gold.countMined);
        Assert.AreEqual(3, gold.countSold, "added to the existing totals");
        Assert.AreEqual(125, gold.totalEarned);
        Assert.AreEqual(55, gold.bestPrice);

        ItemHistoryTotals bandages = totals.Find(t => t.itemId == "itm_bandages");
        Assert.AreEqual(1, bandages.countCollected);
        Assert.AreEqual(1, bandages.countSold);
        Assert.AreEqual(15, bandages.totalEarned);
        Assert.AreEqual(15, bandages.bestPrice);

        Assert.AreEqual(0, ledger.RollUp(new HashSet<string> { "owned" }, totals), "running it again changes nothing");
    }

    [Test]
    public void PcStorage_WritesAtomically_AndKeepsBackup()
    {
        string folder = Path.Combine(Path.GetTempPath(), "awot_save_test_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new PcSaveStorage(folder);
            Assert.IsFalse(storage.Exists(3));

            storage.Write(3, "first");
            storage.Write(3, "second");

            Assert.AreEqual("second", storage.Read(3));
            Assert.AreEqual("first", File.ReadAllText(storage.BackupPathFor(3)));
            Assert.IsFalse(File.Exists(storage.PathFor(3) + ".tmp"));
            CollectionAssert.AreEqual(new[] { 3 }, storage.ListSlots());

            storage.Delete(3);
            Assert.IsFalse(storage.Exists(3));
            Assert.IsFalse(File.Exists(storage.BackupPathFor(3)));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }
}
