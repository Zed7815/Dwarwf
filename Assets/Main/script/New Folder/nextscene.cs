using UnityEngine;
using System.Collections;

public class nextscene : MonoBehaviour
{
    [Header("移動量（1回あたりのピクセル量）")]
    public float stepX = 0.0f;     // ★追加：X軸方向の移動量（例: 横からスライドさせる場合）
    public float stepY = -600.0f;  // Y軸方向の移動量

    [Header("演出するパネル")]
    public GameObject transitionPanel;

    // シーン終了時（黒い板が閉まる／覆いかぶさる時）
    public IEnumerator endKuro()
    {
        for (int n = 0; n < 5; n++)
        {
            if (transitionPanel != null)
            {
                // X軸とY軸の両方の移動量を反映
                transitionPanel.transform.position += new Vector3(stepX, stepY, 0);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    // シーン開始時（黒い板が開く／画面から消える時）
    public IEnumerator startKuro()
    {
        for (int n = 0; n < 5; n++)
        {
            if (transitionPanel != null)
            {
                // 開く時は逆方向へ動かす（またはマイナスを反転させる）
                // ※もし逆へ戻したい場合は (-stepX, -stepY) にしてください
                transitionPanel.transform.position += new Vector3(-stepX, -stepY, 0);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
}