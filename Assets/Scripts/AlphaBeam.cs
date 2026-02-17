using UnityEngine;

public class AlphaBeam : MonoBehaviour
{
    public SpriteRenderer sprite;
   
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        
    void Start()
    {
        Color tmpColor = sprite.color;
        tmpColor.a = 0.0f;
        sprite.color = tmpColor;
        
    }

    // Update is called once per frame
    public void UpdateColor(float curAlpha, float alphaSpeed)
    {
        Color currentAlpha = sprite.color;
        currentAlpha.a = Mathf.Lerp(currentAlpha.a, curAlpha, alphaSpeed);
        sprite.color = currentAlpha;
    }
}
