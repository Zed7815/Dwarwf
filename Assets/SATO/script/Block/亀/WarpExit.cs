using UnityEngine;

public class WarpExit : MonoBehaviour
{
    [Header("テレポート紐付け設定")]
    [Tooltip("この出口の番号。同じ番号のWarpEntranceから飛んできます")]
    public int warpID = 0;

    [HideInInspector] public Animator exitAnimator;

    private void Awake()
    {
        exitAnimator = GetComponent<Animator>();
    }
}