using UnityEngine;
using System.Collections;

public class TutorialAutoStarter : MonoBehaviour
{
    [Header("自動スタート設定")]
    [Tooltip("シーンに入ってから自動で動き出すまでの待機時間（秒）")]
    public float autoStartDelay = 1.0f; // インスペクターで変更可能！

    [Header("参照設定")]
    public GameManager gameManager;

    void Start()
    {
        if (gameManager == null) gameManager = GameManager.instance;

        // シーンが読み込まれたら自動スタートのカウントダウンを開始
        StartCoroutine(AutoStartRoutine());
    }

    IEnumerator AutoStartRoutine()
    {
        // 1. インスペクターで設定した時間だけ待機（タメを作る）
        yield return new WaitForSecondsRealtime(autoStartDelay);

        // 2. GameManagerが存在し、現在が編集（Edit）モードなら自動でスタートボタンの処理を実行！
        if (gameManager != null && gameManager.currentState == GameManager.GameState.Edit)
        {
            Debug.Log($"<color=cyan>【チュートリアル自動起動】 {autoStartDelay}秒経過したため、自動でゲームを実行します</color>");

            // GameManagerにある「スタートボタンを押した時と同じ処理」を呼び出す
            gameManager.StartGame();
        }
    }
}