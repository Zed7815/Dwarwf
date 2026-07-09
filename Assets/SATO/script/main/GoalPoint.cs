using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GoalPoint : MonoBehaviour
{
    public int thisStageNumber;

    [Header("演出設定")]
    public nextscene nextSceneScript; // ★インスペクターで割り当てられるように追加

    private bool isGoalReached = false; // 二重発動防止用

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // プレイヤーが触れた、かつ、まだゴール処理が始まっていない場合
        if (collision.CompareTag("Player") && !isGoalReached)
        {
            isGoalReached = true;
            StartCoroutine(GoalSequence());
        }
    }

    IEnumerator GoalSequence()
    {
        // 1. セーブデータの処理
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        if (thisStageNumber > clearedStage)
        {
            PlayerPrefs.SetInt("StageCleared", thisStageNumber);
        }

        // ★星の取得セーブ (ステージ番号をキーにして保存)
        if (GameManager.instance != null && GameManager.instance.hasCollectedStarInThisRun)
        {
            PlayerPrefs.SetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 1);
        }

        PlayerPrefs.Save();

        // 2. 黒い板が降りてくる演出を実行
        if (nextSceneScript != null)
        {
            // endKuro演出の終了を待機
            yield return StartCoroutine(nextSceneScript.endKuro());
        }

        // 3. 指示通り 0.5秒 待機
        yield return new WaitForSecondsRealtime(0.5f);

        // 4. ステージセレクト画面へ移動
        SceneManager.LoadScene("StageSelect");
    }
}