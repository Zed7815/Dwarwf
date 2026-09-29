using Unity.VisualScripting;
using UnityEngine;

public class stickynote : MonoBehaviour
{
    //やることとしてアニメーション導入が終わったらアニメーションnowかのboolを導入する
    public menubuttun menubuttun;
    public int sticky;//どのページにとぶ付箋かどうか
    
    void Start()
    {
        menubuttun.page = menubuttun.What_stage;
    }
    private void OnMouseDown()
    { 
        menubuttun.page = sticky;
    }
}
