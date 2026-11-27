using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Shared drawing for the play-mode cargo and ledger views.</summary>
internal static class SessionInspectorGUI
{
    public const int LedgerRowsShown = 20;

    private static GUIStyle mono;
    private static GUIStyle Mono => mono ??= new GUIStyle(EditorStyles.miniLabel)
    {
        font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New" }, 11),
        richText = false,
    };

    public static void NotPlaying()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Enter Play mode to see cargo instances and ledger rows.", MessageType.None);
    }

    public static void Cargo(Ship ship)
    {
        ShipState state = ship.State;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Cargo: {state.cargo.Count} item(s), {state.CargoUnitsUsed}/{state.cargoUnitsMax} units   Credits: {state.credits}",
            EditorStyles.boldLabel);

        if (state.cargo.Count == 0)
        {
            EditorGUILayout.LabelField("  (empty)", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.LabelField("  item                     id        source  system     acquired  cond", Mono);
        foreach (ItemInstance item in state.cargo)
        {
            string line = $"  {item.itemId,-24} {item.ShortId}  {item.source,-6}  {item.acquiredSystemId,-9}  {item.acquiredAt,7:0.0}s  {item.condition:0.00}";
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, new GUIContent(line, $"instanceId: {item.instanceId}\nshipId: {item.shipId}"), Mono);
        }
    }

    public static void Ledger(IReadOnlyList<LedgerRow> newestFirst, string title)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        if (newestFirst.Count == 0)
        {
            EditorGUILayout.LabelField("  (no rows yet)", EditorStyles.miniLabel);
            return;
        }
        foreach (LedgerRow row in newestFirst)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rect, new GUIContent("  " + row, $"instanceId: {row.instanceId}"), Mono);
        }
    }
}

[CustomEditor(typeof(Ship))]
public class ShipInspector : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            SessionInspectorGUI.NotPlaying();
            return;
        }

        var ship = (Ship)target;
        SessionInspectorGUI.Cargo(ship);

        GameSession session = GameSession.Current;
        if (session == null)
            return;
        string id = ship.Id;
        List<LedgerRow> rows = session.Ledger.Latest(SessionInspectorGUI.LedgerRowsShown,
            r => r.fromShipId == id || r.toShipId == id);
        SessionInspectorGUI.Ledger(rows, $"Ledger for {id} (last {SessionInspectorGUI.LedgerRowsShown}, newest first)");
    }
}

[CustomEditor(typeof(GameSession))]
public class GameSessionInspector : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            SessionInspectorGUI.NotPlaying();
            return;
        }

        var session = (GameSession)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Sim time: {session.Clock.Now:0.0}s   System: {session.CurrentSystemId}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Ledger rows: {session.Ledger.rows.Count}   Schema: {session.Universe.schemaVersion}", EditorStyles.miniLabel);

        foreach (Ship ship in FindObjectsByType<Ship>())
            SessionInspectorGUI.Cargo(ship);

        SessionInspectorGUI.Ledger(session.Ledger.Latest(SessionInspectorGUI.LedgerRowsShown),
            $"Ledger (last {SessionInspectorGUI.LedgerRowsShown}, newest first)");
    }
}
