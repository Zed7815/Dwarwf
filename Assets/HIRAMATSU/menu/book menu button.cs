using UnityEngine;

public class bookmenubutton : MonoBehaviour
{
    public menubuttun menubuttun;
    public menuAnimation menuAnimation;
    void OnMouseDown()
    {
        if (menubuttun.menu_now)
        {
            menubuttun.menu_now = false;
            menuAnimation.menu_up();
            Debug.Log("button反応");
        }
    }
}
