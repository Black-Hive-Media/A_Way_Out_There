using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using static CollectibleItem;

public class ItemWorldDisplay : MonoBehaviour
{
    // The visual/physical components on your prefab
    [SerializeField] private SpriteRenderer spriteRenderer;

    // The actual data reference for this specific instance
    public CollectibleItem itemData { get; private set; }
    private Rigidbody2D rb; 
    private float seed;
    /// <summary>
    /// This is called by the Spawner immediately after getting the object from the pool.
    /// </summary>
    public void Setup(CollectibleItem data)
    {
        itemData = data;
        rb = GetComponent<Rigidbody2D>();
        seed = Random.value * 100f; // Unique seed for Perlin noise

        // Initial "nudge" into space
        rb.linearVelocity = Random.insideUnitCircle * itemData.driftSpeed;
        rb.angularVelocity = Random.Range(-itemData.rotationSpeed, itemData.rotationSpeed);

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
                ItemPoolManager.Instance.ReleaseItem(this.gameObject);
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
    void FixedUpdate()
    {
        if (itemData == null) return;

        switch (itemData.movementStyle)
        {
            case MovementStyle.HeavyDrift: // Raw Minerals
                                           // High linear drag in Inspector, minimal torque
                rb.AddForce(Vector2.down * 0.1f); // Subtle "gravity" pull
                break;

            case MovementStyle.UnstableTumble: // Ship Parts
                                               // Sharp rotation changes using Perlin Noise
                if (Mathf.PerlinNoise(Time.time, seed) > 0.7f)
                    rb.AddTorque(Random.Range(-5f, 5f), ForceMode2D.Impulse);
                break;

            case MovementStyle.OrganicBob: // Medical
                                           // Soft sine wave bobbing
                float bob = Mathf.Sin(Time.time + seed) * 0.5f;
                rb.AddForce(Vector2.up * bob);
                rb.angularVelocity = Mathf.Lerp(rb.angularVelocity, 0, Time.fixedDeltaTime);
                break;

            case MovementStyle.ErraticThruster: // Living Cargo
                                                // Sudden micro-bursts
                if (Random.value < 0.01f) // 1% chance per physics step
                {
                    Vector2 burst = Random.insideUnitCircle * 5f;
                    rb.AddForce(burst, ForceMode2D.Impulse);
                    AI_Manager.Instance.TriggerMessage("PassengerResisting");
                }
                break;

            case MovementStyle.Anomalous: // Rare Items
                                          // Orbiting a point or "Phase Shifting" (teleporting slightly)
                Vector2 orbit = new Vector2(Mathf.Cos(Time.time), Mathf.Sin(Time.time)) * 2f;
                rb.MovePosition(rb.position + orbit * Time.fixedDeltaTime);
                break;
        }
    }
}