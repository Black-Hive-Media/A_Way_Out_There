using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SpacePortUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject soldItemPrefab;
    [SerializeField] private PlayerCredits currentPlayerCredits;
    [SerializeField] private GameObject gameCam;
    public Animator animator;
    public void OpenMenu()
    {
        menuPanel.SetActive(true);
        //Swap Cameras:
        gameCam.SetActive(false);


        RefreshList();
    }
    public void CloseMenu()
    {

        //call animation
        animator.Play("leaving");

        StartCoroutine(LeavePort());


    }

    IEnumerator LeavePort() 
    {
        yield return new WaitForSeconds(5f);
        CargoManager.Instance.soldAllAndResetUI();
        gameCam.SetActive(true);
        menuPanel.SetActive(false);
    }

    public void RefreshList()
    {
        // 1. Clear existing UI items
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // 2. Spawn a button for every individual item in cargo
        List<CollectibleItem> items = CargoManager.Instance.cargoList;
        for (int i = 0; i < items.Count; i++)
        {
            GameObject btnObj = Instantiate(soldItemPrefab, contentParent);
            CollectibleItem item = items[i];

            // 3. Setup button visuals
            btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text =
                $"{item.itemName} - {item.baseValue} CR";

            // 4. Hook up the click event
            /*btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => {
                SellItem(index);
            });*/
            CargoManager.Instance.SellSpecificItem(i);
            Debug.Log(item.name);

            currentPlayerCredits.UpdateCredits();
        }
    }

}
