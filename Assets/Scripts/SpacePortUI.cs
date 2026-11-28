using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SpacePortUI : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject soldItemPrefab;
    [SerializeField] private PlayerCredits currentPlayerCredits;
    [SerializeField] private GameObject gameCam;
    [Tooltip("Fallback ship when none is docked. Empty = the only ship in the scene.")]
    [SerializeField] private Ship ship;
    [Tooltip("Sell all cargo automatically 2 seconds after docking (the old behaviour). Off = sell with the item buttons or a Sell All button.")]
    [SerializeField] private bool autoSellOnDock = false;
    public Animator animator;

    [Header("Pages")]
    [Tooltip("Objects shown on the Sell page: the Inventory list, the Sell All button...")]
    [SerializeField] private GameObject[] sellPage = new GameObject[0];
    [Tooltip("Objects shown on the Upgrades page: the upgrade buttons.")]
    [SerializeField] private GameObject[] upgradesPage = new GameObject[0];
    [Tooltip("Tab button that opens the Sell page (greyed out while that page is showing).")]
    [SerializeField] private Button sellTab;
    [Tooltip("Tab button that opens the Upgrades page (greyed out while that page is showing).")]
    [SerializeField] private Button upgradesTab;
    [Tooltip("Optional: greyed out when there's nothing to sell.")]
    [SerializeField] private Button sellAllButton;
    [Tooltip("Optional: text shown on the Sell page when the hold is empty.")]
    [SerializeField] private GameObject emptyCargoMessage;

    private bool showingUpgrades;

    /// <summary>The sell button for each cargo instance, by instanceId.</summary>
    private readonly Dictionary<string, GameObject> sellButtons = new Dictionary<string, GameObject>();

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
        ShowSellPage();
        GameSession.Autosave("dock");
    }

    /// <summary>Shows the item list and Sell All; hides the upgrade buttons. Hook the Sell tab here.</summary>
    public void ShowSellPage() => ShowPage(upgrades: false);

    /// <summary>Shows the upgrade buttons; hides the item list. Hook the Upgrades tab here.</summary>
    public void ShowUpgradesPage() => ShowPage(upgrades: true);

    private void ShowPage(bool upgrades)
    {
        showingUpgrades = upgrades;
        foreach (GameObject go in sellPage)
            if (go != null) go.SetActive(!upgrades);
        foreach (GameObject go in upgradesPage)
            if (go != null) go.SetActive(upgrades); // enabling an UpgradeButton refreshes its price text
        if (sellTab != null) sellTab.interactable = upgrades;
        if (upgradesTab != null) upgradesTab.interactable = !upgrades;
        UpdateSellControls();
    }

    /// <summary>Greys out Sell All and shows the empty message when there's no cargo.</summary>
    private void UpdateSellControls()
    {
        Ship current = CurrentShip;
        bool hasCargo = current != null && current.State.cargo.Count > 0;
        if (sellAllButton != null)
            sellAllButton.interactable = hasCargo;
        if (emptyCargoMessage != null)
            emptyCargoMessage.SetActive(!showingUpgrades && !hasCargo);
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
        sellButtons.Clear();

        // 2. Spawn a button for every individual item in cargo
        Ship current = CurrentShip;
        if (current == null)
            return;

        foreach (ItemInstance item in current.State.cargo)
        {
            CollectibleItem definition = item.Definition;
            GameObject btnObj = Instantiate(soldItemPrefab, contentParent);
            btnObj.name = $"Sell {item.itemId} [{item.ShortId}]";

            // 3. Setup button visuals: the template has a "Name" and a "Value" text
            string itemName = definition != null ? definition.itemName : item.itemId;
            string value = definition != null ? $"{definition.baseValue} CR" : "?";
            SetText(btnObj, "Name", itemName, out bool hasName);
            SetText(btnObj, "Value", value, out bool hasValue);
            if (!hasName && !hasValue)
                btnObj.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"{itemName} - {value}";

            // 4. Hook up the click event, keyed by the instance (not its position in the list)
            Button button = btnObj.GetComponent<Button>();
            if (button == null)
            {
                button = btnObj.AddComponent<Button>();
                button.targetGraphic = btnObj.GetComponentInChildren<Graphic>();
            }
            string instanceId = item.instanceId;
            button.onClick.AddListener(() => SellItem(instanceId));
            sellButtons[instanceId] = btnObj;
        }

        UpdateSellControls();

        if (autoSellOnDock)
            StartCoroutine(sellAll());
    }

    IEnumerator sellAll() 
    {
        yield return new WaitForSeconds(2.0f);
        SellAll();
    }

    /// <summary>Sells one cargo instance at its base value (markets are step 6).</summary>
    public void SellItem(string instanceId)
    {
        if (Sell(instanceId))
        {
            AI_Manager.Instance.TriggerMessage("ItemSold");
            RefreshCredits();
        }
    }

    /// <summary>Sells everything in the docked ship's cargo at base value. Hook a Sell All button here.</summary>
    public void SellAll()
    {
        Ship current = CurrentShip;
        if (current == null)
            return;

        int soldCount = 0;
        foreach (ItemInstance item in current.State.cargo.ToArray())
            if (Sell(item.instanceId))
                soldCount++;

        if (soldCount > 0)
        {
            AI_Manager.Instance.TriggerMessage("ItemSold");
            RefreshCredits();
        }
    }

    /// <summary>Removes the instance from the ship, pays for it, writes the ledger row and removes its button.</summary>
    private bool Sell(string instanceId)
    {
        Ship current = CurrentShip;
        if (current == null)
            return false;

        ItemInstance sold = current.RemoveCargo(instanceId);
        if (sold == null)
            return false;

        CollectibleItem definition = sold.Definition;
        int price = definition != null ? definition.baseValue : 0;
        if (definition == null)
            Debug.LogWarning($"Sold {sold} for 0: its item '{sold.itemId}' isn't in the ContentDatabase.");
        current.AddCredits(price);

        string portId = SpacePortManager.Instance != null ? SpacePortManager.Instance.PortId : "";
        GameSession session = GameSession.Current;
        if (session != null)
            session.Record(LedgerEvent.Sold, sold, fromShipId: current.Id, portId: portId, price: price);

        if (sellButtons.TryGetValue(instanceId, out GameObject button))
        {
            if (button != null)
                Destroy(button);
            sellButtons.Remove(instanceId);
        }
        UpdateSellControls();
        return true;
    }

    /// <summary>Sets the TextMeshPro on the child called childName (or its children).</summary>
    private static void SetText(GameObject root, string childName, string text, out bool found)
    {
        found = false;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != childName)
                continue;
            var tmp = t.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp == null)
                continue;
            tmp.text = text;
            found = true;
            return;
        }
    }

    private void RefreshCredits()
    {
        if (currentPlayerCredits != null)
            currentPlayerCredits.UpdateCredits();
    }
}
