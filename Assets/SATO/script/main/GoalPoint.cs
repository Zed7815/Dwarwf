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

            Player_walk pWalk = collision.GetComponent<Player_walk>();
            if (pWalk == null) pWalk = collision.GetComponentInParent<Player_walk>();

            if (pWalk != null)
            {
                // ★修正：空中にいる（着地していない）場合は、すぐに止めずに
                // 「地面に降りるまで少し待つ（または落下させる）」処理を挟む！
                StartCoroutine(StopPlayerWhenGrounded(pWalk));
            }

            StartCoroutine(GoalSequence());
        }
    }

    IEnumerator StopPlayerWhenGrounded(Player_walk pWalk)
    {
        Rigidbody2D rb = pWalk.GetComponent<Rigidbody2D>();

        // プレイヤーが地面（Grounded）に触れるまで、あるいは0.5秒経つまで待つ
        float timeout = 0.5f;
        while (timeout > 0)
        {
            // Player_walkの中にある地面判定がtrueになったら終了
            // ※もしPlayer_walkのisGroundedがprivateなら、Rigidbodyの縦速度がほぼ0になったらでもOKです
            if (rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
            {
                break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 1. すべてのコルーチンを完全停止
        pWalk.ForceStopAbilities();

        // 2. 物理演算をフリーズさせてピタッと止める
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

        // 2. セーブデータの処理
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        if (thisStageNumber > clearedStage)
        {
            PlayerPrefs.SetInt("StageCleared", thisStageNumber);
        }

        // 星の獲得セーブ（二重ガード）
        bool hasStar = false;
        if (GameManager.instance != null)
        {
            hasStar = GameManager.instance.hasCollectedStarInThisRun || GameManager.instance.totalItemCount > 0;
        }

        if (hasStar)
        {
            PlayerPrefs.SetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 1);
            Debug.Log($"<color=yellow>【ゴール】ステージ {GameManager.instance.stageNumber} の星を正常に保存しました！</color>");
        }

        PlayerPrefs.Save();

        yield return new WaitForSecondsRealtime(0.5f);

        // 3. クリア画面のポップアップ表示
        if (StageClearPopup.instance != null)
        {
            StageClearPopup.instance.ShowClearPopup();
            yield break; // ★ここで完全に停止し、ボタン操作を待つ！
        }

        // --- 以下、万が一ポップアップが無い場合のフォールバック ---
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