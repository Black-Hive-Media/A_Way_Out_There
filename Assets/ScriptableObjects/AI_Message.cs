using UnityEngine;
[CreateAssetMenu(fileName = "NewMessage", menuName = "AI/Message")]

public class AI_Message : ScriptableObject
{
    //public string messageID;
    [TextArea(3, 10)] public string[] helpfulVariations;
    [TextArea(3, 10)] public string[] snarkyVariations;
    public float snarkThreshold = 0.5f; // Random chance to be snarky
}
