using UnityEngine;


public class UpgradeButton : MonoBehaviour
{
    public ShipUpgrade myUpgrade;

    public void OnClick()
    {
        StoreManager.Instance.AttemptPurchase(myUpgrade);
    }
}


