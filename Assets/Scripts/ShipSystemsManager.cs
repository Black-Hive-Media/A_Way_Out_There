using UnityEngine;
using UnityEngine.InputSystem; // Must include this namespace

public class ShipSystemsManager : MonoBehaviour
{
    [SerializeField] private GameObject laserObject;
    [SerializeField] private GameObject tractorBeamObject;

 
    public void OnSelectLaser(InputAction.CallbackContext context)
    {
        if (context.performed) 
        {
            SetSystemActive(true, false);
        }
        if (context.canceled)
        {
            SetSystemActive(false, false);
        }
    }

    // Called by your "SwitchToTractor" Input Action
    public void OnSelectTractor(InputAction.CallbackContext context)
    {
        if (context.performed) 
        {
            SetSystemActive(false, true);
        }
        if (context.canceled)
        {
           // SetSystemActive(false, false);
            
        }
    }

    private void SetSystemActive(bool useLaser, bool useTractorBeam)
    {
        laserObject.GetComponent<LaserController>().Fire(useLaser);            
        
        tractorBeamObject.GetComponent<TractorBeam>().DeployBeam(useTractorBeam);   
    }
}