using System.Collections.Generic;
using UnityEngine;
public class SpacePortUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemButtonPrefab;
    [SerializeField] private CollectibleItem.DestinationPort currentPortType;

    public void OpenMenu()//(CollectibleItem.DestinationPort portType)
    {
        currentPortType = CollectibleItem.DestinationPort.SettlementPort;//portType;
        menuPanel.SetActive(true);
        RefreshList();
    }

    public void RefreshList()
    {
        // 1. Clear existing UI items
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 2. Spawn a button for every individual item in cargo
        List<CollectibleItem> items = CargoManager.Instance.cargoList;
        for (int i = 0; i < items.Count; i++)
        {
            GameObject btnObj = Instantiate(itemButtonPrefab, contentParent);
            CollectibleItem item = items[i];
            int index = i; // Local copy for the button click

            // 3. Setup button visuals
            btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text =
                $"{item.itemName} - {item.baseValue} CR";

            // 4. Hook up the click event
            btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                SellItem(index);
            });

            // Optional: Dim buttons for items that don't belong at this port
            if (item.targetPort != currentPortType)
            {
                //btnObj.GetComponent<UnityEngine.UI.Image>().color = Color.gray;
            }
        }
    }

    private void SellItem(int index)
    {
        CargoManager.Instance.SellSpecificItem(index);
        RefreshList(); 
    }
}
