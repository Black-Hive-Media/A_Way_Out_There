using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CargoManager : MonoBehaviour
{
    public static CargoManager Instance;

    int currCapacity = 3;

    public List<CollectibleItem> cargoList = new List<CollectibleItem>();

    public TextMeshProUGUI cargoText;
    public GameObject cargoFullImg;
    public Image cargoFill;

    public Transform[] placeHolderIconPos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cargoText.text = "Cargo Capacity: " + currCapacity;

        float tempFillAmt = 0;
        for (int i = 0; i < cargoList.Count; i++)
        {
            tempFillAmt += cargoList[i].cargoUnitsRequired;
        }
        cargoFill.fillAmount = tempFillAmt;
    }
    public bool cargoCheck(CollectibleItem newItem) 
    {
        float tempFillAmt = 0;
        for (int i = 0; i < cargoList.Count; i++)
        {
            tempFillAmt += cargoList[i].cargoUnitsRequired;
        }
        if (((float)newItem.cargoUnitsRequired + tempFillAmt) <= (float)currCapacity) 
        {
            cargoFill.fillAmount = ((tempFillAmt + (float)newItem.cargoUnitsRequired) / (float)currCapacity);
            addCargo(newItem);
            return true;
        }
       
        AI_Manager.Instance.TriggerMessage("CargoBayUpgradeNeeded");
        return false;
    }

    void addCargo(CollectibleItem newItem) 
    {
        AI_Manager.Instance.TriggerMessage("GenericPickup");

        cargoList.Add(newItem);


        if (cargoFill.fillAmount == 1)
        {
            if (cargoFullImg.activeInHierarchy == false)
            {
                cargoFullImg.SetActive(true);
                AI_Manager.Instance.TriggerMessage("CargoBayFull");
                
            }
        }

        //fill the icon in the hud now
        SpawnHudIcon(newItem);
    }

    void SpawnHudIcon(CollectibleItem newItem)
    {
        // 1. Get from your ItemPoolManager (ensure  are in the pool array)
        GameObject hudIcon = Instantiate(newItem.prefabForHud);

        // 2. Position it off-screen
        hudIcon.transform.position = placeHolderIconPos[cargoList.Count-1].position;

        hudIcon.transform.SetParent(placeHolderIconPos[cargoList.Count-1]);
    }
    // Logic to sell ONE specific item by its index in the UI list
    public void SellSpecificItem(int index)
    {
        if (index < 0 || index >= cargoList.Count) return;

        CollectibleItem itemToSell = cargoList[index];

        // 1. Give money (assuming a separate WalletManager exists)
        WalletManager.Instance.AddCredits(itemToSell.baseValue);

        cargoList.RemoveAt(index);

        if (cargoFullImg.activeInHierarchy == true)
        {
            cargoFullImg.SetActive(false);
        }

        AI_Manager.Instance.TriggerMessage("ItemSold");
    }

    // Upgrade System: Call this when buying a bigger cargo hold
    public void UpgradeCargoCapacity(int additionalUnits)
    {
        currCapacity += additionalUnits;
        AI_Manager.Instance.TriggerMessage("CargoUpgraded");
        cargoText.text = "Cargo Capacity: " + currCapacity;
    }

}
