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

    [Header("星のUI設定")]
    public TextMeshProUGUI totalStarText;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip enterSE;

    // 同一セッション（起動中）かどうかを判定するフラグ
    private static bool sessionResetDone = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // ★修正点：ゲーム起動後、最初の1回目だけデータを全消去する
        if (!sessionResetDone)
        {
            PlayerPrefs.DeleteAll(); // ステージ進捗も星のデータもすべて削除
            PlayerPrefs.Save();
            sessionResetDone = true; // 次にこの画面に来た時はこの処理を飛ばす
            Debug.Log("ゲーム起動：セーブデータを初期状態にリセットしました");
        }

        UpdateStarCountUI();
        RefreshStageButtons();
    }

    void Update()
    {
        // デバッグ用ショートカット：Lキーで全開放（星も含む）
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            UnlockAllStages();
        }
    }

    public void UpdateStarCountUI()
    {
        int totalStars = 0;
        // 取得済みの星を合算
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
            stageButtons[i].interactable = isUnlocked;
            stageButtons[i].onClick.RemoveAllListeners();

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
        Debug.Log("デバッグ：全ステージ・全星を開放します");
        PlayerPrefs.SetInt("StageCleared", stageButtons.Length + 1);
        for (int i = 1; i <= 20; i++) PlayerPrefs.SetInt("StarCollected_Stage_" + i, 1);
        PlayerPrefs.Save();

        UpdateStarCountUI();
        RefreshStageButtons();

        FinalStageLock fsl = FindObjectOfType<FinalStageLock>();
        if (fsl != null) fsl.RefreshLockStatus();
    }

    // --- 以下、イベント・ロード処理（変更なし） ---
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
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(sceneName);
    }
}