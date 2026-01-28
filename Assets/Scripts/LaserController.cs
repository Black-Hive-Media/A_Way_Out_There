using UnityEngine;
using UnityEngine.InputSystem;
using static MiningLaserStats;

public interface IMineable
{
    // Returns true if the object was successfully destroyed/depleted
    bool OnMined(float damage, float yieldBonus, MaxAsteriodSizeCapable canHandleSize);
}
public class LaserController : MonoBehaviour
{
    [SerializeField] private MiningLaserStats currentStats;

    private bool isFiring = false;
    [Header("Visuals")]
    [SerializeField] private LineRenderer beamLine;
    [SerializeField] private float maxBeamDistance = 10f;
    void Update()
    {
        if (isFiring)
        {
            UpdateLaserVisuals();
        }
        else
        {
            beamLine.enabled = false;
        }
        
    }
    public void UpdateLaserTier(MiningLaserStats newStats)
    {
        this.currentStats = newStats; // Overwrites Tier 1 with Tier 2, etc.
                                      // Trigger AI snark about the new shiny laser
        AI_Manager.Instance.TriggerMessage("LaserUpgraded");
    }
    void UpdateLaserVisuals()
    {
        beamLine.enabled = true;

        // 1. Get our "Wobbly" direction from the method we wrote earlier
        Vector2 direction = ApplyWobble(transform.up, currentStats.beamWobble);

        // 2. Perform a Raycast to see where the beam should stop
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxBeamDistance, currentStats.validTargets);

        // 3. Set the start of the line (The ship's gun)
        beamLine.SetPosition(0, transform.position);

        // 4. Set the end of the line
        if (hit.collider != null)
        {
            // Stop at the object we hit
            beamLine.SetPosition(1, hit.point);
            
            // Tier 1 Logic: Actually mine the target
            if (hit.collider.gameObject != null) 
            {
                MineTarget(hit.collider.gameObject);
            }
        }
        else
        {
            // If we hit nothing, draw the beam to its maximum range
            Vector3 maxPos = (Vector3)((Vector2)transform.position + (direction * maxBeamDistance));
            beamLine.SetPosition(1, maxPos);
        }
    }
    private Vector2 ApplyWobble(Vector2 originalDirection, float wobbleIntensity)
    {
        // 1. Exit early if there is no wobble (Tier 1 Stabilized Upgrade)
        if (wobbleIntensity <= 0) return originalDirection;

        // 2. Use Time and PerlinNoise to get a smooth "wavering" value between -1 and 1
        // We use different 'seeds' (10.5 and 20.5) to ensure X and Y wobble differently
        float noiseX = (Mathf.PerlinNoise(Time.time * 5f, 10.5f) * 2f) - 1f;
        float noiseY = (Mathf.PerlinNoise(Time.time * 5f, 20.5f) * 2f) - 1f;

        // 3. Scale the noise by the Intensity from your ScriptableObject
        Vector2 offset = new Vector2(noiseX, noiseY) * wobbleIntensity;

        // 4. Return the new direction (original + the wobble)
        return (originalDirection + offset).normalized;
    }

    void MineTarget(GameObject target)
    {
        // 1. Try to get the IMineable component
        IMineable mineable = target.GetComponent<IMineable>();

        if (mineable != null)
        {
            // 2. Calculate damage based on frame time so mining is "per second"
            float damageThisFrame = currentStats.miningSpeed * Time.deltaTime;

            // 3. Pass stats to the object
            bool isDepleted = mineable.OnMined(
                damageThisFrame,
                currentStats.bonusYieldChance,
                currentStats.maxAsteriodSizeCapable
            );

            // 4. AI Feedback: Trigger a snarky comment if we finish mining a big rock
            if (isDepleted)
            {
                AI_Manager.Instance.TriggerMessage("AsteroidDestroyed");
            }
        }
    }
    public void Fire(InputAction.CallbackContext context)
    {
        // 1. Check if the button was just pressed
        if (context.started)
        {
            isFiring = true;
        }

        // 2. Check if the button was just released
        if (context.canceled)
        {
            isFiring = false;
        }
    }

}

