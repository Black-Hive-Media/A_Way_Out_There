using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AIMessageListWrapper
{
    // This variable name must match the key "messages" in the JSON
    public List<AI_MessageData> messages;
}

[System.Serializable]
public class AI_MessageData
{
    public string messageID;
    public string[] helpfulVariations;
    public string[] snarkyVariations;
    public float snarkThreshold;
}
public class AIDataImporter : MonoBehaviour
{
    public TextAsset jsonFile; // Drag your .json here in Inspector

    [ContextMenu("Import AI Messages")]
    public void Import()
    {
        AIMessageListWrapper data = JsonUtility.FromJson<AIMessageListWrapper>(jsonFile.text);

        foreach (var msgData in data.messages)
        {
            // Create a new instance of your ScriptableObject
            AI_Message asset = ScriptableObject.CreateInstance<AI_Message>();

            // Populate it with the JSON data
            asset.messageID = msgData.messageID;
            asset.helpfulVariations = msgData.helpfulVariations;
            asset.snarkyVariations = msgData.snarkyVariations;
            asset.snarkThreshold = msgData.snarkThreshold;

            // Save it to the project files (UnityEditor only)
#if UNITY_EDITOR
            string path = $"Assets/Data/AI/Msg_{msgData.messageID}.asset";
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
#endif
        }
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.SaveAssets();
#endif
        Debug.Log("Import complete!");
    }
}
