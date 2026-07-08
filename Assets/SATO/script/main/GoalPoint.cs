using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalPoint : MonoBehaviour
{
    public int thisStageNumber;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 現在のクリア記録
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
            SceneManager.LoadScene("StageSelect");
        }
    }
}