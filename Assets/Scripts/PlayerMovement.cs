using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 moveInput;

    public float thrustSpeed = 10f;
    public float turnSpeed = 5f;

    private float thrustInput;
    private float turnInput;

    [Header("Idle Detection")]
    [SerializeField] private float idleThreshold = 10f; // Seconds
    private float idleTimer = 0f;
    private bool hasTriggeredIdle = false;

    
    [System.Serializable]
    public struct ThrusterGroup
    {
        public ParticleSystem particles;
        public AudioSource audio;
    }
    [Header("Thruster Visuals")]
    public ThrusterGroup topLeft, topRight, bottomLeft, bottomRight;


    [Header("World Boundaries")]
    public float minX, maxX;
    public float minY, maxY;
    //37 maxY
    //72 maxX
    //72 minX
    //37 minY
    public float warningBuffer = 5f; // Warn 5 units before hitting the hard limit

    private bool isAtEdge = false; // The State Flag to prevent spam

    bool shipMovedForward = false;

    [System.Serializable]
    public struct RocketGroup
    {
        public ParticleSystem[] particles;
        public AudioSource audio;
        public GameObject rocketToggleObj;
    }
    [Header("Rocket Visuals")]
    public RocketGroup rocketFX;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        StartCoroutine(ControlsHelp());
    }

    IEnumerator ControlsHelp()
    {
        yield return new WaitForSeconds(3f);
        AI_Manager.Instance.TriggerMessage("Basic Movement Controls");

    }

    void LateUpdate() // Use LateUpdate to ensure this runs after movement code
    {
        Vector3 pos = transform.position;

        // 1. Logic: Check if player is in the 'Warning Zone'
        bool currentlyAtEdge = (pos.x > maxX - warningBuffer || pos.x < minX + warningBuffer ||
                                pos.y > maxY - warningBuffer || pos.y < minY + warningBuffer);

        // 2. Trigger: Only call the AI when the state CHANGES to 'True'
        if (currentlyAtEdge && !isAtEdge)
        {
            AI_Manager.Instance.TriggerMessage("LeavingMissionArea");
            isAtEdge = true; // "Lock" the message
        }
        else if (!currentlyAtEdge)
        {
            isAtEdge = false; // "Unlock" so it can trigger again later
        }

        // 3. Enforcement: Hard clamp the physical movement
        //pos.x = Mathf.Clamp(pos.x, minX, maxX);
        //pos.y = Mathf.Clamp(pos.y, minY, maxY);
        //transform.position = pos;
    }
    private void Update()
    {
        thrustInput = UnityEngine.Input.GetAxis("Vertical");
        turnInput = UnityEngine.Input.GetAxis("Horizontal");

        // 2. Check for "Zero" input (Idle)
        // We use a small epsilon (0.01) to account for joystick drift
        if (Mathf.Abs(thrustInput) < 0.01f && Mathf.Abs(turnInput) < 0.01f)
        {
            
            idleTimer += Time.deltaTime; // Increment timer while idle

            // 3. Trigger once after 10 seconds
            if (idleTimer >= idleThreshold && !hasTriggeredIdle)
            {
                AI_Manager.Instance.TriggerMessage("Idle Conversation");
                hasTriggeredIdle = true; // Prevent spamming every frame
            }
        }
        else
        {
            // 4. Reset everything if the player moves
            idleTimer = 0f;
            hasTriggeredIdle = false;
            
        }
    }
    void FixedUpdate()
    {
        // Apply forward/backward force based on rotation
        rb.AddRelativeForce(Vector2.up * thrustInput * thrustSpeed);

        // Apply torque (rotation)
        rb.AddTorque(-turnInput * turnSpeed);
    }
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        // Horizontal Logic
        ToggleThruster(topRight, moveInput.x < 0);
        ToggleThruster(bottomRight, moveInput.x < 0);
        ToggleThruster(topLeft, moveInput.x > 0);
        ToggleThruster(bottomLeft, moveInput.x > 0);

        // Vertical Logic (Forward/Backward)
        if (moveInput.y > 0) // Moving Forward
        {
            //ToggleThruster(bottomLeft, true);
           // ToggleThruster(bottomRight, true);
            
        }
        else if (moveInput.y < 0) // Moving Backward
        {
            ToggleThruster(topLeft, true);
            ToggleThruster(topRight, true);
        }
         
        
        if (moveInput.y > 0)
        {
            if (shipMovedForward == false) 
            {
                shipMovedForward = true;
                ToggleRocketFX(rocketFX, true);
            }
            
        }
        else
        {
            if (shipMovedForward == true)
            {
                shipMovedForward = false;
                ToggleRocketFX(rocketFX, false);
            }
        }
         
    }
    private void ToggleThruster(ThrusterGroup group, bool shouldPlay)
    {
        if (shouldPlay)
        {
            if (!group.particles.isEmitting) group.particles.Play(); //
            if (!group.audio.isPlaying) group.audio.Play(); //
        }
        else
        {
            group.particles.Stop(); // Stops emission; existing particles linger
            group.audio.Stop();
        }
    }
    private void ToggleRocketFX(RocketGroup group, bool shouldPlay)
    {
        if (shouldPlay)
        {
            for (int i = 0; i < group.particles.Length; i++) 
            {
                if (!group.particles[i].isEmitting) group.particles[i].Play();
            } //
            if (!group.audio.isPlaying) group.audio.Play(); //
            if (!group.rocketToggleObj.activeInHierarchy) group.rocketToggleObj.SetActive(true); //
        }
        else
        {
            for (int i = 0; i < group.particles.Length; i++)
            {
                group.particles[i].Stop(); // Stops emission; existing particles linger
            }
            
            group.audio.Stop();
            group.rocketToggleObj.SetActive(false);
        }
    }

}