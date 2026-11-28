using UnityEngine;
using TMPro;

/// <summary>
/// Shows a ship's credits in the TextMeshPro on this object, and keeps it current while enabled
/// (sales, purchases, anything that changes the wallet).
/// </summary>
public class PlayerCredits : MonoBehaviour
{
    [Tooltip("Ship whose credits to show. Empty = the ship on a parent, else the only ship in the scene.")]
    [SerializeField] private Ship ship;

    private Ship listeningTo;

    private void OnEnable()
    {
        ship = Ship.Resolve(this, ship);
        if (ship != null)
        {
            ship.CreditsChanged += OnCreditsChanged;
            ship.StateReplaced += UpdateCredits;
            listeningTo = ship;
        }
        UpdateCredits();
    }

    private void OnDisable()
    {
        if (listeningTo != null)
        {
            listeningTo.CreditsChanged -= OnCreditsChanged;
            listeningTo.StateReplaced -= UpdateCredits;
        }
        listeningTo = null;
    }

    private void OnCreditsChanged(int balance, int change) => UpdateCredits();

    public void UpdateCredits()
    {
        ship = Ship.Resolve(this, ship);
        if (ship == null)
            return;

        TextMeshProUGUI playerTMP = GetComponent<TextMeshProUGUI>();
        playerTMP.text = "Credits: \n" + ship.Credits.ToString();
    }
}
