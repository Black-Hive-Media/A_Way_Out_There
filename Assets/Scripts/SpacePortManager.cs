using UnityEngine;

public class SpacePortManager : MonoBehaviour
{
    public SpacePortUI portUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {

            portUI.OpenMenu();
            //probably turn off ship controls
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
