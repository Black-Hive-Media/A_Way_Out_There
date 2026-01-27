using System.Collections.Generic;
using UnityEngine;

public class CargoManager : MonoBehaviour
{
    public static CargoManager Instance;

    int currItemCount = 0;
    int currCapacity = 2;

    public List<CollectibleItem> cargoList = new List<CollectibleItem>();
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
        
    }
    public bool cargoCheck(CollectibleItem newItem) 
    {
        if ((newItem.cargoUnitsRequired + currItemCount) < currCapacity) 
        {
            addCargo(newItem);
            
            return true;

        }
        return false;
    }

    void addCargo(CollectibleItem newItem) 
    {
        currItemCount += newItem.cargoUnitsRequired;
        AI_Manager.Instance.TriggerMessage("GenericPickup");
        cargoList.Add(newItem);
    }
    // Logic to sell ONE specific item by its index in the UI list
    public void SellSpecificItem(int index)
    {
        if (index < 0 || index >= cargoList.Count) return;

        CollectibleItem itemToSell = cargoList[index];

        // 1. Give money (assuming a separate WalletManager exists)
        WalletManager.Instance.AddCredits(itemToSell.baseValue);

        // 2. Free up space and remove
        currItemCount -= itemToSell.cargoUnitsRequired;
        cargoList.RemoveAt(index);

        AI_Manager.Instance.TriggerMessage("ItemSold");
    }

    // Upgrade System: Call this when buying a bigger cargo hold
    public void UpgradeCargoCapacity(int additionalUnits)
    {
        currCapacity += additionalUnits;
        AI_Manager.Instance.TriggerMessage("CargoUpgraded");
    }

}
