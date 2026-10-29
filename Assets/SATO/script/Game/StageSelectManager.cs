using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class StageSelectManager : MonoBehaviour
{
    [Header("左上の図鑑ボタン（シーン移動時に消す用）")]
    public GameObject openBookButton;

    [System.Serializable]
    public class StageSetting
    {
        public string stageName;
        public Button button;
        public string sceneName;
        public GimmickType stageGimmick;
        public GameObject animalVisual;
        public GameObject missingStarVisual;
    }

    [Header("ステージ別設定")]
    public List<StageSetting> stageSettings;

    [Header("演出設定")]
    public nextscene fadeInScript;
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip enterSE;

    [Header("星のUI設定")]
    public TextMeshProUGUI totalStarText;

    private static bool sessionResetDone = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() { sessionResetDone = false; }

    void Start()
    {
        if (!sessionResetDone)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            sessionResetDone = true;
            Debug.Log("初回起動：リセット実行");
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (fadeInScript != null) StartCoroutine(fadeInScript.startKuro());

        UpdateStarCountUI();
        RefreshStageButtons();

        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        bool hasSeenBookTutorial = PlayerPrefs.GetInt("Tutorial_Seen_SelectBook", 0) == 1;

        if (clearedStage >= 1 && !hasSeenBookTutorial)
        {
            PlayerPrefs.SetInt("Tutorial_Seen_SelectBook", 1);
            PlayerPrefs.Save();
            StartCoroutine(ShowBookTutorialDelay());
        }
    }

    IEnumerator ShowBookTutorialDelay()
    {
        yield return new WaitForSeconds(1.5f);
        if (TutorialMessageUI.instance != null)
        {
            TutorialMessageUI.instance.ShowMessage("画面左上の「本（図鑑）」から\nいつでも動物のヒントが見られるよ！");
        }
    }

    void Update()
    {
        // ★新旧入力システム両対応で確実にLキーを検知
        bool lKeyPressed = false;
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) lKeyPressed = true;
        if (Input.GetKeyDown(KeyCode.L)) lKeyPressed = true;

        if (lKeyPressed)
        {
            UnlockAllStages();
        }
    }

    public void RefreshStageButtons()
    {
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);

        for (int i = 0; i < stageSettings.Count; i++)
        {
            StageSetting setting = stageSettings[i];
            if (setting.button == null) continue;

            int stageNum = i + 1;
            bool isUnlocked = (i <= clearedStage);
            bool isActuallyCleared = (i < clearedStage);
            bool hasStar = (PlayerPrefs.GetInt("StarCollected_Stage_" + stageNum, 0) == 1);

            setting.button.interactable = isUnlocked;
            setting.button.onClick.RemoveAllListeners();

            if (setting.animalVisual != null)
            {
                setting.animalVisual.SetActive(isActuallyCleared);
            }

            if (setting.missingStarVisual != null)
            {
                bool shouldShowWarning = (isActuallyCleared && !hasStar);
                setting.missingStarVisual.SetActive(shouldShowWarning);
            }

            if (isUnlocked)
            {
                AddHoverEvent(setting.button);
                setting.button.onClick.AddListener(() => {
                    PlayerPrefs.SetInt("LastSelectedStage", stageNum);
                    PlayerPrefs.Save();

                    if (stageNum == 1)
                    {
                        StageOneGuide guide = FindObjectOfType<StageOneGuide>();
                        if (guide != null) guide.HideGuide();
                    }

                    if (!string.IsNullOrEmpty(setting.sceneName)) LoadStage(setting.sceneName, setting.stageGimmick);
                });
            }
        }

        FinalStageLock fsl = FindObjectOfType<FinalStageLock>();
        if (fsl != null) fsl.RefreshLockStatus();
    }

    public void UpdateStarCountUI()
    {
        int totalStars = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) totalStars++;
        }
        if (totalStarText != null) totalStarText.text = totalStars.ToString();
    }

    // ★ここを巣穴システム完全対応に修正！
    public void UnlockAllStages()
    {
        // 1. 巣穴（Nest）の数を計算（最低でも10個分保存）
        int totalNests = Mathf.Max(10, StageSelectNest.allNests.Count);
        if (stageSettings != null && stageSettings.Count > 0)
        {
            totalNests = Mathf.Max(totalNests, stageSettings.Count);
        }

        // 2. 巣穴システム用のセーブデータを全解放
        PlayerPrefs.SetInt("NestClearedCount", totalNests);
        PlayerPrefs.SetInt("StageCleared", totalNests + 1);

        for (int i = 1; i <= totalNests; i++)
        {
            PlayerPrefs.SetInt($"Nest_{i}_Easy_Cleared", 1); // 簡単クリア
            PlayerPrefs.SetInt($"Nest_{i}_Hard_Cleared", 1); // 難しいクリア
        }

        // 3. 星も全ステージ（1〜30）獲得済みにする
        for (int i = 1; i <= 30; i++)
        {
            PlayerPrefs.SetInt("StarCollected_Stage_" + i, 1);
        }

        // 4. エンディング・最終面フラグも解放
        PlayerPrefs.SetInt("GameFullCleared", 1);
        PlayerPrefs.Save();

        // 5. ★最重要：画面上のすべての巣穴（StageSelectNest）に「見た目を更新せよ！」と命令を送る
        foreach (var nest in StageSelectNest.allNests)
        {
            if (nest != null)
            {
                nest.RefreshNestStatus();
            }
        }

        // 6. UIと各ボタン・ロック表示をすべて更新
        UpdateStarCountUI();
        RefreshStageButtons();

        FinalStageLock fsl = FindObjectOfType<FinalStageLock>();
        if (fsl != null) fsl.RefreshLockStatus();

        FinalEntrance entrance = FindObjectOfType<FinalEntrance>();
        if (entrance != null) entrance.RefreshEntrance();

        Debug.Log("<color=green>【デバッグ】Lキー入力：全巣穴・全難易度（簡単/難しい）・全スターを完全開放しました！</color>");
    }

    void AddHoverEvent(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener((data) => { if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE); });
        trigger.triggers.Add(entry);
    }

    public void LoadStage(string sceneName, GimmickType gimmick)
    {
        StartCoroutine(LoadSequence(sceneName, gimmick));
    }

    IEnumerator LoadSequence(string sceneName, GimmickType gimmick)
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);

        if (openBookButton != null)
        {
            openBookButton.SetActive(false);
        }
        if (InGameMenuController.instance != null && InGameMenuController.instance.openBookButtonInSelect != null)
        {
            InGameMenuController.instance.openBookButtonInSelect.SetActive(false);
        }

        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.5f);

        SceneLoader.Load(sceneName, gimmick);
    }
}