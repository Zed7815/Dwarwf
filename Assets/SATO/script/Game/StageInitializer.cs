using UnityEngine;
using System.Collections;

public class StageInitializer : MonoBehaviour
{
    [Header("演出設定")]
    public nextscene nextSceneScript; // フェードイン用
    public float startDelay = 0.5f;   // 演出後の待機時間

    IEnumerator Start()
    {
        // 黒い板をどかす演出を実行
        if (nextSceneScript != null)
        {
            yield return StartCoroutine(nextSceneScript.startKuro());
            // 演出が終わってから少し待機
            yield return new WaitForSecondsRealtime(startDelay);
        }

        Debug.Log("固定ステージの開始準備が完了しました");
    }
}
