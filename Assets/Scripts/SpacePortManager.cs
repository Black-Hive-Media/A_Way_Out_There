using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpacePortManager : MonoBehaviour
{
    public static SpacePortManager Instance { get; private set; }

    public SpacePortUI portUI;
    public PlayerInput playerInput;
    public Animator doorAnimator;

    public TextMeshProUGUI playerCreditsTxt;

    private GameObject playerObj;

    [SerializeField] private float landingSpeed = 2f;
    [SerializeField] private float zDepthTarget = 5f;
    private float origYPos;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // How far back it moves
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void UITakeControl()
    {
        // Switches to the "UI" map and automatically disables the previous one
        playerInput.SwitchCurrentActionMap("UI");
    }

    public void PlayerTakeControl()
    {
        // Switches back to your gameplay/ship controls
        StopAllCoroutines();

        if (playerObj.transform != null)
        {
            StartCoroutine(LaunchShip(playerObj.transform));
        }
    }

    private IEnumerator LaunchShip(Transform shipTransform)
    {

        doorAnimator.Play("doors_open", 0);
        
        yield return new WaitForSeconds(3.5f);
        playerObj.GetComponentInChildren<Renderer>().sortingOrder = 0;
        // Target is the original Z, but keeping current X/Y to move straight up
        Vector3 targetPosition = new Vector3(shipTransform.position.x, shipTransform.position.y, 0);

        while (Mathf.Abs(shipTransform.position.z - 0) > 0.01f)
        {
            shipTransform.position = Vector3.MoveTowards(
                shipTransform.position,
                targetPosition,
                landingSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Reset Z to exactly the original value
        shipTransform.position = new Vector3(shipTransform.position.x, shipTransform.position.y, 0);
        
        playerInput.SwitchCurrentActionMap("Player");
    }
    private IEnumerator LandShip(GameObject playerShip, Transform shipTransform)
    {
        // Disable player controls here if necessary
        Vector3 targetPosition = new Vector3(transform.position.x, -32, zDepthTarget);

        while (Vector3.Distance(shipTransform.position, targetPosition) > 0.01f)
        {
            // MoveTowards handles the smooth linear interpolation
            shipTransform.position = Vector3.MoveTowards(
                shipTransform.position,
                targetPosition,
                landingSpeed * Time.deltaTime
            );

            yield return null; // Wait for the next frame
        }

        // Final snap to target
        shipTransform.position = targetPosition;
        //close doors
        
        playerShip.GetComponentInChildren<Renderer>().sortingOrder = -2;
        doorAnimator.Play("doors_close", 0);
        yield return new WaitForSecondsRealtime(5);
        //then open menu
        portUI.OpenMenu();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //stop player controls
            UITakeControl();
            //open doors
            doorAnimator.Play("doors_open", 0);
            //change z of ship
            if (playerObj == null) { playerObj = collision.gameObject; }
            origYPos = playerObj.transform.position.y;
            StartCoroutine(LandShip(playerObj, collision.transform));
            
        }

    }

}
