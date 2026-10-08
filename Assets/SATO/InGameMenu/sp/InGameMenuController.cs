using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InGameMenuController : MonoBehaviour
{
    public static InGameMenuController instance;

    [Header("1. 暗幕（背景を暗くするUI）")]
    public GameObject darkOverlayUI;

    [Header("2. アニメーション専用の本（Canvas外のSprite）")]
    public GameObject animatedBookWorld; // SpriteRenderer + Animatorを持つオブジェクト
    public Animator worldBookAnimator;   // そのアニメーター
    public string openTriggerName = "Open";
    public string closeTriggerName = "Close";
    [Tooltip("開くアニメーションの長さ（秒）")]
    public float openAnimDuration = 0.4f;
    [Tooltip("閉じるアニメーションの長さ（秒）")]
    public float closeAnimDuration = 0.3f;

    [Header("3. 本物の本UI（Canvas内の静止画＋ボタン）")]
    public GameObject staticBookUI;
    public GameObject guideBookPanel; // ギミック図鑑（任意）

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip bookOpenSE;
    public AudioClip bookCloseSE;
    public AudioClip clickSE;

    [Header("演出参照")]
    public nextscene fadeOutScript;

    private float previousTimeScale = 1f;
    private bool isMenuOpen = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // 初期状態：すべて非表示にしておく
        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (staticBookUI != null) staticBookUI.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
    }

    // --- メニューを開く処理 ---
    public void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f; // ゲーム一時停止

        StopAllCoroutines();
        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        // 1. 暗幕を出す
        if (darkOverlayUI != null) darkOverlayUI.SetActive(true);
        if (staticBookUI != null) staticBookUI.SetActive(false);

        // 2. アニメーション専用の本（Sprite）を出現させて再生
        if (animatedBookWorld != null)
        {
            animatedBookWorld.SetActive(true);
            if (worldBookAnimator != null)
            {
                worldBookAnimator.SetTrigger(openTriggerName);
            }
        }

        if (audioSource != null && bookOpenSE != null) audioSource.PlayOneShot(bookOpenSE);

        // 3. アニメーションが終わるまで待つ（Realtimeで待機）
        yield return new WaitForSecondsRealtime(openAnimDuration);

        // 4. ★すり替え！ アニメ用の本を消し、本物のUI本を表示する
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (staticBookUI != null) staticBookUI.SetActive(true);
    }

    // --- メニューを閉じる処理 ---
    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;

        StopAllCoroutines();
        StartCoroutine(CloseSequence());
    }

    IEnumerator CloseSequence()
    {
        // 1. ★すり替え！ 本物のUI本を即座に消す
        if (staticBookUI != null) staticBookUI.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);

        // 2. アニメーション専用の本（Sprite）を出現させて閉じるアニメ再生
        if (animatedBookWorld != null)
        {
            animatedBookWorld.SetActive(true);
            if (worldBookAnimator != null)
            {
                worldBookAnimator.SetTrigger(closeTriggerName);
            }
        }

        if (audioSource != null && bookCloseSE != null) audioSource.PlayOneShot(bookCloseSE);

        // 3. 閉じきるまで待つ
        yield return new WaitForSecondsRealtime(closeAnimDuration);

        // 4. すべて消してゲーム再開
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);

        Time.timeScale = previousTimeScale; // 時間を戻す
    }

    // --- 各ボタンの処理 ---

    public void OnFullResetClicked()
    {
        PlayClickSE();
        if (CheckpointManager.instance != null) CheckpointManager.instance.ClearCheckpoint();

        // パネルを消して時間を戻す
        if (staticBookUI != null) staticBookUI.SetActive(false);
        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        isMenuOpen = false;
        Time.timeScale = previousTimeScale;

        if (GameManager.instance != null) GameManager.instance.ResetGame();
    }

    public void OnOpenGuideClicked()
    {
        PlayClickSE();
        if (staticBookUI != null) staticBookUI.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(true);
    }

    public void OnCloseGuideClicked()
    {
        PlayClickSE();
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
        if (staticBookUI != null) staticBookUI.SetActive(true);
    }

    public void OnReturnToStageSelectClicked()
    {
        PlayClickSE();
        if (staticBookUI != null) staticBookUI.SetActive(false);
        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);

        Time.timeScale = 1f;
        StartCoroutine(ReturnSequence());
    }

    IEnumerator ReturnSequence()
    {
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene("StageSelect");
    }

    void PlayClickSE()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
    }
}