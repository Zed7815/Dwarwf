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
        [Tooltip("★このステージに星が存在するか（星がないチュートリアル面などはチェックを外す）")]
        public bool hasStarFeature = true;
    }

    [Header("巣の識別")]
    [Tooltip("巣の番号（1, 2, 3...）")]
    public int nestIndex = 1;

    [Header("難易度別ステージ設定")]
    public StageData easyStage;
    public StageData hardStage;

    [Header("巣穴本体のホバー演出")]
    [Tooltip("★巣穴にカーソルを乗せた時に表示する枠Sprite")]
    public GameObject nestHoverFrame;
    public AudioClip nestHoverSE;

    [Header("UI・ビジュアル参照")]
    [Tooltip("巣の頭上に配置するポップアップルートオブジェクト")]
    public GameObject popupRoot;
    public StageSelectPopupButton easyButton;
    public StageSelectPopupButton hardButton;
    [Tooltip("両方クリア時に表示する動物ビジュアル")]
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
        isNestUnlocked = (nestIndex <= clearedNestCount + 1);

        string easyKey = $"Nest_{nestIndex}_Easy_Cleared";
        string hardKey = $"Nest_{nestIndex}_Hard_Cleared";

        bool isEasyCleared = PlayerPrefs.GetInt(easyKey, 0) == 1;
        bool isHardCleared = PlayerPrefs.GetInt(hardKey, 0) == 1;

        bool hasEasyStar = PlayerPrefs.GetInt($"StarCollected_Stage_{easyStage.starStageID}", 0) == 1;
        bool hasHardStar = PlayerPrefs.GetInt($"StarCollected_Stage_{hardStage.starStageID}", 0) == 1;

        bool isFullyCleared = isEasyCleared && isHardCleared;
        if (animalVisual != null) animalVisual.SetActive(isFullyCleared);

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

    // ★巣穴本体のマウスホバー処理
    void OnMouseEnter()
    {
        // 巣が解放されており、ポップアップがまだ開いていない時だけ枠を出す
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

            // ポップアップを開いた時は巣穴のホバー枠を消す
            if (nestHoverFrame != null) nestHoverFrame.SetActive(false);

            if (nextState && popupOpenSE != null && audioSource != null)
            {
                audioSource.PlayOneShot(popupOpenSE);
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