using UnityEngine;

[CreateAssetMenu(fileName = "PlayerWallet", menuName = "Systems/Wallet")]
public class PlayerWallet : ScriptableObject
{
    public int credits;

    // Optional: Reset for new games
    public void Initialize(int startingCredits) => credits = startingCredits;
}
