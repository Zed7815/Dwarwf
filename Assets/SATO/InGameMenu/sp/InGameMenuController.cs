using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InGameMenuController : MonoBehaviour
{
    public static InGameMenuController instance;

    [Header("1. 暗幕")]
    public GameObject darkOverlayUI;

    [Header("2. アニメーション本（Sprite）")]
    public GameObject animatedBookWorld; // 外のSprite
    public Animator worldBookAnimator;   // そのAnimator
    public string openTriggerName = "Open";
    public string closeTriggerName = "Close";
    public float openAnimDuration = 0.4f;
    public float closeAnimDuration = 0.3f;

    [Header("落下・上昇の演出設定")]
    [Tooltip("上から落ちてくる距離")]
    public float dropDistanceY = 12f;
    public float dropDuration = 0.35f;
    public float flyUpDuration = 0.25f;

    [Header("3. 本物のUI本（Canvas）")]
    public RectTransform staticBookUI;   // UIの本（中央）
    public GameObject menuModePanel;      // メニューボタン一覧の画面
    public GameObject encyclopediaPanel;  // 図鑑の見開き画面

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip bookDropSE;    // ドサッ（着地音）
    public AudioClip bookOpenSE;    // パサッ（開く音）
    public AudioClip bookCloseSE;   // パタン（閉じる音）
    public AudioClip bookFlyUpSE;   // ヒュン（飛び去る音）
    public AudioClip clickSE;

    [Header("演出参照")]
    public nextscene fadeOutScript;

    private float previousTimeScale = 1f;
    private bool isMenuOpen = false;
    private bool isAnimating = false;
    private Vector3 bookCenterWorldPos;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // 全て初期非表示
        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);
    }

    // --- メニューを開く（上から落下 ➔ 開く ➔ UI表示） ---
    public void OpenMenu()
    {
        if (isMenuOpen || isAnimating) return;
        isMenuOpen = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f; // 一時停止

        StopAllCoroutines();
        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        isAnimating = true;

        // 1. 位置のミリ単位同期（UI本の画面位置にアニメ本を吸着させる）
        AlignWorldBookToUI();

        // 2. 暗幕ON
        if (darkOverlayUI != null) darkOverlayUI.SetActive(true);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);

        // 3. 上から閉じた本が落ちてくる演出
        if (animatedBookWorld != null)
        {
            animatedBookWorld.SetActive(true);

            // 画面上部から中央へ
            Vector3 startPos = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            Vector3 endPos = bookCenterWorldPos;
            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dropDuration;
                // ドサッと落ちて少し弾むイージング
                t = t - 1f;
                float bounce = t * t * ((1.70158f + 1f) * t + 1.70158f) + 1f;
                animatedBookWorld.transform.position = Vector3.LerpUnclamped(startPos, endPos, bounce);
                yield return null;
            }
            animatedBookWorld.transform.position = endPos;

            if (audioSource != null && bookDropSE != null) audioSource.PlayOneShot(bookDropSE);

            // 4. 着地後、本が開くアニメーション再生
            if (worldBookAnimator != null) worldBookAnimator.SetTrigger(openTriggerName);
            if (audioSource != null && bookOpenSE != null) audioSource.PlayOneShot(bookOpenSE);

            yield return new WaitForSecondsRealtime(openAnimDuration);
        }

        // 5. すり替え！UI本を表示
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(true);
        if (menuModePanel != null) menuModePanel.SetActive(true);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);

        isAnimating = false;
    }

    // --- 本を閉じて上へ飛び去る共通演出 ---
    IEnumerator CloseAndFlyUpRoutine()
    {
        isAnimating = true;

        // 1. UIを消して、アニメ本を表示
        AlignWorldBookToUI();
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);

        if (animatedBookWorld != null)
        {
            animatedBookWorld.SetActive(true);
            animatedBookWorld.transform.position = bookCenterWorldPos;

            // 2. 本が閉じるアニメーション
            if (worldBookAnimator != null) worldBookAnimator.SetTrigger(closeTriggerName);
            if (audioSource != null && bookCloseSE != null) audioSource.PlayOneShot(bookCloseSE);

            yield return new WaitForSecondsRealtime(closeAnimDuration);

            // 3. 上へシュッと飛んでいく
            if (audioSource != null && bookFlyUpSE != null) audioSource.PlayOneShot(bookFlyUpSE);

            Vector3 startPos = bookCenterWorldPos;
            Vector3 endPos = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            float elapsed = 0f;

            while (elapsed < flyUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / flyUpDuration;
                // 加速しながら上に消える
                animatedBookWorld.transform.position = Vector3.Lerp(startPos, endPos, t * t);
                yield return null;
            }

            animatedBookWorld.SetActive(false);
        }

        // 暗幕も消す
        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);

        isAnimating = false;
        isMenuOpen = false;
    }

    // --- ボタン1：閉じる ---
    public void OnCloseClicked()
    {
        if (isAnimating) return;
        StartCoroutine(CloseMenuAction());
    }

    IEnumerator CloseMenuAction()
    {
        yield return StartCoroutine(CloseAndFlyUpRoutine());
        Time.timeScale = previousTimeScale; // 再開
    }

    // --- ボタン2：最初からやり直す（全リセット） ---
    public void OnFullResetClicked()
    {
        if (isAnimating) return;
        PlayClickSE();
        StartCoroutine(FullResetAction());
    }

    IEnumerator FullResetAction()
    {
        yield return StartCoroutine(CloseAndFlyUpRoutine());

        // 旗の記憶を消去
        if (CheckpointManager.instance != null) CheckpointManager.instance.ClearCheckpoint();

        Time.timeScale = previousTimeScale;

        // 全リセット実行
        if (GameManager.instance != null) GameManager.instance.ResetGame();
    }

    // --- ボタン3：ステージセレクトへ ---
    public void OnReturnToStageSelectClicked()
    {
        if (isAnimating) return;
        PlayClickSE();
        StartCoroutine(StageSelectAction());
    }

    IEnumerator StageSelectAction()
    {
        yield return StartCoroutine(CloseAndFlyUpRoutine());

        // その後に黒い板が降りてくる
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("StageSelect");
    }

    // --- ズレ防止：UI本のワールド座標を取得してアニメ本を合わせる ---
    void AlignWorldBookToUI()
    {
        if (staticBookUI == null || Camera.main == null) return;
        Vector3 screenPos = staticBookUI.position;
        screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
        bookCenterWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
        bookCenterWorldPos.z = 0f;
    }

    void PlayClickSE()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
    }
}