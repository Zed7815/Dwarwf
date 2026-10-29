using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // ★TextMeshPro用に追加

public class StageSelectNest : MonoBehaviour
{
    public static List<StageSelectNest> allNests = new List<StageSelectNest>();

    [System.Serializable]
    public class StageData
    {
        public string sceneName;
        public GimmickType gimmick;
        [Tooltip("星管理用のID（例: 1, 2）")]
        public int starStageID = 1;
        [Tooltip("このステージに星が存在するか（星がないチュートリアル面などはチェックを外す）")]
        public bool hasStarFeature = true;
    }

    [Header("巣の識別")]
    [Tooltip("巣の番号（1, 2, 3...）")]
    public int nestIndex = 1;

    [Header("★星の数によるロック設定（第6ステージ用）")]
    [Tooltip("解放に必要な星の合計数（0なら星ロックなし）")]
    public int requiredStarsToUnlock = 0;
    [Tooltip("ロック中に表示する鍵マークのオブジェクト")]
    public GameObject starLockGraphic;
    [Tooltip("鍵マークの上に表示する星の個数テキスト（TextMeshPro）")]
    public TMP_Text starLockCountText;
    [Tooltip("ロック中にクリックした時の拒否音")]
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
    [Tooltip("簡単・難しいのどちらかに星の取り逃しがある時に表示するビックリマーク")]
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
        // Lキーでデバッグ全開放
        if ((Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.L))
        {
            StageSelectManager ssm = FindObjectOfType<StageSelectManager>();
            if (ssm != null) ssm.UnlockAllStages();
        }

        // フェード中（シーン遷移中）ならクリック判定を一切行わない
        if (StageSelectPopupButton.isAnyTransitioning) return;

        // ポップアップが開いている時に画面の他の場所をクリックしたら閉じる
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

        // 現在集めている星の総数をカウント
        int currentTotalStars = GetCurrentTotalStars();

        // ★星のロック判定
        bool isStarConditionMet = true;
        if (requiredStarsToUnlock > 0)
        {
            isStarConditionMet = (currentTotalStars >= requiredStarsToUnlock);
        }

        // 前の巣をクリアしており、かつ必要な星の数も満たしていれば解放！
        isNestUnlocked = isPrevNestCleared && isStarConditionMet;

        // ★鍵マークと星の個数テキストの表示更新
        if (requiredStarsToUnlock > 0)
        {
            // 必要数の星を集めたら鍵マークを消す（集まっていなければ表示）
            bool showLock = !isStarConditionMet;

            if (starLockGraphic != null)
            {
                starLockGraphic.SetActive(showLock);
            }

            if (starLockCountText != null)
            {
                starLockCountText.gameObject.SetActive(showLock);
                // 例: 「3 / 10」のように表示（現在の個数だけ出したい場合は currentTotalStars.ToString() に変更可）
                starLockCountText.text = $"{currentTotalStars} / {requiredStarsToUnlock}";
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

        bool hasEasyStar = PlayerPrefs.GetInt($"StarCollected_Stage_{easyStage.starStageID}", 0) == 1;
        bool hasHardStar = PlayerPrefs.GetInt($"StarCollected_Stage_{hardStage.starStageID}", 0) == 1;

        bool isFullyCleared = isEasyCleared && isHardCleared;
        if (animalVisual != null) animalVisual.SetActive(isFullyCleared);

        // 各ステージのビックリマーク条件
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

    // 獲得している星の総数を計算
    int GetCurrentTotalStars()
    {
        int totalStars = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) totalStars++;
        }
        return totalStars;
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
        if (StageSelectPopupButton.isAnyTransitioning) return;

        // ロック中なら拒否音を鳴らして終了
        if (!isNestUnlocked)
        {
            if (lockedDeniedSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedDeniedSE);
            }
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