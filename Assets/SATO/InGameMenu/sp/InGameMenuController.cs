using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
public class InGameMenuController : MonoBehaviour
{
    public static InGameMenuController instance;

    [Header("1. 暗幕")]
    public GameObject darkOverlayUI;

    [Header("2. アニメーション本（Sprite）")]
    public GameObject animatedBookWorld;
    public Animator worldBookAnimator;
    public string openTriggerName = "Open";
    public string closeTriggerName = "Close";
    public float openAnimDuration = 0.4f;
    public float closeAnimDuration = 0.3f;
    public float dropDistanceY = 12f;
    public float dropDuration = 0.35f;
    public float flyUpDuration = 0.25f;

    [Header("3. 本物のUI本（Canvas）")]
    public RectTransform staticBookUI;
    public GameObject menuModePanel;      // メニュー一覧（リセット、セレクト等のページ）
    public GameObject encyclopediaPanel;  // 図鑑ページ（動物たちのページ）

    [Header("位置の微調整")]
    public Vector3 animationOffset = Vector3.zero;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip bookDropSE;
    public AudioClip bookOpenSE;
    public AudioClip bookCloseSE;
    public AudioClip bookFlyUpSE;
    public AudioClip pageFlipSE;    // ★追加：メニュー ⇔ 図鑑をめくる音
    public AudioClip clickSE;

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("左ページ（ステージ情報・メモ）のUI参照")]
    public TextMeshProUGUI stageTitleText;       // 例: STAGE 2 - クモの谷
    public TextMeshProUGUI thisStageStarText;     // 例: ★ 獲得済み / ☆ 未獲得
    public GameObject thisStageStarCheckIcon;    // 星を取った時に出るチェックマーク（任意）
    public TextMeshProUGUI totalStarText;         // 例: ★ 5 / 20
    public Image playerPortraitImage;            // 自機の小さな立ち絵
    public TextMeshProUGUI hintMemoText;         // 手書き風ヒント文章

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

        // 1. 初回起動時から、位置をあらかじめ計算しておく
        AlignWorldBookToUI();

        // 2. ★初回バグ防止：画面に出す前に、最初から空の上（画面外）へ追放しておく！
        if (animatedBookWorld != null)
        {
            animatedBookWorld.transform.position = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            animatedBookWorld.SetActive(false);
        }

        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);
    }

    // --- メニューを開く ---
    public void OpenMenu()
    {
        if (isMenuOpen || isAnimating) return;
        isMenuOpen = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // メニューを開いた瞬間に、左ページの内容を最新に更新！
        UpdateLeftPageInfo();

        StopAllCoroutines();
        StartCoroutine(OpenSequence());
    }

    void UpdateLeftPageInfo()
    {
        // 1. ステージ情報の取得
        int currentStageNum = 1;
        string currentTitle = "STAGE";

        if (StageInfo.instance != null)
        {
            currentStageNum = StageInfo.instance.stageNumber;
            currentTitle = $"STAGE {currentStageNum} - {StageInfo.instance.stageTitle}";

            // 自機のイラストとヒント文
            if (playerPortraitImage != null && StageInfo.instance.playerPortrait != null)
            {
                playerPortraitImage.sprite = StageInfo.instance.playerPortrait;
            }
            if (hintMemoText != null)
            {
                hintMemoText.text = StageInfo.instance.hintMemo;
            }
        }
        else if (GameManager.instance != null)
        {
            currentStageNum = GameManager.instance.stageNumber;
            currentTitle = $"STAGE {currentStageNum}";
        }

        if (stageTitleText != null) stageTitleText.text = currentTitle;

        // 2. このステージの星の獲得状況（過去にクリア済みか、今回のプレイで取ったか）
        bool hasStarAlready = PlayerPrefs.GetInt("StarCollected_Stage_" + currentStageNum, 0) == 1;
        bool gotStarNow = (GameManager.instance != null && GameManager.instance.hasCollectedStarInThisRun);
        bool hasStar = hasStarAlready || gotStarNow;

        if (thisStageStarText != null)
        {
            thisStageStarText.text = hasStar ? "獲得済み！" : "未獲得！";
        }
        if (thisStageStarCheckIcon != null)
        {
            thisStageStarCheckIcon.SetActive(hasStar);
        }

        // 3. 全ステージの総スター数
        int totalStars = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) totalStars++;
        }
        if (totalStarText != null)
        {
            totalStarText.text = $"あつめた星 : {totalStars} / 6";
        }
    }

IEnumerator OpenSequence()
    {
        isAnimating = true;

        // 1. 座標の再計算
        AlignWorldBookToUI();

        // 2. UI本は確実に消しておく
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);
        if (darkOverlayUI != null) darkOverlayUI.SetActive(true);

        // 3. 上から落ちてくる演出
        if (animatedBookWorld != null)
        {
            Vector3 startPos = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            Vector3 endPos = bookCenterWorldPos;

            // ★超重要：画面に表示（SetActive）する「前」に、空の上の座標へセットする！
            // これで画面中央に1コマだけ映る現象が物理的に不可能になります
            animatedBookWorld.transform.position = startPos;
            animatedBookWorld.SetActive(true);

            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dropDuration;

                // ドサッと落ちるバウンスイージング
                t = t - 1f;
                float bounce = t * t * ((1.70158f + 1f) * t + 1.70158f) + 1f;
                animatedBookWorld.transform.position = Vector3.LerpUnclamped(startPos, endPos, bounce);
                yield return null;
            }
            animatedBookWorld.transform.position = endPos;

            if (audioSource != null && bookDropSE != null) audioSource.PlayOneShot(bookDropSE);

            // 4. 着地してから開く
            if (worldBookAnimator != null) worldBookAnimator.SetTrigger(openTriggerName);
            if (audioSource != null && bookOpenSE != null) audioSource.PlayOneShot(bookOpenSE);

            yield return new WaitForSecondsRealtime(openAnimDuration);
        }

        // 5. 本物のUI本にすり替え
        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(true);
        if (menuModePanel != null) menuModePanel.SetActive(true);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);

        isAnimating = false;
    }

    // --- ★【新機能】メニューページ ➔ 図鑑ページへめくる ---
    public void SwitchToEncyclopedia()
    {
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);

        if (menuModePanel != null) menuModePanel.SetActive(false);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(true);
    }

    // --- ★【新機能】図鑑ページ ➔ メニューページへ戻る ---
    public void SwitchToMenu()
    {
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);

        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);
        if (menuModePanel != null) menuModePanel.SetActive(true);
    }

    // --- 本を閉じて上へ飛び去る演出 ---
    IEnumerator CloseAndFlyUpRoutine()
    {
        isAnimating = true;
        AlignWorldBookToUI();
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);

        if (animatedBookWorld != null)
        {
            animatedBookWorld.SetActive(true);
            animatedBookWorld.transform.position = bookCenterWorldPos;

            if (worldBookAnimator != null) worldBookAnimator.SetTrigger(closeTriggerName);
            if (audioSource != null && bookCloseSE != null) audioSource.PlayOneShot(bookCloseSE);

            yield return new WaitForSecondsRealtime(closeAnimDuration);

            if (audioSource != null && bookFlyUpSE != null) audioSource.PlayOneShot(bookFlyUpSE);

            Vector3 startPos = bookCenterWorldPos;
            Vector3 endPos = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            float elapsed = 0f;

            while (elapsed < flyUpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / flyUpDuration;
                animatedBookWorld.transform.position = Vector3.Lerp(startPos, endPos, t * t);
                yield return null;
            }

            animatedBookWorld.SetActive(false);
        }

        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);

        isAnimating = false;
        isMenuOpen = false;
    }

    // --- 閉じるボタン ---
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

    // --- 最初からやり直すボタン ---
    public void OnFullResetClicked()
    {
        if (isAnimating) return;
        PlayClickSE();
        StartCoroutine(FullResetAction());
    }

    IEnumerator FullResetAction()
    {
        yield return StartCoroutine(CloseAndFlyUpRoutine());
        if (CheckpointManager.instance != null) CheckpointManager.instance.ClearCheckpoint();
        Time.timeScale = previousTimeScale;
        if (GameManager.instance != null) GameManager.instance.ResetGame();
    }

    // --- ★【修正】ステージセレクトへ戻るボタン ---
    public void OnReturnToStageSelectClicked()
    {
        Debug.Log("<color=yellow>【ステージセレクトへ戻る】ボタンが押されました</color>");
        PlayClickSE();

        // ★最重要：フリーズを防ぐため、真っ先に時間を1.0に戻す！
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        StartCoroutine(StageSelectAction());
    }

    IEnumerator StageSelectAction()
    {
        // 1. 本が閉じて上に飛んでいく
        yield return StartCoroutine(CloseAndFlyUpRoutine());

        // 2. 黒い板が降りてくる（もし設定されていれば）
        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.2f);

        // 3. セレクト画面へ遷移
        Debug.Log("StageSelect シーンをロードします");
        SceneLoader.Load("StageSelect", GimmickType.Generic);
    }

    void AlignWorldBookToUI()
    {
        if (staticBookUI == null) return;
        Canvas parentCanvas = staticBookUI.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            if (Camera.main != null)
            {
                Vector3 screenPos = staticBookUI.position;
                screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
                bookCenterWorldPos = Camera.main.ScreenToWorldPoint(screenPos);
            }
        }
        else
        {
            bookCenterWorldPos = staticBookUI.position;
        }
        bookCenterWorldPos.z = 0f;
        bookCenterWorldPos += animationOffset;
    }

    void PlayClickSE()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
    }
}