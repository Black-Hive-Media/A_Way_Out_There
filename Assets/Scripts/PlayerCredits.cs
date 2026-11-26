using UnityEngine;
using TMPro;

/// <summary>Shows a ship's credits in the TextMeshPro on this object when enabled or refreshed.</summary>
public class PlayerCredits : MonoBehaviour
{
    [Tooltip("Ship whose credits to show. Empty = the ship on a parent, else the only ship in the scene.")]
    [SerializeField] private Ship ship;

    private void OnEnable()
    {
        UpdateCredits();
    }

    public void UpdateCredits()
    {
        ship = Ship.Resolve(this, ship);
        if (ship == null)
            return;

        TextMeshProUGUI playerTMP = GetComponent<TextMeshProUGUI>();
        playerTMP.text = "Credits: \n" + ship.Credits.ToString();
    }
}
