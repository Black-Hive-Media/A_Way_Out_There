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
    [Tooltip("Fallback ship when none is docked. Empty = the only ship in the scene.")]
    [SerializeField] private Ship ship;
    public Animator animator;

    /// <summary>The ship at the port (one ship for now; several is step 11).</summary>
    private Ship CurrentShip
    {
        get
        {
            Ship docked = SpacePortManager.Instance != null ? SpacePortManager.Instance.DockedShip : null;
            if (docked != null)
                return docked;
            ship = Ship.Resolve(this, ship);
            return ship;
        }
    }
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
        gameCam.SetActive(true);
        menuPanel.SetActive(false);
    }

    public void RefreshList()
    {
        // 1. Clear existing UI items
        foreach (Transform child in contentParent) Destroy(child.gameObject);
        ;
        // 2. Spawn a button for every individual item in cargo
        Ship current = CurrentShip;
        if (current == null)
            return;
        List<CollectibleItem> items = current.State.cargo;
       
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
            //SellItemAt(i);
            

            //currentPlayerCredits.UpdateCredits();
        }

        StartCoroutine(sellAll());
    }

    IEnumerator sellAll() 
    {
        yield return new WaitForSeconds(2.0f);
        SellAll();

        yield return new WaitForSeconds(2.0f);
        currentPlayerCredits.UpdateCredits();
    }

    /// <summary>Sells one cargo entry at its base value. Not wired to a button yet.</summary>
    public void SellItemAt(int index)
    {
        Ship current = CurrentShip;
        if (current == null)
            return;

        CollectibleItem sold = current.RemoveCargoAt(index);
        if (sold == null)
            return;

        current.AddCredits(sold.baseValue);
        AI_Manager.Instance.TriggerMessage("ItemSold");
    }

    /// <summary>Sells everything in the docked ship's cargo at base value.</summary>
    public void SellAll()
    {
        Ship current = CurrentShip;
        if (current == null)
            return;

        int soldCount = 0;
        for (int i = current.State.cargo.Count - 1; i >= 0; i--)
        {
            CollectibleItem sold = current.RemoveCargoAt(i);
            if (sold == null)
                continue;
            current.AddCredits(sold.baseValue);
            soldCount++;
        }

        if (soldCount > 0)
            AI_Manager.Instance.TriggerMessage("ItemSold");
    }
}
