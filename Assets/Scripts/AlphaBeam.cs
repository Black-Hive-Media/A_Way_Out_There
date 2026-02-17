using UnityEngine;

public class AlphaBeam : MonoBehaviour
{
    public SpriteRenderer sprite;
    public GameObject tractorBeamFX;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        
    void Start()
    {
        Color tmpColor = sprite.color;
        tmpColor.a = 0.0f;
        sprite.color = tmpColor;
        
    }

    // Update is called once per frame
    public void UpdateColor(float newAlpha, float alphaSpeed)
    {
        newAlpha = ( newAlpha / 100 );
        tractorBeamFX.SetActive(true);
        //Debug.Log(newAlpha);
        Color currentAlpha = sprite.color;
        currentAlpha.a = Mathf.Lerp(currentAlpha.a, newAlpha, alphaSpeed);
        sprite.color = currentAlpha;

        if (newAlpha == 0) 
        {
            tractorBeamFX.SetActive(false);
        }
    }
}
