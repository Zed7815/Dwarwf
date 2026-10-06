using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StageClearPopup : MonoBehaviour
{
    public static StageClearPopup instance;

    [Header("ポップアップ本体")]
    public GameObject clearPanel; // クリア画面の親オブジェクト

    [Header("次ステージ設定")]
    [Tooltip("次に進むステージのシーン名（例：Stage2）")]
    public string nextStageSceneName;
    [Tooltip("次のステージで紹介するギミック")]
    public GimmickType nextStageGimmick = GimmickType.Generic;
    [Tooltip("最終ステージの場合はチェック（エンディングへ直行）")]
    public bool isFinalStage = false;
    public string endingSceneName = "Ending";

    [Header("星の獲得演出（任意）")]
    public GameObject starAchievedIcon; // 今回星を取った場合に光らせるアイコン

    [Header("演出参照")]
    public nextscene fadeOutScript; // 黒い板

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip buttonClickSE;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    // GoalPoint.cs から呼ばれる：クリア画面を開く
    public void ShowClearPopup()
    {
        if (clearPanel != null) clearPanel.SetActive(true);

        // 今回のプレイで星を取ったかどうかの演出
        if (starAchievedIcon != null && GameManager.instance != null)
        {
            starAchievedIcon.SetActive(GameManager.instance.hasCollectedStarInThisRun);
        }
    }

    // --- ボタン1：次のステージへ進む ---
    public void OnNextStageClicked()
    {
        PlaySE();
        StartCoroutine(NextStageRoutine());
    }

    IEnumerator NextStageRoutine()
    {
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);

        if (isFinalStage)
        {
            // 最終面ならエンディングへ
            SceneManager.LoadScene(endingSceneName);
        }
        else
        {
            // ロード画面を経由して次のステージへ！
            SceneLoader.Load(nextStageSceneName, nextStageGimmick);
        }
    }

    // --- ボタン2：ステージセレクトへ戻る ---
    public void OnReturnToSelectClicked()
    {
        PlaySE();
        StartCoroutine(ReturnToSelectRoutine());
    }

    IEnumerator ReturnToSelectRoutine()
    {
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);

        // セレクト画面へ戻る
        SceneLoader.Load("StageSelect", GimmickType.Generic);
    }

    void PlaySE()
    {
        if (audioSource != null && buttonClickSE != null) audioSource.PlayOneShot(buttonClickSE);
    }
}