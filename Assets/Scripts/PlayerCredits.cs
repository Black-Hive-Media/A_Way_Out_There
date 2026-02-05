using UnityEngine;
using TMPro;
public class PlayerCredits : MonoBehaviour
{

    private void OnEnable()
    {
        UpdateCredits();
    }
    void Start()
    {
        
    }

    public void UpdateCredits()
    {
        //Declare and Set the reference to the TextMeshPro GameObject
        TextMeshProUGUI playerTMP = GetComponent<TextMeshProUGUI>();
        int playerCredits = WalletManager.Instance.GetCurrentBalance();
        //set the TMPro's .text to "Credits: " + playerCredits;
        playerTMP.text = "Credits: \n" + playerCredits.ToString();
    }

}
