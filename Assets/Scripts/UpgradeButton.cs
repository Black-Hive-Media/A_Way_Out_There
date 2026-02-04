using UnityEngine;
using TMPro;
using System.Data.Common;

public class UpgradeButton : MonoBehaviour
{
    public ShipUpgrade[] myUpgrade;

    private TextMeshProUGUI textObj;

    public ShipUpgrade.UpgradeType upgradeType;

    private void OnEnable()
    {
        //set next upgrade's name and amount
        //
        if (textObj == null)
        {
            textObj = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (upgradeType == ShipUpgrade.UpgradeType.MiningLaser)
        {

            if (StoreManager.Instance.playerLaser.currentStats.name.CompareTo("Tier1Laser") == 0)
            {
                textObj.text = myUpgrade[0].name + " Cost: " + myUpgrade[0].cost;

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
                            textObj.text = myUpgrade[newUpgrade].name + " Cost: " + myUpgrade[newUpgrade].cost;
                            break;
                        }

                    }
                }
            }
        }

    }
    public void OnClick()
    {
        if (upgradeType == ShipUpgrade.UpgradeType.MiningLaser)
        {
           
            if (StoreManager.Instance.playerLaser.currentStats.name.CompareTo("Tier1Laser") == 0)
            {
                Debug.Log("here1111");
                //first upgrade
                StoreManager.Instance.AttemptPurchase(myUpgrade[0]);

            }
            else
            {
                for (int i = 0; i < myUpgrade.Length; i++)
                {
                    Debug.Log(myUpgrade[i].upgradeData.name + " h  " + StoreManager.Instance.playerLaser.currentStats.name);

                    if (myUpgrade[i].upgradeData.name == StoreManager.Instance.playerLaser.currentStats.name)
                    {
                        int newUpgrade = (i + 1);
                        if (newUpgrade < myUpgrade.Length)
                        {
                            Debug.Log("here222222");
                            StoreManager.Instance.AttemptPurchase(myUpgrade[newUpgrade]);
                            break;
                        }
                        else
                        {
                            Debug.Log("hereeeeeeeee");
                            AI_Manager.Instance.TriggerMessage("AlreadyOwnAllUpgrades");
                        }

                    }
                }
            }
        }

    }
}


