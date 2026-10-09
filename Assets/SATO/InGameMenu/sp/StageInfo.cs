using UnityEngine;

public class StageInfo : MonoBehaviour
{
    public static StageInfo instance;

    [Header("ステージ情報")]
    public int stageNumber = 1;
    public string stageTitle = "はじまりの森";

    [Header("冒険者のメモ（左ページ用）")]
    public Sprite playerPortrait; // このステージ用の自機の表情イラスト
    [TextArea(2, 5)]
    public string hintMemo = "まずはジャンプ台を置いてみよう！"; // 手書き風ヒント

    void Awake()
    {
        instance = this;
    }
}