using UnityEngine;

public class sceneAnimation : MonoBehaviour
{
    public SpriteRenderer sr;
    public Sprite image1;
    public Sprite image2;
    public Sprite image3;
    public Sprite image4;
    public Sprite image5;
    public Sprite image6;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void fadein(int n)
    {
        switch (n)
        {
            case 1:sr.sprite = image1;  break;
            case 2:sr.sprite = image2;  break;
            case 3:sr.sprite = image3;  break;
            case 4:sr.sprite = image4;  break;
            case 5:sr.sprite = image5;  break;
            case 6:sr.sprite = image6;  break;
        }
    }
}
