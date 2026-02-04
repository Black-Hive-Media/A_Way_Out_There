using UnityEngine;
using TMPro;
public class PlayerCredits : MonoBehaviour
{

    private void OnEnable()
    {
        //Declare and Set the reference to the TextMeshPro GameObject
        int playerCredits = WalletManager.Instance.GetCurrentBalance();
        //set the TMPro's .text to "Credits: " + playerCredits;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

}
