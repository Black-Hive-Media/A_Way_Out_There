using TMPro;
using UnityEngine;

/// <summary>
/// Credits display for one ship. Holds no money itself; it shows the bound Ship's wallet and
/// reacts to its CreditsChanged event. To change credits, call Ship.AddCredits / TrySpendCredits.
/// </summary>
public class WalletManager : MonoBehaviour
{
    [Tooltip("Ship this display shows. Empty = the ship on a parent, else the only ship in the scene.")]
    [SerializeField] private Ship ship;

    [Tooltip("Unused at runtime: starting credits now come from the Ship. Kept until the move is confirmed.")]
    [SerializeField] private PlayerWallet walletData;

    [Tooltip("Text to update. Empty = SpacePortManager's player credits text.")]
    [SerializeField] private TextMeshProUGUI creditsText;

    [Tooltip("A single sale above this triggers the ship AI's BigProfit line.")]
    [SerializeField] private int bigProfitThreshold = 1000;

    public Ship Ship => ship;

    private void Start()
    {
        if (creditsText == null && SpacePortManager.Instance != null)
            creditsText = SpacePortManager.Instance.playerCreditsTxt;
        Bind(Ship.Resolve(this, ship));
    }

    private void OnDestroy() => Unbind();

    public void Bind(Ship newShip)
    {
        Unbind();
        ship = newShip;
        if (ship != null)
            ship.CreditsChanged += OnCreditsChanged;
    }

    private void Unbind()
    {
        if (ship != null)
            ship.CreditsChanged -= OnCreditsChanged;
    }

    private void OnCreditsChanged(int balance, int change)
    {
        if (creditsText != null)
            creditsText.text = "Credits: " + balance;

        if (change > bigProfitThreshold)
            AI_Manager.Instance.TriggerMessage("BigProfit");
    }
}
