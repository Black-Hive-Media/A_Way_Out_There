using UnityEngine;
using static CollectibleItem;

public class ItemWorldDisplay : MonoBehaviour
{

    // The actual data reference for this specific instance
    public CollectibleItem itemData { get; private set; }
    private Rigidbody2D rb; 
    private float seed;
    private GameObject newVisuals;

   
    /// <summary>
    /// This is called by the Spawner immediately after getting the object from the pool.
    /// </summary>
    ///
    public void Setup(CollectibleItem data, GameObject parent)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        //Debug.Log(parent.name + "  " + data.name);
        itemData = data;
        rb = GetComponent<Rigidbody2D>();
        seed = Random.value * 100f; // Unique seed for Perlin noise

        // Initial "nudge" into space
        rb.linearVelocity = Random.insideUnitCircle * itemData.driftSpeed;
        rb.angularVelocity = Random.Range(-itemData.rotationSpeed, itemData.rotationSpeed);
       
        if (data.visualPrefab != null)
        {
            GameObject visual = Instantiate(data.visualPrefab, transform);

            // 3. Reset local position/rotation to align with the parent
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
        // Update the visuals based on the ScriptableObject
        /*if (spriteRenderer != null && data.visualPrefab != null)
        {
            spriteRenderer.sprite = data.visualPrefab;
        }*/


        // Optional: Update the name of the object for easier debugging
        gameObject.name = $"Item_{data.itemName}";
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
        //if there is a parent, then we don't need movement
        if (gameObject.transform.parent != null) { return; }

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