using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class StageSelectManager : MonoBehaviour
{
    public Button[] stageButtons;
    public string[] stageSceneNames;

    [Header("演出設定")]
    [Tooltip("シーン開始時に黒い板が退く演出スクリプト")]
    public nextscene fadeInScript;
    [Tooltip("ボタン押下時に黒い板が降りてくる演出スクリプト")]
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
    static void Init()
    {
        sessionResetDone = false;
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // シーン開始時のフェードイン（黒い板がどく）演出
        if (fadeInScript != null)
        {
            StartCoroutine(fadeInScript.startKuro());
        }

        if (!sessionResetDone)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            sessionResetDone = true;
            Debug.Log("セーブデータを完全に初期化しました");
        }

        UpdateStarCountUI();
        RefreshStageButtons();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) UnlockAllStages();
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

    public void RefreshStageButtons()
    {
        int clearedStage = PlayerPrefs.GetInt("StageCleared", 0);
        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            bool isUnlocked = (i <= clearedStage);
            bool isActuallyCleared = (i < clearedStage);

            stageButtons[i].interactable = isUnlocked;
            stageButtons[i].onClick.RemoveAllListeners();

            // ★ここだけ修正：ボタンの中にある "ClearedVisual" を探して表示・非表示を切り替える
            Transform clearedVisual = stageButtons[i].transform.Find("ClearedVisual");
            if (clearedVisual != null)
            {
                clearedVisual.gameObject.SetActive(isActuallyCleared);
            }

            if (isUnlocked)
            {
                AddHoverEvent(stageButtons[i]);
                string sceneName = stageSceneNames.Length > i ? stageSceneNames[i] : "";
                stageButtons[i].onClick.AddListener(() => {
                    if (!string.IsNullOrEmpty(sceneName)) LoadStage(sceneName);
                });
            }
        }
    }

    void UnlockAllStages()
    {
        PlayerPrefs.SetInt("StageCleared", stageButtons.Length + 1);
        for (int i = 1; i <= 20; i++) PlayerPrefs.SetInt("StarCollected_Stage_" + i, 1);
        PlayerPrefs.Save();
        UpdateStarCountUI();
        RefreshStageButtons();
        FinalStageLock fsl = FindObjectOfType<FinalStageLock>();
        if (fsl != null) fsl.RefreshLockStatus();
    }

    void AddHoverEvent(Button btn)
    {
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => { if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE); });
        trigger.triggers.Add(entry);
    }

    public void LoadStage(string sceneName) { StartCoroutine(LoadSequence(sceneName)); }

    IEnumerator LoadSequence(string sceneName)
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);

        yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(sceneName);
    }
}