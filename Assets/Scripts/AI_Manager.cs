using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AI_Manager : MonoBehaviour
{ // 1. Singleton Instance
    public static AI_Manager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject messagePanel;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float displayDuration = 3f;
    [Range(0, 1)] public float snarkProbability = 0.3f;

    [Header("Data")]
    [SerializeField] private AIMessageDatabase database;

    private Dictionary<string, AI_Message> messageDict = new Dictionary<string, AI_Message>();
    private Coroutine typeRoutine;

    private void Awake()
    {
        // 2. Singleton Initialization
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

       /* // Initialize Dictionary for quick lookup via ID
        foreach (var msg in messageDatabase)
        {
            messageDict.Add(msg.messageID, msg);
        }*/
    }

    

    /// <summary>
    /// Call this from any script: AI_Manager.Instance.TriggerMessage("LowFuel");
    /// </summary>
    public void TriggerMessage(string id)
    {
        AI_Message data = database.GetMessage(id);
        if (data != null)
        {
           // if (messageDict.TryGetValue(id, out AI_Message data))
            //{
                string chosenText = "";

                // Procedural Logic: Snark vs. Helpful
                if (Random.value < snarkProbability && data.snarkyVariations.Length > 0)
                {
                    chosenText = data.snarkyVariations[Random.Range(0, data.snarkyVariations.Length)];
                }
                else
                {
                    chosenText = data.helpfulVariations[Random.Range(0, data.helpfulVariations.Length)];
                }

                // Start Typewriter
                if (typeRoutine != null) StopCoroutine(typeRoutine);
                typeRoutine = StartCoroutine(TypeText(chosenText));
            //}
        }
    }
   

    private IEnumerator TypeText(string text)
    {
        messagePanel.SetActive(true);
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(displayDuration);
        messagePanel.SetActive(false);
    }

}
