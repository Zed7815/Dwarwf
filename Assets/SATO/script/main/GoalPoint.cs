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

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターで割り当てるか自動取得
    public AudioClip goalSE;       // ゴールした時の音

    private bool isGoalReached = false;

    private void Start()
    {
        // AudioSourceが未設定なら自分から取得を試みる
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

        // 3. 暗転演出（黒い板が降りてくる）
        if (nextSceneScript != null)
        {
            yield return StartCoroutine(nextSceneScript.endKuro());
        }

        // 演出の余韻
        yield return new WaitForSecondsRealtime(0.5f);

        // 4. シーン切り替え判定
        if (isFinalStage)
        {
            // ★最終ステージなら直接エンディングシーンへ
            Debug.Log("最終クリア！エンディングへ移行します");
            SceneManager.LoadScene(endingSceneName);
        }
        else
        {
            // それ以外は通常通りステージセレクトへ
            SceneLoader.Load("StageSelect", GimmickType.Generic);
        }
    }
}