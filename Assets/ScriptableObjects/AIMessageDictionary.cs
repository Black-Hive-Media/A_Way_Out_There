using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class AIMessageDictionary : ISerializationCallbackReceiver
{
    // The "Internal" dictionary used in code for O(1) lookups
    public Dictionary<string, AI_Message> m_Dict = new Dictionary<string, AI_Message>();

    // These lists are what you actually see and edit in the Unity Inspector
    //[SerializeField] private List<string> keys = new List<string>();
    //[SerializeField] private List<AI_Message> values = new List<AI_Message>();

    //private Dictionary<TKey, TValue> m_Dict = new Dictionary<TKey, TValue>();
    public Dictionary<string, AI_Message> Dict => m_Dict;

    [System.Serializable]
    public class KeyValue
    {
        public string key;
        public AI_Message value;
    }
    [SerializeField]
    private List<KeyValue> m_Data = new List<KeyValue>();
    // Unity calls this before saving/serializing the object
    /*public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var pair in Dict)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // Unity calls this when loading the object in the editor or at runtime
    public void OnAfterDeserialize()
    {
        Dict.Clear();
        for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
        {
            Dict[keys[i]] = values[i];
        }
    }*/
    public void OnAfterDeserialize()
    {
      //  m_Dict.Clear();
        //foreach (var kv in m_Data)
          //  m_Dict.Add(kv.key, kv.value);

        m_Dict.Clear(); // Critical step to avoid duplicates on reload
        for (int i = 0; i < m_Data.Count; i++)
        {
            // Optional: Check for duplicates before adding to prevent the crash
            if (!m_Dict.ContainsKey(m_Data[i].key))
            {
                m_Dict.Add(m_Data[i].key, m_Data[i].value);
               // m_Dict.Add(kv.key, kv.value);
            }
        }
    }

    public void OnBeforeSerialize()
    {
        //m_Data.Clear();

        // foreach (var kv in m_Dict)
        //  m_Data.Add(new KeyValue { key = kv.Key, value = kv.Value });
        foreach (var kv in m_Dict)
            TryAdd(m_Data, kv.Key, kv.Value);
    }
    private void TryAdd(List<KeyValue> list, string key, AI_Message value)
    {
        // Check if the key already exists (case-insensitive)
        if (!list.Any(kvp => kvp.key.Equals(key, StringComparison.OrdinalIgnoreCase)))
        {
            list.Add(new KeyValue { key = key, value = value });
        }
    }
}
