using UnityEngine;
using UnityEngine.SceneManagement;

public class main_GoalPoint : MonoBehaviour
{
    public int thisStageNumber; // このステージの番号

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 現在のクリア記録の読み込み
            int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);

            // このステージが最新のクリアなら、記録を更新
            if (thisStageNumber > clearedStage)
            {
                PlayerPrefs.SetInt("StageCleared", thisStageNumber);
                PlayerPrefs.Save(); // 保存
            }

            // ステージセレクト画面へ戻る
            SceneManager.LoadScene("StageSelect");
        }
    }
}
