using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("移動先の簡単ステージ設定")]
    [Tooltip("次に読み込む簡単ステージのシーン名")]
    public string nextStageSceneName;
    [Tooltip("ローディングや図鑑用に渡すギミックの種類")]
    public GimmickType stageGimmick = GimmickType.Generic;

    [Header("演出参照")]
    public nextscene fadeOutScript; // オプション（もし暗転させたい場合）

    [Header("待機時間")]
    public float waitTime = 0.4f;

    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        // プレイヤーが右端に到達したら
        if (collision.CompareTag("Player") || collision.GetComponentInParent<Player_walk>() != null)
        {
            isTriggered = true;
            StartCoroutine(TransitionRoutine(collision.gameObject));
        }
    }

    IEnumerator TransitionRoutine(GameObject player)
    {
        // 1. プレイヤーの操作を固定し、右へ歩き続ける慣性を与える
        Player_walk pWalk = player.GetComponent<Player_walk>();
        if (pWalk != null)
        {
            pWalk.StateChange(1); // 歩行状態
            pWalk.direction = 1;  // 右向き固定
        }

        // 2. カメラがプレイヤーを追い越して右へスライドする演出を待つ（または即座に遷移）
        // ※もしカメラをスーッと右に逃がしたい場合はここで少し待機します
        yield return new WaitForSeconds(waitTime);

        // 3. 次のシーンへダイレクトにジャンプ！
        // （ローディング画面を挟まず、スパッと次のシーンの左端へ繋がります）
        SceneLoader.Load(nextStageSceneName, stageGimmick);
    }
}