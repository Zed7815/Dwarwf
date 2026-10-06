using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InGameMenuController : MonoBehaviour
{
    public static InGameMenuController instance;

    [Header("UIパネル参照")]
    public GameObject menuModalPanel;   // メニューのポップアップ親オブジェクト
    public GameObject guideBookPanel;   // ギミック図鑑のサブパネル（任意）

    [Header("本のアニメーション設定")]
    public Animator bookAnimator;       // ★本のアニメーター
    public string bookOpenTrigger = "Open";   // 本を開くトリガー名
    public string bookCloseTrigger = "Close"; // 本を閉じるトリガー名
    public RectTransform bookWindow;    // アニメーターが無い場合の拡縮用（任意）

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip bookOpenSE;        // ★本を開く時の音（バサッ、パカッなど）
    public AudioClip closeSE;
    public AudioClip clickSE;

    private float previousTimeScale = 1f;
    private bool isMenuOpen = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
    }

    // --- メニューを開く（本を開く） ---

    public void OpenMenu()
    {
        if (isMenuOpen) return;
        isMenuOpen = true;

        // 本を開く効果音
        if (audioSource != null && bookOpenSE != null) audioSource.PlayOneShot(bookOpenSE);

        // ゲーム時間を停止
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (menuModalPanel != null) menuModalPanel.SetActive(true);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);

        // ★本を開くアニメーションを開始！
        StopAllCoroutines();
        StartCoroutine(OpenBookRoutine());
    }

    IEnumerator OpenBookRoutine()
    {
        // 1. Animatorがある場合はトリガーを引く
        if (bookAnimator != null)
        {
            bookAnimator.SetTrigger(bookOpenTrigger);
        }
        // 2. Animatorが無くても綺麗に開いて見えるC#バックアップ演出（横にパカッと開く）
        else if (bookWindow != null)
        {
            bookWindow.localScale = new Vector3(0f, 1f, 1f); // 閉じた状態（幅0）
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // 一時停止中なので unscaledDeltaTime を使用
                float t = elapsed / duration;

                // 本がパカッと開くイージング
                float scaleX = Mathf.Sin(t * Mathf.PI * 0.5f);
                bookWindow.localScale = new Vector3(scaleX, 1f, 1f);
                yield return null;
            }
            bookWindow.localScale = Vector3.one;
        }

        yield return null;
    }

    // --- メニューを閉じる ---

    public void CloseMenu()
    {
        if (!isMenuOpen) return;
        isMenuOpen = false;

        if (audioSource != null && closeSE != null) audioSource.PlayOneShot(closeSE);

        StartCoroutine(CloseBookRoutine());
    }

    IEnumerator CloseBookRoutine()
    {
        // 本を閉じるアニメーションがあれば再生
        if (bookAnimator != null && !string.IsNullOrEmpty(bookCloseTrigger))
        {
            bookAnimator.SetTrigger(bookCloseTrigger);
            yield return new WaitForSecondsRealtime(0.2f); // 閉じるのを少し待つ
        }

        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);

        // 時間を再開
        Time.timeScale = previousTimeScale;
    }

    // --- ボタン1：最初からやり直す（全リセット） ---

    public void OnFullResetClicked()
    {
        PlayClickSE();

        if (CheckpointManager.instance != null)
        {
            CheckpointManager.instance.ClearCheckpoint();
        }

        CloseMenu();

        if (GameManager.instance != null)
        {
            GameManager.instance.ResetGame();
        }
    }

    // --- ボタン2：ギミック図鑑を開く ---

    public void OnOpenGuideClicked()
    {
        PlayClickSE();
        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(true);
    }

    public void OnCloseGuideClicked()
    {
        PlayClickSE();
        if (guideBookPanel != null) guideBookPanel.SetActive(false);
        if (menuModalPanel != null) menuModalPanel.SetActive(true);
    }

    // --- ボタン3：ステージセレクトへ戻る ---

    public void OnReturnToStageSelectClicked()
    {
        PlayClickSE();

        if (menuModalPanel != null) menuModalPanel.SetActive(false);
        if (guideBookPanel != null) guideBookPanel.SetActive(false);

        Time.timeScale = 1f;
        StartCoroutine(ReturnSequence());
    }

    IEnumerator ReturnSequence()
    {
        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene("StageSelect");
    }

    void PlayClickSE()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
    }
}