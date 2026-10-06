using UnityEngine;

public class nextpage : MonoBehaviour
{
    public menubuttun menubuttun;
    public bool RorL;//L=true
    private void OnMouseDown()
    {
        menubuttun.Page(RorL);
    }
}
