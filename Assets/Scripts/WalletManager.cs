using UnityEngine;
using System;
using TMPro;
public class WalletManager : MonoBehaviour
{
    public static WalletManager Instance { get; private set; }

    [SerializeField] private PlayerWallet walletData;

    public TextMeshProUGUI creditsText; 

    // Use an Action to notify the UI whenever the balance changes
    public static event Action<int> OnBalanceChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddCredits(int amount)
    {
        walletData.credits += amount;
        OnBalanceChanged?.Invoke(walletData.credits);

        //creditsText.text = "Credits: " + walletData.credits;
        // AI Reaction for big scores
        if (amount > 1000) AI_Manager.Instance.TriggerMessage("BigProfit");
    }

    public bool TrySpendCredits(int cost)
    {
        if (walletData.credits >= cost)
        {
            walletData.credits -= cost;
            OnBalanceChanged?.Invoke(walletData.credits);
            creditsText.text = "Credits: " + walletData.credits;
            return true; // Purchase successful
        }

        AI_Manager.Instance.TriggerMessage("InsufficientFunds");
        return false; // Not enough money
    }

    public int GetCurrentBalance() => walletData.credits;
}
