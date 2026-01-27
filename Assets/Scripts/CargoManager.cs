using UnityEngine;

public class CargoManager : MonoBehaviour
{
    public static CargoManager instance;

    int currItemCount = 0;
    int currCapacity = 2;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public bool cargoCheck() 
    {
        if (currItemCount < currCapacity) 
        {
            addCargo();
            return true;

        }
        return false;
    }

    void addCargo() 
    {
        AI_Manager.Instance.TriggerMessage("GenericPickup");
        currItemCount++;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
