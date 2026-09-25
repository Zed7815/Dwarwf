using UnityEngine;

public class menubuttun : MonoBehaviour
{
    public menuAnimation menuAnimation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnMouseDown()
    {
        menuAnimation.menu();
    }
}
