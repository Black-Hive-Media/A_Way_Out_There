using UnityEngine;

[CreateAssetMenu(fileName = "AIDatabase", menuName = "AI/Message Database")]
    public class AIMessageDatabase : ScriptableObject
    {
        public AIMessageDictionary Messages;

        public AI_Message GetMessage(string id)
        {
            if (Messages.Dict.TryGetValue(id, out AI_Message msg))
                return msg;

            Debug.LogWarning($"AI Message ID '{id}' not found in database!");
            return null;
        }
    }

