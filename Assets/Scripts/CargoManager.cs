using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cargo HUD for one ship: fill bar, capacity text, "cargo full" banner, item icons and the
/// ship AI's cargo lines. Holds no cargo itself; it shows the bound Ship's cargo and reacts to
/// its events. To change cargo, call the Ship (AddCargo / RemoveCargo).
/// </summary>
public class CargoManager : MonoBehaviour
{
    [Tooltip("Ship this HUD shows. Empty = the ship on a parent, else the only ship in the scene.")]
    [SerializeField] private Ship ship;

    public TextMeshProUGUI cargoText;
    public GameObject cargoFullImg;
    public Image cargoFill;

    public Transform[] placeHolderIconPos;

    private class HudIcon
    {
        public GameObject icon;
        public int slot = -1;
    }

    /// <summary>HUD icon and slot for each cargo instance, by instanceId.</summary>
    private readonly Dictionary<string, HudIcon> iconsById = new Dictionary<string, HudIcon>();

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

    private void OnCargoAdded(ItemInstance item)
    {
        AI_Manager.Instance.TriggerMessage("GenericPickup");
        UpdateFill();

        if (ship.IsCargoFull && !cargoFullImg.activeInHierarchy)
        {
            cargoFullImg.SetActive(true);
            AI_Manager.Instance.TriggerMessage("CargoBayFull");
        }

        PlayDropdown(cargoFullImg.activeInHierarchy ? "UI_dropdown_Stay" : "UI_dropdown");
        SpawnHudIcon(item);
    }

    private void OnCargoRemoved(ItemInstance item)
    {
        UpdateFill();
        HideFullBannerIfRoom();

        if (iconsById.TryGetValue(item.instanceId, out HudIcon hud))
        {
            if (hud.icon != null)
                Destroy(hud.icon);
            iconsById.Remove(item.instanceId);
        }
    }

    private void OnCargoRejected(ItemInstance item)
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

    /// <summary>Icon in the first free slot, keyed by the instance id.</summary>
    private void SpawnHudIcon(ItemInstance item)
    {
        var hud = new HudIcon { slot = FirstFreeSlot() };
        CollectibleItem definition = item.Definition;
        if (definition != null && definition.prefabForHud != null && hud.slot >= 0)
        {
            Transform slot = placeHolderIconPos[hud.slot];
            hud.icon = Instantiate(definition.prefabForHud);
            hud.icon.name = $"{definition.prefabForHud.name} [{item.ShortId}]";
            hud.icon.transform.position = slot.position;
            hud.icon.transform.SetParent(slot);
        }
        iconsById[item.instanceId] = hud;
    }

    private int FirstFreeSlot()
    {
        for (int slot = 0; slot < placeHolderIconPos.Length; slot++)
        {
            bool used = false;
            foreach (HudIcon hud in iconsById.Values)
                if (hud.slot == slot) { used = true; break; }
            if (!used)
                return slot;
        }
        return -1; // more cargo than slots: no icon
    }

    private void RebuildIcons()
    {
        foreach (HudIcon hud in iconsById.Values)
            if (hud.icon != null)
                Destroy(hud.icon);
        iconsById.Clear();

        foreach (ItemInstance item in ship.State.cargo)
            if (item != null)
                SpawnHudIcon(item);
    }
}
