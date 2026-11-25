using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reads and writes the trigger-key list inside AIMessageDatabase assets (AIDatabaseObj).
/// AI_Manager.TriggerMessage looks messages up by these keys, so new messages must be registered here.
/// </summary>
public static class AIMessageDatabaseEditorUtility
{
    private const string DataPath = "Messages.m_Data";

    public static List<AIMessageDatabase> FindDatabases()
    {
        var list = new List<AIMessageDatabase>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(AIMessageDatabase), new[] { "Assets" }))
        {
            var db = AssetDatabase.LoadAssetAtPath<AIMessageDatabase>(AssetDatabase.GUIDToAssetPath(guid));
            if (db != null)
                list.Add(db);
        }
        return list;
    }

    /// <summary>Message asset -> the first trigger key that points at it, across all databases.</summary>
    public static Dictionary<AI_Message, string> GetTriggerKeys()
    {
        var keys = new Dictionary<AI_Message, string>();
        foreach (AIMessageDatabase db in FindDatabases())
        {
            SerializedProperty data = new SerializedObject(db).FindProperty(DataPath);
            if (data == null)
                continue;
            for (int i = 0; i < data.arraySize; i++)
            {
                SerializedProperty element = data.GetArrayElementAtIndex(i);
                var msg = element.FindPropertyRelative("value").objectReferenceValue as AI_Message;
                string key = element.FindPropertyRelative("key").stringValue;
                if (msg != null && !string.IsNullOrEmpty(key) && !keys.ContainsKey(msg))
                    keys.Add(msg, key);
            }
        }
        return keys;
    }

    /// <summary>
    /// Adds key -> msg to every AIMessageDatabase that lacks the key. Returns the number of
    /// databases changed. Warns when the key already points at a different message.
    /// </summary>
    public static int Register(AI_Message msg, string key)
    {
        int changed = 0;
        foreach (AIMessageDatabase db in FindDatabases())
        {
            var so = new SerializedObject(db);
            SerializedProperty data = so.FindProperty(DataPath);
            if (data == null)
                continue;

            bool found = false;
            for (int i = 0; i < data.arraySize && !found; i++)
            {
                SerializedProperty element = data.GetArrayElementAtIndex(i);
                if (!string.Equals(element.FindPropertyRelative("key").stringValue, key, StringComparison.OrdinalIgnoreCase))
                    continue;
                found = true;
                var existing = element.FindPropertyRelative("value").objectReferenceValue;
                if (existing != msg)
                    Debug.LogWarning($"[Content] {db.name}: trigger key '{key}' already points at '{(existing ? existing.name : "nothing")}', not '{msg.name}'. Left as is.", db);
            }
            if (found)
                continue;

            int index = data.arraySize;
            data.InsertArrayElementAtIndex(index);
            SerializedProperty added = data.GetArrayElementAtIndex(index);
            added.FindPropertyRelative("key").stringValue = key;
            added.FindPropertyRelative("value").objectReferenceValue = msg;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            changed++;
        }
        return changed;
    }
}
