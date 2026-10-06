using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GoalPoint : MonoBehaviour
{
    public int thisStageNumber;

    [Header("演出設定")]
    public nextscene nextSceneScript;

    [Header("最終ステージ設定")]
    public bool isFinalStage = false;
    public string endingSceneName = "Ending";

    [Header("クリア後のロード設定")]
    [Tooltip("クリアしてセレクト画面に戻る時に表示する説明の種類")]
    public GimmickType returnGimmickType = GimmickType.Generic;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip goalSE;

    private bool isGoalReached = false;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isGoalReached)
        {
            isGoalReached = true;
            StartCoroutine(GoalSequence());
        }
    }

    IEnumerator GoalSequence()
    {
        // 1. SE再生
        if (audioSource != null && goalSE != null)
        {
            audioSource.PlayOneShot(goalSE);
        }

        // 2. セーブデータの処理
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        if (thisStageNumber > clearedStage)
        {
            PlayerPrefs.SetInt("StageCleared", thisStageNumber);
        }

        if (GameManager.instance != null && GameManager.instance.hasCollectedStarInThisRun)
        {
            PlayerPrefs.SetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 1);
        }

        // データを確実に書き込む
        PlayerPrefs.Save();

        // プレイヤーの動きを停止
        Player_walk pWalk = FindObjectOfType<Player_walk>();
        if (pWalk != null) pWalk.StateChange(0);

        yield return new WaitForSecondsRealtime(0.5f);

        // クリア画面がある場合は、それを開いてコルーチンを即座に「終了」する！
        if (StageClearPopup.instance != null)
        {
            StageClearPopup.instance.ShowClearPopup();
            yield break; // ★ここで完全に処理を止める！（下の自動移動を実行させない）
        }

        // --- 以下は万が一ポップアップが無い場合だけの予備動作 ---
        if (nextSceneScript != null)
        {
            yield return StartCoroutine(nextSceneScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (isFinalStage)
        {
            SceneManager.LoadScene(endingSceneName);
        }
        else
        {
            SceneLoader.Load("StageSelect", returnGimmickType);
        }
    }
}