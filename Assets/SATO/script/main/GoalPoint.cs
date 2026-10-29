using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GoalPoint : MonoBehaviour
{
    [Header("巣穴・難易度設定")]
    [Tooltip("このステージが属する巣の番号（1, 2, 3...）")]
    public int nestIndex = 1;
    [Tooltip("チェックを入れると『簡単』、外すと『難しい』ステージ")]
    public bool isEasyStage = true;

    [Header("ステージ番号設定（旧システム互換用）")]
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
        if (audioSource != null && goalSE != null)
        {
            audioSource.PlayOneShot(goalSE);
        }

        // 1. ステージクリア保存
        string clearKey = isEasyStage ? $"Nest_{nestIndex}_Easy_Cleared" : $"Nest_{nestIndex}_Hard_Cleared";
        PlayerPrefs.SetInt(clearKey, 1);

        bool easyDone = PlayerPrefs.GetInt($"Nest_{nestIndex}_Easy_Cleared", 0) == 1;
        bool hardDone = PlayerPrefs.GetInt($"Nest_{nestIndex}_Hard_Cleared", 0) == 1;

        if (easyDone && hardDone)
        {
            int currentNestCleared = PlayerPrefs.GetInt("NestClearedCount", 0);
            if (nestIndex > currentNestCleared)
            {
                PlayerPrefs.SetInt("NestClearedCount", nestIndex);
                PlayerPrefs.SetInt("StageCleared", nestIndex);
            }
        }

        // 2. ★星の獲得セーブ（二重保存で絶対に外さない）
        bool hasStar = false;
        if (GameManager.instance != null)
        {
            hasStar = GameManager.instance.hasCollectedStarInThisRun || GameManager.instance.totalItemCount > 0;
        }

        if (hasStar)
        {
            // 自動キー（巣の番号 ＋ 難易度）で保存！
            string autoStarKey = $"Nest_{nestIndex}_{(isEasyStage ? "Easy" : "Hard")}_Star";
            PlayerPrefs.SetInt(autoStarKey, 1);

            // 従来の番号キーでも念のため保存
            PlayerPrefs.SetInt("StarCollected_Stage_" + thisStageNumber, 1);
            if (GameManager.instance != null)
            {
                PlayerPrefs.SetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 1);
            }

            Debug.Log($"<color=yellow>【星獲得成功！】キー「{autoStarKey}」を確実に保存しました！</color>");
        }
        else
        {
            Debug.Log("<color=orange>【ゴール】このプレイでは星を獲得していません</color>");
        }

        PlayerPrefs.SetInt("LastSelectedNest", nestIndex);
        PlayerPrefs.Save();

        yield return new WaitForSecondsRealtime(0.5f);

        if (StageClearPopup.instance != null)
        {
            StageClearPopup.instance.ShowClearPopup();
            yield break;
        }

        if (nextSceneScript != null)
        {
            yield return StartCoroutine(nextSceneScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.5f);

        if (isFinalStage) SceneManager.LoadScene(endingSceneName);
        else SceneLoader.Load("StageSelect", returnGimmickType);
    }
}