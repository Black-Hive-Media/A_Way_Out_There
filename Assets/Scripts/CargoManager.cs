using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cargo HUD for one ship: fill bar, capacity text, "cargo full" banner, item icons and the
/// ship AI's cargo lines. Holds no cargo itself; it shows the bound Ship's cargo and reacts to
/// its events. To change cargo, call the Ship (AddCargo / RemoveCargoAt).
/// </summary>
public class CargoManager : MonoBehaviour
{
    [Tooltip("Ship this HUD shows. Empty = the ship on a parent, else the only ship in the scene.")]
    [SerializeField] private Ship ship;

    [HideInInspector, Tooltip("Unused: cargo now lives on the Ship. Kept until the move is confirmed.")]
    public List<CollectibleItem> cargoList = new List<CollectibleItem>();

    public TextMeshProUGUI cargoText;
    public GameObject cargoFullImg;
    public Image cargoFill;

    public Transform[] placeHolderIconPos;

    /// <summary>HUD icons, one per cargo entry, in the same order as the ship's cargo list.</summary>
    public List<GameObject> itemIcons = new List<GameObject>();

    private Animator dropdownAnimator;

    public Ship Ship => ship;

    private void Start()
    {
        dropdownAnimator = GetComponentInChildren<Animator>();
        Bind(Ship.Resolve(this, ship));
    }

    private void OnDestroy() => Unbind();

    /// <summary>Show a different ship (used by split-screen later).</summary>
    public void Bind(Ship newShip)
    {
        Unbind();
        ship = newShip;
        if (ship == null)
            return;

        ship.CargoAdded += OnCargoAdded;
        ship.CargoRemoved += OnCargoRemoved;
        ship.CargoRejected += OnCargoRejected;
        ship.CargoCapacityChanged += OnCargoCapacityChanged;

        RebuildIcons();
        UpdateCapacityText();
        UpdateFill();
    }

    private void Unbind()
    {
        if (ship == null)
            return;
        ship.CargoAdded -= OnCargoAdded;
        ship.CargoRemoved -= OnCargoRemoved;
        ship.CargoRejected -= OnCargoRejected;
        ship.CargoCapacityChanged -= OnCargoCapacityChanged;
    }

    // ---------------------------------------------------------------- Ship events

    private void OnCargoAdded(CollectibleItem item, int index)
    {
        AI_Manager.Instance.TriggerMessage("GenericPickup");
        UpdateFill();

        if (ship.IsCargoFull && !cargoFullImg.activeInHierarchy)
        {
            cargoFullImg.SetActive(true);
            AI_Manager.Instance.TriggerMessage("CargoBayFull");
        }

        PlayDropdown(cargoFullImg.activeInHierarchy ? "UI_dropdown_Stay" : "UI_dropdown");
        SpawnHudIcon(item, index);
    }

    private void OnCargoRemoved(CollectibleItem item, int index)
    {
        UpdateFill();
        HideFullBannerIfRoom();

        if (index >= 0 && index < itemIcons.Count)
        {
            if (itemIcons[index] != null)
                Destroy(itemIcons[index]);
            itemIcons.RemoveAt(index);
        }
    }

    private void OnCargoRejected(CollectibleItem item)
    {
        AI_Manager.Instance.TriggerMessage("CargoBayUpgradeNeeded");
    }

    private void OnCargoCapacityChanged(int newMax)
    {
        UpdateCapacityText();
        UpdateFill();
        HideFullBannerIfRoom();
        AI_Manager.Instance.TriggerMessage("CargoUpgraded");
    }

    // ---------------------------------------------------------------- HUD

    private void UpdateCapacityText()
    {
        if (cargoText != null)
            cargoText.text = "Cargo Capacity: " + ship.CargoUnitsMax;
    }

    private void UpdateFill()
    {
        if (cargoFill != null)
            cargoFill.fillAmount = ship.CargoUnitsMax > 0 ? Mathf.Clamp01((float)ship.CargoUnitsUsed / ship.CargoUnitsMax) : 0f;
    }

    private void HideFullBannerIfRoom()
    {
        if (cargoFullImg.activeInHierarchy && !ship.IsCargoFull)
        {
            cargoFullImg.SetActive(false);
            PlayDropdown("UI_dropdown_Rev");
        }
    }

    private void PlayDropdown(string state)
    {
        if (dropdownAnimator != null)
            dropdownAnimator.Play(state, 0);
    }

    private void SpawnHudIcon(CollectibleItem item, int index)
    {
        GameObject hudIcon = null;
        if (item.prefabForHud != null && placeHolderIconPos.Length > 0)
        {
            // Same slot rule as before: the slot matching the cargo count.
            Transform slot = placeHolderIconPos[Mathf.Clamp(ship.State.cargo.Count - 1, 0, placeHolderIconPos.Length - 1)];
            hudIcon = Instantiate(item.prefabForHud);
            hudIcon.transform.position = slot.position;
            hudIcon.transform.SetParent(slot);
        }
        // Keep the list lined up with the cargo list even if there's no icon.
        itemIcons.Insert(Mathf.Clamp(index, 0, itemIcons.Count), hudIcon);
    }

    private void RebuildIcons()
    {
        foreach (GameObject icon in itemIcons)
            if (icon != null)
                Destroy(icon);
        itemIcons.Clear();

        var cargo = ship.State.cargo;
        for (int i = 0; i < cargo.Count; i++)
        {
            GameObject hudIcon = null;
            if (cargo[i] != null && cargo[i].prefabForHud != null && i < placeHolderIconPos.Length)
            {
                hudIcon = Instantiate(cargo[i].prefabForHud);
                hudIcon.transform.position = placeHolderIconPos[i].position;
                hudIcon.transform.SetParent(placeHolderIconPos[i]);
            }
            itemIcons.Add(hudIcon);
        }
    }
}
