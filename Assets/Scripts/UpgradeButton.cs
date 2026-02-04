using UnityEngine;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    public ShipUpgrade[] myUpgrade;

    private void OnEnable()
    {
        //set next upgrade's name and amount
        //
        if (StoreManager.Instance.playerLaser.currentStats.name.CompareTo("Tier1Laser") == 0) 
        {
           // GetComponentInChildren<TextMeshPro>().text = myUpgrade[0].name + " Cost: " + myUpgrade[0].cost;

        }
        else
        {
            for (int i = 0; i < myUpgrade.Length; i++)
            {
                Debug.Log(myUpgrade[i].upgradeData.name + "   " + StoreManager.Instance.playerLaser.currentStats.name);

                if (myUpgrade[i].upgradeData.name == StoreManager.Instance.playerLaser.currentStats.name)
                {
                    int newUpgrade = (i + 1);
                    if (newUpgrade < myUpgrade.Length)
                    {
                        GetComponentInChildren<TextMeshPro>().text = myUpgrade[newUpgrade].name + " Cost: " + myUpgrade[newUpgrade].cost;
                        break;
                    }

                }
            }
        }
        
    }
    public void OnClick()
    {
        for (int i = 0; i < myUpgrade.Length; i++) 
        {
            if (myUpgrade[i].upgradeData.name == StoreManager.Instance.playerLaser.currentStats.name) 
            {
                int newUpgrade = (i + 1);
                if (newUpgrade < myUpgrade.Length)
                {
                    StoreManager.Instance.AttemptPurchase(myUpgrade[newUpgrade]);
                    break;
                }
                else 
                {
                    AI_Manager.Instance.TriggerMessage("AlreadyOwnAllUpgrades");
                }
                
            }
        }
       
    }
}


