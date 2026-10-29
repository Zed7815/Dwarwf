using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GoalPoint : MonoBehaviour
{
    [Header("巣穴・難易度設定")]
    [Tooltip("このステージが属する巣の番号（1, 2, 3...）")]
    public int nestIndex = 1;
    [Tooltip("チェックを入れると『簡単』、外すと『難しい』ステージとして記録")]
    public bool isEasyStage = true;

    [Header("ステージ番号設定（星や既存システム用）")]
    public int thisStageNumber = 1;

    [Header("演出設定")]
    public nextscene nextSceneScript;

    [Header("最終ステージ設定")]
    public bool isFinalStage = false;
    public string endingSceneName = "Ending";

    [Header("クリア後のロード設定")]
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
            if (WarpEntrance.isAnyWarping) return;

            isGoalReached = true;

            Player_walk pWalk = collision.GetComponent<Player_walk>();
            if (pWalk == null) pWalk = collision.GetComponentInParent<Player_walk>();

            if (pWalk != null)
            {
                StartCoroutine(StopPlayerWhenGrounded(pWalk));
            }

            StartCoroutine(GoalSequence());
        }
    }

    IEnumerator StopPlayerWhenGrounded(Player_walk pWalk)
    {
        Rigidbody2D rb = pWalk.GetComponent<Rigidbody2D>();
        float timeout = 0.5f;
        while (timeout > 0)
        {
            if (rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.05f) break;
            timeout -= Time.deltaTime;
            yield return null;
        }

        pWalk.ForceStopAbilities();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    IEnumerator GoalSequence()
    {
        // 1. SE再生
        if (audioSource != null && goalSE != null)
        {
            audioSource.PlayOneShot(goalSE);
        }

        // 2. ★巣のクリア判定セーブ
        string clearKey = isEasyStage ? $"Nest_{nestIndex}_Easy_Cleared" : $"Nest_{nestIndex}_Hard_Cleared";

        PlayerPrefs.SetInt("LastSelectedNest", nestIndex);
        PlayerPrefs.Save();

        PlayerPrefs.SetInt(clearKey, 1);
        Debug.Log($"<color=green>【クリア記録】{clearKey} を保存しました！</color>");

        // 簡単と難しいの両方がクリアされたかチェック
        bool easyDone = PlayerPrefs.GetInt($"Nest_{nestIndex}_Easy_Cleared", 0) == 1;
        bool hardDone = PlayerPrefs.GetInt($"Nest_{nestIndex}_Hard_Cleared", 0) == 1;

        if (easyDone && hardDone)
        {
            // 両方クリアなら巣全体のクリア数を更新
            int currentNestCleared = PlayerPrefs.GetInt("NestClearedCount", 0);
            if (nestIndex > currentNestCleared)
            {
                PlayerPrefs.SetInt("NestClearedCount", nestIndex);
                PlayerPrefs.SetInt("StageCleared", nestIndex); // 図鑑等の連動用
                Debug.Log($"<color=yellow>【巣完全制覇】巣 {nestIndex} を完全クリアしました！</color>");
            }
        }

        // 星の獲得セーブ
        bool hasStar = false;
        if (GameManager.instance != null)
        {
            hasStar = GameManager.instance.hasCollectedStarInThisRun || GameManager.instance.totalItemCount > 0;
        }

        if (hasStar)
        {
            PlayerPrefs.SetInt("StarCollected_Stage_" + thisStageNumber, 1);
            Debug.Log($"<color=yellow>【星獲得】ステージ {thisStageNumber} の星を保存しました！</color>");
        }

        PlayerPrefs.Save();

        yield return new WaitForSecondsRealtime(0.5f);

        // 3. クリア画面のポップアップ表示
        if (StageClearPopup.instance != null)
        {
            StageClearPopup.instance.ShowClearPopup();
            yield break;
        }

        // フォールバック
        if (nextSceneScript != null)
        {
            yield return StartCoroutine(nextSceneScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (isFinalStage) SceneManager.LoadScene(endingSceneName);
        else SceneLoader.Load("StageSelect", returnGimmickType);
    }
}