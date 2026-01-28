using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class CargoManager : MonoBehaviour
{
    public static CargoManager Instance;

    int currCapacity = 2;

    public List<CollectibleItem> cargoList = new List<CollectibleItem>();

    public TextMeshProUGUI cargoText;
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
    }
    public bool cargoCheck(CollectibleItem newItem) 
    {
        if ((newItem.cargoUnitsRequired + cargoList.Count) < currCapacity) 
        {
            addCargo(newItem);
            
            return true;

        }
        return false;
    }

    void addCargo(CollectibleItem newItem) 
    {
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

        cargoList.RemoveAt(index);

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
