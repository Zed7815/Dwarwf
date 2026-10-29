using UnityEngine;
using System.Collections.Generic;

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

    [Header("巣穴のアニメーション設定")]
    public Animator nestAnimator;
    public string lockedStateName = "Locked";
    public string unlockedStateName = "Unlocked";

    [Header("巣穴の画像設定（フォールバック用）")]
    public SpriteRenderer nestRenderer;
    public Sprite lockedNestSprite;
    public Sprite unlockedNestSprite;

    [Header("★巣穴本体のビックリマーク")]
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
        isNestUnlocked = (nestIndex <= clearedNestCount + 1);

        UpdateNestVisual();

        string easyKey = $"Nest_{nestIndex}_Easy_Cleared";
        string hardKey = $"Nest_{nestIndex}_Hard_Cleared";

        bool isEasyCleared = PlayerPrefs.GetInt(easyKey, 0) == 1;
        bool isHardCleared = PlayerPrefs.GetInt(hardKey, 0) == 1;

        bool hasEasyStar = PlayerPrefs.GetInt($"StarCollected_Stage_{easyStage.starStageID}", 0) == 1;
        bool hasHardStar = PlayerPrefs.GetInt($"StarCollected_Stage_{hardStage.starStageID}", 0) == 1;

        bool isFullyCleared = isEasyCleared && isHardCleared;
        if (animalVisual != null) animalVisual.SetActive(isFullyCleared);

        // ★各ステージのビックリマーク条件
        bool easyWarning = easyStage.hasStarFeature && isEasyCleared && !hasEasyStar;
        bool hardWarning = hardStage.hasStarFeature && isHardCleared && !hasHardStar;

        // ★どちらか片方でも星の取り逃しがあれば、巣穴本体にもビックリマークを表示！
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
        if (!isNestUnlocked) return;

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