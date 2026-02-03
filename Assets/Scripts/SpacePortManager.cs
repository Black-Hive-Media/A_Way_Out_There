using UnityEngine;
using UnityEngine.InputSystem;

public class SpacePortManager : MonoBehaviour
{
    public SpacePortUI portUI;
    public PlayerInput playerInput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void UITakeControl()
    {
       // playerInput.actions.FindActionMap("Player").Disable();
        // Switches to the "UI" map and automatically disables the previous one
        playerInput.SwitchCurrentActionMap("UI");
       // playerInput.DeactivateInput();
    }

    public void PlayerTakeControl()
    {
        // Switches back to your gameplay/ship controls
        playerInput.SwitchCurrentActionMap("Player");
        //playerInput.ActivateInput();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("take controls");
            //stop player controls
            UITakeControl();
            //open doors
            //change z of ship
            //close doors
            //then open menu
            portUI.OpenMenu();
            //probably turn off ship controls
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
