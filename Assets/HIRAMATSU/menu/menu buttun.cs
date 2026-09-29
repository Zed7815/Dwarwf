using Unity.VisualScripting;
using UnityEngine;

public class menubuttun : MonoBehaviour
{
    public menuAnimation menuAnimation;
    public bool menu_now;//いまmenuを出しているかtrue=出ている
    public bool now_animation;//いまアニメーション演出中か
    [Header("↓これに今のステージ入力")]
    public int What_stage;//今どのステージにいるか
    public int page;//今のページ数
    void OnMouseDown()//このbuttonが押されたときメニューを表示する
    {
        if(!menu_now)
        {
        menu_now = true;
        menuAnimation.menu_Drop();
        }
    }
    public void Page(bool a)
    {
        if (a)
        {
            page--;
        }
        else if(!a)
        {
            page++;
        }
    }
}
