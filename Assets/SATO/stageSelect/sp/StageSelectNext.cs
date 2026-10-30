using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class StageSelectNest : MonoBehaviour
{
    public static List<StageSelectNest> allNests = new List<StageSelectNest>();

    [System.Serializable]
    public class StageData
    {
        public string sceneName;
        public GimmickType gimmick;
        public int starStageID = 1;
        [Tooltip("このステージに星が存在するか（星がないチュートリアル面などはチェックを外す）")]
        public bool hasStarFeature = true;
    }

    [Header("巣の識別")]
    [Tooltip("巣の番号（1, 2, 3...）")]
    public int nestIndex = 1;

    [Header("★星の数によるロック設定（第6ステージ用）")]
    [Tooltip("解放に必要な星の合計数（例: 10 など）")]
    public int requiredStarsToUnlock = 0;
    public GameObject starLockGraphic;
    public TMP_Text starLockCountText;
    public AudioClip lockedDeniedSE;

    [Header("巣穴のアニメーション設定")]
    public Animator nestAnimator;
    public string lockedStateName = "Locked";
    public string unlockedStateName = "Unlocked";

    [Header("巣穴の画像設定（フォールバック用）")]
    public SpriteRenderer nestRenderer;
    public Sprite lockedNestSprite;
    public Sprite unlockedNestSprite;

    [Header("巣穴本体のビックリマーク")]
    public GameObject nestMissingStarVisual;

    [Header("難易度別ステージ設定")]
    public StageData easyStage;
    public StageData hardStage;

    [Header("巣穴本体のホバー演出")]
    public GameObject nestHoverFrame;
    public AudioClip nestHoverSE;

    [Header("UI・ビジュアル参照")]
    public GameObject popupRoot;
    public StageSelectPopupButton easyButton;
    public StageSelectPopupButton hardButton;
    public GameObject animalVisual;

    [Header("SE")]
    public AudioClip popupOpenSE;
    private AudioSource audioSource;

    private bool isNestUnlocked = false;

    void Awake()
    {
        if (!allNests.Contains(this)) allNests.Add(this);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (nestRenderer == null) nestRenderer = GetComponent<SpriteRenderer>();
        if (nestAnimator == null) nestAnimator = GetComponent<Animator>();
        if (nestHoverFrame != null) nestHoverFrame.SetActive(false);
    }

    void OnDestroy()
    {
        allNests.Remove(this);
    }

    void Start()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
        RefreshNestStatus();
    }

    void Update()
    {
        if (InGameMenuController.IsMenuOpen) return;

        if ((Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.L))
        {
            StageSelectManager ssm = FindObjectOfType<StageSelectManager>();
            if (ssm != null) ssm.UnlockAllStages();
        }

        if (StageSelectPopupButton.isAnyTransitioning) return;

        if (popupRoot != null && popupRoot.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

                if (hit.collider == null || (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)))
                {
                    ClosePopup();
                }
            }
        }
    }

    public void RefreshNestStatus()
    {
        int clearedNestCount = PlayerPrefs.GetInt("NestClearedCount", 0);
        bool isPrevNestCleared = (nestIndex <= clearedNestCount + 1);

        // 星の合計数を計算
        int currentTotalStars = GetCurrentTotalStars();

        bool isStarConditionMet = true;
        if (requiredStarsToUnlock > 0)
        {
            isStarConditionMet = (currentTotalStars >= requiredStarsToUnlock);
        }

        isNestUnlocked = isPrevNestCleared && isStarConditionMet;

        // 鍵マークの更新
        if (requiredStarsToUnlock > 0)
        {
            bool showLock = !isStarConditionMet;
            if (starLockGraphic != null) starLockGraphic.SetActive(showLock);

            if (starLockCountText != null)
            {
                starLockCountText.gameObject.SetActive(showLock);
                // 文字化けを防ぐため「★」と「/」を明確に表示
                starLockCountText.text = $" {currentTotalStars} / {requiredStarsToUnlock}";
            }
        }
        else
        {
            if (starLockGraphic != null) starLockGraphic.SetActive(false);
            if (starLockCountText != null) starLockCountText.gameObject.SetActive(false);
        }

        UpdateNestVisual();

        string easyKey = $"Nest_{nestIndex}_Easy_Cleared";
        string hardKey = $"Nest_{nestIndex}_Hard_Cleared";

        bool isEasyCleared = PlayerPrefs.GetInt(easyKey, 0) == 1;
        bool isHardCleared = PlayerPrefs.GetInt(hardKey, 0) == 1;

        // ★自動キーと旧キーの両方をチェック（絶対に星を落とさない）
        bool hasEasyStar = PlayerPrefs.GetInt($"Nest_{nestIndex}_Easy_Star", 0) == 1
                        || PlayerPrefs.GetInt($"StarCollected_Stage_{easyStage.starStageID}", 0) == 1;

        bool hasHardStar = PlayerPrefs.GetInt($"Nest_{nestIndex}_Hard_Star", 0) == 1
                        || PlayerPrefs.GetInt($"StarCollected_Stage_{hardStage.starStageID}", 0) == 1;

        bool isFullyCleared = isEasyCleared && isHardCleared;
        if (animalVisual != null) animalVisual.SetActive(isFullyCleared);

        // ビックリマーク判定
        bool easyWarning = easyStage.hasStarFeature && isEasyCleared && !hasEasyStar;
        bool hardWarning = hardStage.hasStarFeature && isHardCleared && !hasHardStar;

        if (nestMissingStarVisual != null)
        {
            nestMissingStarVisual.SetActive(easyWarning || hardWarning);
        }

        bool canPlayEasy = isNestUnlocked;
        bool canPlayHard = isNestUnlocked && isEasyCleared;

        if (easyButton != null)
        {
            easyButton.SetupButton(canPlayEasy, isEasyCleared, hasEasyStar, easyStage.hasStarFeature, easyStage.sceneName, easyStage.gimmick, this);
        }

        if (hardButton != null)
        {
            hardButton.SetupButton(canPlayHard, isHardCleared, hasHardStar, hardStage.hasStarFeature, hardStage.sceneName, hardStage.gimmick, this);
        }
    }

    // ★全星の集計（新旧どちらのキーも合算）
    public static int GetCurrentTotalStars()
    {
        int total = 0;
        // 巣穴方式の星
        for (int n = 1; n <= 10; n++)
        {
            if (PlayerPrefs.GetInt($"Nest_{n}_Easy_Star", 0) == 1) total++;
            if (PlayerPrefs.GetInt($"Nest_{n}_Hard_Star", 0) == 1) total++;
        }
        // 旧方式の星（もしあれば重複しないよう大きい方を採用）
        int legacyTotal = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) legacyTotal++;
        }
        return Mathf.Max(total, legacyTotal);
    }

    void UpdateNestVisual()
    {
        if (nestAnimator == null) nestAnimator = GetComponent<Animator>();
        if (nestRenderer == null) nestRenderer = GetComponent<SpriteRenderer>();

        if (nestAnimator != null)
        {
            string targetState = isNestUnlocked ? unlockedStateName : lockedStateName;
            nestAnimator.Play(targetState, 0, 0f);
            nestAnimator.Update(0f);
        }

        if (nestRenderer != null)
        {
            if (isNestUnlocked && unlockedNestSprite != null)
            {
                nestRenderer.sprite = unlockedNestSprite;
            }
            else if (!isNestUnlocked && lockedNestSprite != null)
            {
                nestRenderer.sprite = lockedNestSprite;
            }
        }
    }

    void OnMouseEnter()
    {
        if (InGameMenuController.IsMenuOpen) return;
        if (StageSelectPopupButton.isAnyTransitioning) return;

        if (isNestUnlocked && (popupRoot == null || !popupRoot.activeSelf))
        {
            if (nestHoverFrame != null) nestHoverFrame.SetActive(true);
            if (nestHoverSE != null && audioSource != null) audioSource.PlayOneShot(nestHoverSE);
        }
    }

    void OnMouseExit()
    {
        if (nestHoverFrame != null) nestHoverFrame.SetActive(false);
    }

    void OnMouseDown()
    {
        if (InGameMenuController.IsMenuOpen) return;
        if (StageSelectPopupButton.isAnyTransitioning) return;

        if (!isNestUnlocked)
        {
            if (lockedDeniedSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedDeniedSE);
            }
            // ★診断用：なぜ入れないのかコンソールに出力
            Debug.Log($"<color=orange>【巣穴ロック中】現在の星: {GetCurrentTotalStars()}個 / 必要: {requiredStarsToUnlock}個</color>");
            return;
        }

        CloseAllOtherPopups(this);

        if (popupRoot != null)
        {
            bool nextState = !popupRoot.activeSelf;
            popupRoot.SetActive(nextState);

            if (nestHoverFrame != null) nestHoverFrame.SetActive(false);

            if (nextState)
            {
                RefreshNestStatus();

                if (popupOpenSE != null && audioSource != null)
                {
                    audioSource.PlayOneShot(popupOpenSE);
                }
            }
        }
    }

    public void ClosePopup()
    {
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    public static void CloseAllOtherPopups(StageSelectNest current = null)
    {
        foreach (var nest in allNests)
        {
            if (nest != current) nest.ClosePopup();
        }
    }
}