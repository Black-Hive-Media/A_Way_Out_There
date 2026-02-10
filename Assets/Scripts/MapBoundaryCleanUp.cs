using UnityEngine;

public class MapBoundaryCleanUp : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D other)
    {
        // 1. Identify what left the area
        // It is more efficient to check tags or components than strings
        if (other.CompareTag("Asteroid"))
        {
            ItemPoolManager.Instance.ReleaseAsteroid(other.gameObject);
            return;
        }

        if (other.CompareTag("Collectible"))
        {
            ItemPoolManager.Instance.ReleaseItem(other.gameObject);
            return;
        }
    }
}
