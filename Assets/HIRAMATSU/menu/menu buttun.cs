using UnityEngine;

public class menubuttun : MonoBehaviour
{
    public menuAnimation menuAnimation;
    public bool menu_now;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnMouseDown()
    {
        if(!menu_now)
        {
        menu_now = true;
        menuAnimation.menu_Drop();
        }
    }
}
