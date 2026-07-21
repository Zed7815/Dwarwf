using UnityEngine;

public class StageOneGuide : MonoBehaviour
{
    void Start()
    {
        // すでにステージ1をクリアしている（＝StageClearedが1以上）なら
        // ガイドは不要なので最初から消す
        if (PlayerPrefs.GetInt("StageCleared", 0) > 0)
        {
            gameObject.SetActive(false);
        }
    }

    // 外部（Manager）から呼ばれて消える処理
    public void HideGuide()
    {
        gameObject.SetActive(false);
    }
}