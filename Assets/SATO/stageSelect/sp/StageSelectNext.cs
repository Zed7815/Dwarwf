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
    }

    [Header("巣の識別")]
    [Tooltip("巣の番号（1, 2, 3...）")]
    public int nestIndex = 1;

    [Header("難易度別ステージ設定")]
    public StageData easyStage;
    public StageData hardStage;

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

                // クリック対象がこの巣またはポップアップに含まれていなければ閉じる
                if (hit.collider == null || (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)))
                {
                    ClosePopup();
                }
            }
        }
    }

    /// <summary>
    /// セーブデータをもとに巣と各ボタンの状態を更新
    /// </summary>
    public void RefreshNestStatus()
    {
        int clearedNestCount = PlayerPrefs.GetInt("NestClearedCount", 0);

        // 前の巣を完全クリアしているか、最初の巣であれば開放
        isNestUnlocked = (nestIndex <= clearedNestCount + 1);

        // クリア判定キー
        string easyKey = $"Nest_{nestIndex}_Easy_Cleared";
        string hardKey = $"Nest_{nestIndex}_Hard_Cleared";

        bool isEasyCleared = PlayerPrefs.GetInt(easyKey, 0) == 1;
        bool isHardCleared = PlayerPrefs.GetInt(hardKey, 0) == 1;

        bool hasEasyStar = PlayerPrefs.GetInt($"StarCollected_Stage_{easyStage.starStageID}", 0) == 1;
        bool hasHardStar = PlayerPrefs.GetInt($"StarCollected_Stage_{hardStage.starStageID}", 0) == 1;

        // 簡単と難しいの両方がクリアされていれば巣全体のクリア
        bool isFullyCleared = isEasyCleared && isHardCleared;
        if (animalVisual != null) animalVisual.SetActive(isFullyCleared);

        // 「かんたん」は巣が開いていれば挑戦可能
        bool canPlayEasy = isNestUnlocked;
        // 「むずかしい」は「かんたん」クリアで挑戦可能
        bool canPlayHard = isNestUnlocked && isEasyCleared;

        if (easyButton != null)
        {
            easyButton.SetupButton(canPlayEasy, isEasyCleared, hasEasyStar, easyStage.sceneName, easyStage.gimmick, this);
        }

        if (hardButton != null)
        {
            hardButton.SetupButton(canPlayHard, isHardCleared, hasHardStar, hardStage.sceneName, hardStage.gimmick, this);
        }
    }

    void OnMouseDown()
    {
        if (!isNestUnlocked) return;

        // 他の巣のポップアップを閉じる
        CloseAllOtherPopups();

        // ポップアップを開く
        if (popupRoot != null)
        {
            bool nextState = !popupRoot.activeSelf;
            popupRoot.SetActive(nextState);

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