using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Required for UI elements if you use a Slider

public class TractorBeam : MonoBehaviour
{
    [Header("Input Controls (Set in Unity Input Manager)")]
    public string deployButton = "DeployTractorBeam"; // Map to LB/Shift
    public string pullButton = "PullBeam";   // Map to RB/R

    [Header("Beam Settings")]
    public float pullForce = 50f;
    public float heatPerSecond = 10f;
    public float coolRateNormal = 5f;
    public float coolRateOverheat = 1f;
    public float maxHeat = 100f;
    public float playerMoveHeatMultiplier = 0.1f; // Heat increases with player movement
    public float overheatCooldownTime = 3f; // Time before beam can be used after full overheat
    public float beamWidth = 1f; // For item size check
   
    [Header("Pull Settings")]
    public float pullSpeed = 2f; // Units per second
    public float collectionDistance = 0.5f; // When to "absorb" the item
    [Header("Drift Settings")]
    public float driftBackSpeed = 1f; // Usually slower than pullSpeed
    [Header("Idle Break Settings")]
    public float maxIdleTime = 5f; // Seconds before it breaks free
    private float idleTimer = 0f;
    [Header("Shake Settings")]
    public float shakeThreshold = 0.7f; // Shake starts at 70% of maxIdleTime
    public float shakeIntensity = 0.1f; // How violent the shake is

    [Header("UI Reference")]
    public Slider heatSlider; // Optional: Link to a UI Slider
    public GameObject visualFX;
    public AudioSource audioFX;

    private float currentHeat = 0f;
    private bool isOverheated = false;
    private bool isBeaming = false;
    private GameObject attachedItem = null;
    private Rigidbody2D playerRb;
    private PolygonCollider2D beamPoly;
    private float lastOverheatTime = -Mathf.Infinity;
    private bool isDeployed = false;
    private List<GameObject> itemsInRange = new List<GameObject>();
    private Vector2 localBeamTip;
    void Start()
    {
        playerRb = GetComponentInParent<Rigidbody2D>();
        beamPoly = GetComponent<PolygonCollider2D>();
        if (heatSlider != null)
        {
            heatSlider.maxValue = maxHeat;
            heatSlider.value = 0f;
        }
        ReleaseBeam();
    }
    public void DeployBeam(InputAction.CallbackContext context)
    {
        // 1. Check if the button was just pressed
        if (context.started)
        {
            if (visualFX.activeInHierarchy == false)
            {
                if (!isOverheated)
                {
                    DeployBeam();
                }
            }
            else 
            {
                ReleaseBeam();
            }
        }

        // 2. Check if the button was just released
        if (context.canceled)
        {
        }
    }
    public void PullBeam(InputAction.CallbackContext context)
    {
        // 1. Check if the button was just pressed
        if (context.started)
        {
            if (isDeployed == true)
            {
                isBeaming = true;
            }
        }
        
        // 2. Check if the button was just released
        if (context.canceled)
        {
            isBeaming = false;
        }
    }
    void Update()
    {
        ManageHeat(isBeaming);

        // Update UI
        if (heatSlider != null) heatSlider.value = currentHeat;

        // Check for overheat status cooldown
        if (isOverheated)
        {
            currentHeat = Mathf.Max(0, currentHeat - coolRateOverheat * Time.deltaTime);
            
            if (currentHeat == 0) isOverheated = false;
            return;
        }
    }

    void BreakFree()
    {
        idleTimer = 0f;
        ReleaseBeam(); // Use your existing release logic to unparent and reset physics
                       // Optional: Add a small "pop" force so it doesn't just sit there
    }
    void ApplyWarningShake(float intensity)
    {
        if (attachedItem.GetComponent<ItemWorldDisplay>().itemData.movementStyle == CollectibleItem.MovementStyle.ErraticThruster) 
        {
            AI_Manager.Instance.TriggerMessage("PassengerResisting");
        }
        // Apply a random jitter offset to the local position
        Vector3 shakeOffset = (Vector3)Random.insideUnitCircle * shakeIntensity * intensity;
        attachedItem.transform.localPosition = (Vector3)localBeamTip + shakeOffset;
    }
    /// <summary>
    /// Pull item towards the ship if holding down reel
    /// Otherwise it should float back out to tip at a slower pace than reeling in
    /// </summary>
    void FixedUpdate()
    {
        if (isDeployed == false)
        {
            return;
        }
        if (attachedItem == null) { return; }
        
        bool isAtTip = attachedItem.transform.localPosition == (Vector3)localBeamTip;

        if (!isBeaming && isAtTip)
        {
            //Debug.Log(idleTimer + "   not beam and at tip" + maxIdleTime * shakeThreshold);
            // Item is idle at the tip, start the countdown
            idleTimer += Time.deltaTime;
            // Start shaking if we are past the threshold
            if (idleTimer > maxIdleTime * shakeThreshold)
            {
                // Calculate intensity (gets stronger as idleTimer approaches max)
                float currentIntensity = Mathf.InverseLerp(maxIdleTime * shakeThreshold, maxIdleTime, idleTimer);
                ApplyWarningShake(currentIntensity);
            }

            if (idleTimer >= maxIdleTime)
            {
                BreakFree();
            }
           
        }

        if (isBeaming)
        {
            Rigidbody2D itemRb = attachedItem.GetComponent<Rigidbody2D>();

            idleTimer = 0;
            // Set external velocity to zero once 'attached'
            itemRb.linearVelocity = Vector2.zero; // This fulfills the design requirement
            itemRb.angularVelocity = 0f;

            float distance = Vector2.Distance(transform.position, attachedItem.transform.position);

            if (distance < 0.5f) // Success threshold
            {
                CollectItem(attachedItem);
            }
            else
            {
                
                attachedItem.transform.localPosition = Vector2.MoveTowards(
                    attachedItem.transform.localPosition,Vector2.zero, pullSpeed * Time.deltaTime);

                // 2. Check if it's close enough to be "beamed in"
                if (attachedItem.transform.localPosition.magnitude < collectionDistance)
                {
                    CollectItem(attachedItem);
                }
            }
            // Add extra heat based on player movement (friction)
            currentHeat += playerRb.linearVelocity.magnitude * playerMoveHeatMultiplier * Time.deltaTime;
            if (currentHeat >= maxHeat)
            {
                OverheatBeam();
            }
        }
        else
        {
            
            // DRIFTING BACK TO TIP
            attachedItem.transform.localPosition = Vector2.MoveTowards(
                attachedItem.transform.localPosition,
                localBeamTip,
                driftBackSpeed * Time.deltaTime
            );
        }

    }
    void CollectItem(GameObject item)
    {
        // 1. Get the item data (assuming you have an Item component)
        ItemWorldDisplay data = item.GetComponent<ItemWorldDisplay>();

        // 2. Add to your ship's inventory system
        if (CargoManager.Instance.cargoCheck(data.itemData) == true)
        {
            data.OnItemPickup(data.itemData);

            //Return this object to the pool so it can be reused
            ItemPoolManager.Instance.ReleaseItem(item);
        }
        // 3. Clean up
        ReleaseBeam();

        // 4. Feedback (Optional)
        // PlaySound(collectionSFX);
        // Instantiate(sparkleEffect, transform.position, Quaternion.identity);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDeployed == false)
        {
            return;
        }

        if (itemsInRange.Count >= 1 ) { return; }

        float maxBeamWidth = beamPoly.bounds.size.x;

        if (other.CompareTag("Collectible"))
        {
            Collider2D itemCol = other.GetComponent<Collider2D>();
            float itemSize = Mathf.Max(itemCol.bounds.size.x, itemCol.bounds.size.y);

            //Debug.Log(itemSize + " <= " + maxBeamWidth);
            if (itemSize <= maxBeamWidth)
            {
                attachedItem = other.gameObject;
                Rigidbody2D itemRb = other.GetComponent<Rigidbody2D>();
                itemRb.linearVelocity = Vector2.zero;
                itemRb.angularVelocity = 0f;

                StartSnap(attachedItem);
                //lock ship movement
                playerRb.linearVelocity = Vector2.zero;
                    playerRb.angularVelocity = 0f;
                
            }
            itemsInRange.Add(other.gameObject);

            // If this is the item we just locked onto, freeze it immediately
           /* if (isBeaming && other.gameObject == attachedItem)
            {
                Rigidbody2D itemRb = other.GetComponent<Rigidbody2D>();
                itemRb.linearVelocity = Vector2.zero;
                itemRb.angularVelocity = 0f;
            }*/
        }
    }
    private Coroutine snappingCoroutine;

    void SetRestPosition()
    {
        localBeamTip = attachedItem.transform.localPosition;
    }
    void StartSnap(GameObject item)
    {
        // Stop any existing snap if we switch targets rapidly
        if (snappingCoroutine != null) StopCoroutine(snappingCoroutine);
        snappingCoroutine = StartCoroutine(SnapToBeamTip(item));
    }
    IEnumerator SnapToBeamTip(GameObject item)
    {        
        float elapsed = 0;
        float duration = 0.25f; // How long the "ease in" takes
        Vector3 startPos = item.transform.position;
        Rigidbody2D itemRb = item.GetComponent<Rigidbody2D>();

        // Temporarily disable physics so it doesn't fight the movement
        itemRb.bodyType = RigidbodyType2D.Kinematic;
        itemRb.linearVelocity = Vector2.zero;

        while (elapsed < duration)
        {
            if (item == null) yield break; // Safety if item is destroyed

            // Calculate the current "Tip" position (adjust 2f to your beam length)
            Vector3 currentTip = GetBeamTipWorldPosition();

            // Smoothly interpolate position
            item.transform.position = Vector3.Lerp(startPos, currentTip, elapsed / duration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Lock it perfectly at the end
        item.transform.position = GetBeamTipWorldPosition();
        item.transform.SetParent(this.transform, true);
        
        SetRestPosition();
    }
    Vector3 GetBeamTipWorldPosition()
    {
        PolygonCollider2D poly = beamPoly; // Reference to your beam collider
        Vector2 localTip = poly.points[0];
        float maxDistance = 0f;

        // Find the point furthest from the ship in the local Y axis
        foreach (Vector2 point in poly.points)
        {
            if (point.y > maxDistance)
            { // Change to .x if your ship faces right
                maxDistance = point.y;
                localTip = point;
            }
        }

        // Convert the local vertex point to a world position
        return poly.transform.TransformPoint(localTip);
    }
    void DeployBeam()
    {
        isDeployed = true;
        visualFX.SetActive(true);
        audioFX.Play();
    }

    void ReleaseBeam()
    {
        isDeployed = false;
        isBeaming = false;

       
        visualFX.SetActive(false);
        audioFX.Stop();

        //release anything in the beam
        ReleaseItemsInBeam();
    }

    void ManageHeat(bool pulling)
    {
       
        if (pulling)
        {
            if (isDeployed) 
            { 
                currentHeat += heatPerSecond * Time.deltaTime;
            }
            
        }
        else
        {
            // Faster cool down if managed (not overheated)
            currentHeat -= coolRateNormal * Time.deltaTime;
        }
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        if (currentHeat >= maxHeat)
        {
            OverheatBeam();
        }
    }

    void OverheatBeam()
    {
        isOverheated = true;
        lastOverheatTime = Time.time;
        // Implosion effect: apply a strong, outward force and torque
        ReleaseItemsInBeam();
        ReleaseBeam(); // Detach the item immediately
    }

    private void ReleaseItemsInBeam()
    {
        if (attachedItem != null)
        {
            attachedItem.transform.SetParent(null, true);

            attachedItem = null;

        }
        for (int i = 0;  i < itemsInRange.Count; i++)
        {
            itemsInRange[i].transform.SetParent(null, false);

            Rigidbody2D itemRb = itemsInRange[i].GetComponent<Rigidbody2D>();
            Vector2 directionFromPlayer = (itemsInRange[i].transform.position - transform.position).normalized;
            itemRb.bodyType = RigidbodyType2D.Dynamic;
            itemRb.AddForce(directionFromPlayer * 100f); // Send item spiraling into space
            itemRb.AddTorque(100f);

            itemsInRange.RemoveAt(i);
        }
        

    }
}
