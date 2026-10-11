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
    public GameObject menuModePanel;
    public GameObject encyclopediaPanel;

    [Header("位置の微調整")]
    public Vector3 animationOffset = Vector3.zero;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip bookDropSE;
    public AudioClip bookOpenSE;
    public AudioClip bookCloseSE;
    public AudioClip bookFlyUpSE;
    public AudioClip pageFlipSE;
    public AudioClip clickSE;       // ★各ボタン共通のクリック音

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("左ページ（ステージ情報・メモ）のUI参照")]
    public TextMeshProUGUI stageTitleText;
    public TextMeshProUGUI thisStageStarText;
    public GameObject thisStageStarCheckIcon;
    public TextMeshProUGUI totalStarText;
    public Image playerPortraitImage;
    public TextMeshProUGUI hintMemoText;

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
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        AlignWorldBookToUI();

        if (animatedBookWorld != null)
        {
            animatedBookWorld.transform.position = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            animatedBookWorld.SetActive(false);
        }

        if (darkOverlayUI != null) darkOverlayUI.SetActive(false);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);
    }

    public void OpenMenu()
    {
        if (isMenuOpen || isAnimating) return;
        isMenuOpen = true;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        UpdateLeftPageInfo();

        StopAllCoroutines();
        StartCoroutine(OpenSequence());
    }

    void UpdateLeftPageInfo()
    {
        int currentStageNum = 1;
        string currentTitle = "STAGE";

        if (StageInfo.instance != null)
        {
            currentStageNum = StageInfo.instance.stageNumber;
            currentTitle = $"STAGE {currentStageNum} - {StageInfo.instance.stageTitle}";

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
        AlignWorldBookToUI();

        if (staticBookUI != null) staticBookUI.gameObject.SetActive(false);
        if (darkOverlayUI != null) darkOverlayUI.SetActive(true);

        if (animatedBookWorld != null)
        {
            Vector3 startPos = bookCenterWorldPos + new Vector3(0, dropDistanceY, 0);
            Vector3 endPos = bookCenterWorldPos;

            animatedBookWorld.transform.position = startPos;
            animatedBookWorld.SetActive(true);

            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dropDuration;
                t = t - 1f;
                float bounce = t * t * ((1.70158f + 1f) * t + 1.70158f) + 1f;
                animatedBookWorld.transform.position = Vector3.LerpUnclamped(startPos, endPos, bounce);
                yield return null;
            }
            animatedBookWorld.transform.position = endPos;

            if (audioSource != null && bookDropSE != null) audioSource.PlayOneShot(bookDropSE);

            if (worldBookAnimator != null) worldBookAnimator.SetTrigger(openTriggerName);
            if (audioSource != null && bookOpenSE != null) audioSource.PlayOneShot(bookOpenSE);

            yield return new WaitForSecondsRealtime(openAnimDuration);
        }

        if (animatedBookWorld != null) animatedBookWorld.SetActive(false);
        if (staticBookUI != null) staticBookUI.gameObject.SetActive(true);
        if (menuModePanel != null) menuModePanel.SetActive(true);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);

        isAnimating = false;
    }

    public void SwitchToEncyclopedia()
    {
        PlayClickSE();
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);

        if (menuModePanel != null) menuModePanel.SetActive(false);
        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(true);
    }

    public void SwitchToMenu()
    {
        PlayClickSE();
        if (audioSource != null && pageFlipSE != null) audioSource.PlayOneShot(pageFlipSE);

        if (encyclopediaPanel != null) encyclopediaPanel.SetActive(false);
        if (menuModePanel != null) menuModePanel.SetActive(true);
    }

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

    // --- ★1. 閉じるボタン（SEを追加！） ---
    public void OnCloseClicked()
    {
        if (isAnimating) return;
        PlayClickSE(); // ★SE再生を追加！
        StartCoroutine(CloseMenuAction());
    }

    IEnumerator CloseMenuAction()
    {
        yield return StartCoroutine(CloseAndFlyUpRoutine());
        Time.timeScale = previousTimeScale;
    }

    // --- ★2. 最初からやり直すボタン（SEと完全リセットを保証） ---
    public void OnFullResetClicked()
    {
        if (isAnimating) return;
        PlayClickSE(); // ★SE再生
        StartCoroutine(FullResetAction());
    }

    IEnumerator FullResetAction()
    {
        // 1. 本が閉じて空へ飛んでいく演出
        yield return StartCoroutine(CloseAndFlyUpRoutine());

        // 2. チェックポイント旗の記憶をすべて消去（旗を未通過色に戻す）
        if (CheckpointManager.instance != null)
        {
            CheckpointManager.instance.ClearCheckpoint();
        }

        // 3. ゲーム時間を通常に戻す
        Time.timeScale = 1f;

        // 4. ★GameManagerの完全リセットを実行
        // （自機がスタート位置・右向きに戻り、配置動物が初期位置に戻り、星がステージ上に復活する）
        if (GameManager.instance != null)
        {
            GameManager.instance.ResetGame();
        }

        Debug.Log("<color=yellow>【最初からやり直す】配置動物・星・自機を完全リセットしました！</color>");
    }

    // --- ★3. ステージセレクトへ戻るボタン（SEを確実に鳴らす） ---
    public void OnReturnToStageSelectClicked()
    {
        if (isAnimating) return;
        PlayClickSE(); // ★SE再生

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        StartCoroutine(StageSelectAction());
    }

    IEnumerator StageSelectAction()
    {
        yield return StartCoroutine(CloseAndFlyUpRoutine());

        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.2f);
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

    // SE再生（共通）
    void PlayClickSE()
    {
        if (clickSE != null)
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource != null) audioSource.PlayOneShot(clickSE);
        }
    }
}