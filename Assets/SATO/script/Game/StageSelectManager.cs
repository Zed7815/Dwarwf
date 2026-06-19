using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StageSelectManager : MonoBehaviour
{
    public Button[] stageButtons; // インスペクターでボタンを順に入れる
    private static bool sessionResetDone = false; // テスト用

    void Start()
    { 
        if (!sessionResetDone)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            sessionResetDone = true;
        }

        // どこまでクリアしたかの読み込み
        // 初期値は0
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);

        // クリア状況でボタン押せるかの有無
        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            // 一つ手前のステージをクリアしすると押すことが可能に
            if (i <= clearedStage)
            {
                stageButtons[i].interactable = true;
            }
            else
            {
                stageButtons[i].interactable=false;
            }
        }
    }

    // ボタンによるシーン移動
    public void LoadStage(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
