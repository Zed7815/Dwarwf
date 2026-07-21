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
    // 各ステージの設定をまとめるクラス
    [System.Serializable]
    public class StageSetting
    {
        public string stageName;           // 識別用（例：Stage1）
        public Button button;              // ステージボタン
        public string sceneName;           // 遷移先シーン名
        public GameObject animalVisual;    // クリア時に出る動物
        public GameObject missingStarVisual; // 【追加】クリア済みかつ星未獲得時に出るビックリマーク等
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
            Debug.Log("<color=red>アプリ起動：セーブデータをすべて初期化しました</color>");
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (fadeInScript != null) StartCoroutine(fadeInScript.startKuro());

        UpdateStarCountUI();
        RefreshStageButtons();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) UnlockAllStages();
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

            // 1. 動物ビジュアルの表示切り替え（直接指定）
            if (setting.animalVisual != null)
            {
                setting.animalVisual.SetActive(isActuallyCleared);
            }

            // 2. 星未獲得ビジュアルの表示切り替え（直接指定）
            if (setting.missingStarVisual != null)
            {
                // 「クリア済み」かつ「星を持っていない」時だけ表示
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

                    if (!string.IsNullOrEmpty(setting.sceneName)) LoadStage(setting.sceneName);
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

    void UnlockAllStages()
    {
        PlayerPrefs.SetInt("StageCleared", stageSettings.Count + 1);
        for (int i = 1; i <= 30; i++) PlayerPrefs.SetInt("StarCollected_Stage_" + i, 1);
        PlayerPrefs.Save();
        UpdateStarCountUI();
        RefreshStageButtons();
    }

    void AddHoverEvent(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener((data) => { if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE); });
        trigger.triggers.Add(entry);
    }

    public void LoadStage(string sceneName) { StartCoroutine(LoadSequence(sceneName)); }

    IEnumerator LoadSequence(string sceneName)
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(sceneName);
    }
}