using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class FinalStageLock : MonoBehaviour
{
    [Header("設定")]
    public int requiredStars = 5;
    public int thisStageNumber = 6;    // 最終ステージの番号
    public string finalStageSceneName;
    public TextMeshProUGUI needStarText;

    [Header("演出設定")]
    public nextscene fadeOutScript;
    public GameObject lockGraphic;     // 鍵の画像など

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip hoverSE;
    public AudioClip clickSE;
    public AudioClip enterSE;

    private Button btn;

    void Start()
    {
        btn = GetComponent<Button>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        RefreshLockStatus();
    }

    public void RefreshLockStatus()
    {
        // 1. 星の数を計算
        int currentStars = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) currentStars++;
        }

        int clearedStageNum = PlayerPrefs.GetInt("StageCleared", 0);
        // ★自分が Stage 6 なら、clearedStageNum が 6以上ならクリア済み
        bool isActuallyCleared = (clearedStageNum >= thisStageNumber);

        // クリア済みビジュアルの表示
        Transform cv = transform.Find("ClearedVisual");
        if (cv != null) cv.gameObject.SetActive(isActuallyCleared);


        // 2. 状態判定
        bool isUnlocked = (currentStars >= requiredStars) || isActuallyCleared; // クリア済みなら当然解放

        // 3. クリア済みビジュアルの処理
        // transform.Find は直下の子しか探せないので、確実に見つけるための処理
        GameObject clearedVisualObj = null;
        Transform cvTransform = transform.Find("ClearedVisual");
        if (cvTransform != null) clearedVisualObj = cvTransform.gameObject;

        if (clearedVisualObj != null)
        {
            clearedVisualObj.SetActive(isActuallyCleared);
        }

        // 4. 他の表示物の制御
        if (needStarText != null)
        {
            // すでにクリアしているならテキストは邪魔なので消す、そうでなければ数値を出す
            if (isActuallyCleared) needStarText.gameObject.SetActive(false);
            else needStarText.text = currentStars + " / " + requiredStars;
        }

        if (lockGraphic != null)
        {
            // 解放されている、またはクリア済みなら鍵を隠す
            lockGraphic.SetActive(!isUnlocked && !isActuallyCleared);
        }

        // 5. ボタンとイベントの設定
        if (btn != null)
        {
            btn.interactable = isUnlocked;
            btn.onClick.RemoveAllListeners();

            if (isUnlocked)
            {
                AddHoverEvent(btn);
                btn.onClick.AddListener(() => {
                    StartCoroutine(LoadSequence());
                });
            }
        }
    }

    void AddHoverEvent(Button button)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener((data) => {
            if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE);
        });
        trigger.triggers.Add(entry);
    }

    IEnumerator LoadSequence()
    {
        if (audioSource != null && clickSE != null) audioSource.PlayOneShot(clickSE);
        if (audioSource != null && enterSE != null) audioSource.PlayOneShot(enterSE);

        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(finalStageSceneName);
    }
}