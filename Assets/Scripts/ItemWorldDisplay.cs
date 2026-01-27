using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class ItemWorldDisplay : MonoBehaviour
{
    // The visual/physical components on your prefab
    [SerializeField] private SpriteRenderer spriteRenderer;

    // The actual data reference for this specific instance
    public CollectibleItem itemData { get; private set; }

    /// <summary>
    /// This is called by the Spawner immediately after getting the object from the pool.
    /// </summary>
    public void Setup(CollectibleItem data)
    {
        itemData = data;

        // Update the visuals based on the ScriptableObject
        if (spriteRenderer != null && data.icon != null)
        {
            spriteRenderer.sprite = data.icon;
        }

        // Optional: Update the name of the object for easier debugging
        gameObject.name = $"Item_{data.itemName}";
    }

    // Trigger collection logic
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (CargoManager.Instance.cargoCheck(itemData) == true)
            {

                OnItemPickup(itemData);

                //Return this object to the pool so it can be reused
                ItemPoolManager.Instance.ReturnToPool(this.gameObject);
            }
            else
            {
                AI_Manager.Instance.TriggerMessage("CargoBayFull");
            }


        }
        
    }

    public void OnItemPickup(CollectibleItem newItem)
    {
        // The AI reacts based on the type and value
        if (newItem is LivingCargo cargo && cargo.isHostile)
        {
            AI_Manager.Instance.TriggerMessage("PickedUpOutlaw");
        }
        else if (newItem.targetPort == CollectibleItem.DestinationPort.RefineryPort)
        {
            AI_Manager.Instance.TriggerMessage("PickedUpDirt");
        }
    }
}
