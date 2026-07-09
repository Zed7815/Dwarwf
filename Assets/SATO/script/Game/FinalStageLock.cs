using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // ホバー検知に必要
using System.Collections;

public class FinalStageLock : MonoBehaviour
{
    [Header("設定")]
    public int requiredStars = 5;
    public int thisStageNumber = 6;
    public string finalStageSceneName;
    public TextMeshProUGUI needStarText;

    [Header("演出設定")]
    public nextscene fadeOutScript;    // ★追加：フェードアウト用スクリプト
    public GameObject lockGraphic;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip hoverSE;          // ★追加：ホバー音
    public AudioClip clickSE;          // ★追加：クリック音
    public AudioClip enterSE;          // ★追加：決定音

    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        RefreshLockStatus();
    }

    public void RefreshLockStatus()
    {
        int currentStars = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) currentStars++;
        }

        bool isUnlocked = (currentStars >= requiredStars);
        int clearedStageNum = PlayerPrefs.GetInt("StageCleared", 0);
        bool isActuallyCleared = (clearedStageNum >= thisStageNumber);

        // クリア済みビジュアルの切り替え
        Transform clearedVisual = transform.Find("ClearedVisual");
        if (clearedVisual != null)
        {
            clearedVisual.gameObject.SetActive(isActuallyCleared);
        }

        if (needStarText != null)
        {
            if (isUnlocked) needStarText.text = requiredStars + " / " + requiredStars;
            else needStarText.text = currentStars + " / " + requiredStars;
        }

        if (lockGraphic != null) lockGraphic.SetActive(!isUnlocked);

        if (btn != null)
        {
            btn.interactable = isUnlocked;
            btn.onClick.RemoveAllListeners();

            if (isUnlocked)
            {
                // ★追加：ホバーイベントの登録
                AddHoverEvent(btn);

                // ★修正：クリック時にコルーチンを開始する
                btn.onClick.AddListener(() => {
                    StartCoroutine(LoadSequence());
                });
            }
        }
    }

    // ホバー音を鳴らすためのイベント登録
    void AddHoverEvent(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear(); // 重複防止

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => {
            if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE);
        });
        trigger.triggers.Add(entry);
    }

    // ★追加：演出を待ってからシーン移動する処理
    IEnumerator LoadSequence()
    {
        // 1. SE再生
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);

            yield return StartCoroutine(fadeOutScript.endKuro());
      

        // 3. 指示通りの待機時間
        yield return new WaitForSecondsRealtime(0.5f);

        // 4. シーン移動
        SceneManager.LoadScene(finalStageSceneName);
    }
}