using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class StageClearPopup : MonoBehaviour
{
    public static StageClearPopup instance;

    [Header("ポップアップ本体")]
    public GameObject clearPanel;
    public RectTransform popupWindow;

    [Header("次ステージ設定")]
    public string nextStageSceneName;
    public GimmickType nextStageGimmick = GimmickType.Generic;
    public bool isFinalStage = false;
    public string endingSceneName = "Ending";

    // ★【新機能】最終ステージへの門番設定
    [Header("★最終ステージ前の制限設定")]
    [Tooltip("次のステージが『最終ステージ』ならチェックを入れる！")]
    public bool isNextStageFinal = false;
    [Tooltip("最終ステージに挑むために必要な星の合計数")]
    public int requiredStarsForNextStage = 5;
    public Button nextStageButton; // 「つぎへ」ボタン

    [Header("星の獲得演出（任意）")]
    public GameObject starAchievedIcon;

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip popInSE;
    public AudioClip hoverSE;
    public AudioClip buttonClickSE;
    public AudioClip deniedSE; // ★追加：星が足りない時の警告音

    private bool isTransitioning = false;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (clearPanel != null) clearPanel.SetActive(false);
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (popupWindow == null && clearPanel != null) popupWindow = clearPanel.GetComponent<RectTransform>();

        RegisterAllButtonsHover();
    }

    void RegisterAllButtonsHover()
    {
        if (clearPanel == null) return;
        Button[] allButtons = clearPanel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
            trigger.triggers.RemoveAll(entry => entry.eventID == EventTriggerType.PointerEnter);
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((data) => { PlayHoverSound(btn.gameObject.name); });
            trigger.triggers.Add(entry);
        }
    }

    void PlayHoverSound(string btnName)
    {
        if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE);
    }

    public void ShowClearPopup()
    {
        isTransitioning = false;
        if (clearPanel != null) clearPanel.SetActive(true);

        if (starAchievedIcon != null && GameManager.instance != null)
        {
            starAchievedIcon.SetActive(GameManager.instance.hasCollectedStarInThisRun);
        }

        RegisterAllButtonsHover();

        StopAllCoroutines();
        StartCoroutine(PopInRoutine());
    }

    IEnumerator PopInRoutine()
    {
        if (popupWindow == null) yield break;
        if (audioSource != null && popInSE != null) audioSource.PlayOneShot(popInSE);

        popupWindow.localScale = Vector3.zero;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = t - 1f;
            float scale = t * t * ((1.70158f + 1f) * t + 1.70158f) + 1f;
            popupWindow.localScale = Vector3.one * Mathf.Clamp(scale, 0f, 1.15f);
            yield return null;
        }

        popupWindow.localScale = Vector3.one;
    }

    IEnumerator SnappyPopOutRoutine()
    {
        if (popupWindow == null)
        {
            if (clearPanel != null) clearPanel.SetActive(false);
            yield break;
        }

        float duration = 0.08f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float currentScale = Mathf.Lerp(1.0f, 0f, t * t);
            popupWindow.localScale = new Vector3(currentScale * 1.05f, currentScale * 0.9f, 1f);
            yield return null;
        }

        popupWindow.localScale = Vector3.zero;
        if (clearPanel != null) clearPanel.SetActive(false);
    }

    // --- ボタン1：次のステージへ進む ---
    public void OnNextStageClicked()
    {
        if (isTransitioning) return;

        // ★【最重要ガード！】次が最終ステージなのに、星の数が足りない場合
        if (isNextStageFinal)
        {
            int currentTotalStars = GetCurrentTotalStars();

            if (currentTotalStars < requiredStarsForNextStage)
            {
                // 警告音を鳴らす
                if (audioSource != null && deniedSE != null) audioSource.PlayOneShot(deniedSE);

                int remaining = requiredStarsForNextStage - currentTotalStars;

                // メッセージを画面に出して進ませない！
                if (TutorialMessageUI.instance != null)
                {
                    TutorialMessageUI.instance.ShowMessage($"最終ステージの解放には\n星があと {remaining} 個必要だよ！\nセレクト画面に戻って集めよう！");
                }

                Debug.Log($"<color=red>【入場拒否】星が足りません！ 現在: {currentTotalStars}個 / 必要: {requiredStarsForNextStage}個</color>");
                return; // ★ここで完全に阻止！
            }
        }

        isTransitioning = true;
        PlayClickSE();
        StartCoroutine(NextStageSequence());
    }

    IEnumerator NextStageSequence()
    {
        yield return StartCoroutine(SnappyPopOutRoutine());

        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);

        if (isFinalStage) SceneManager.LoadScene(endingSceneName);
        else SceneLoader.Load(nextStageSceneName, nextStageGimmick);
    }

    // --- ボタン2：ステージセレクトへ戻る ---
    public void OnReturnToSelectClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        PlayClickSE();
        StartCoroutine(ReturnToSelectSequence());
    }

    IEnumerator ReturnToSelectSequence()
    {
        yield return StartCoroutine(SnappyPopOutRoutine());

        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);

        SceneLoader.Load("StageSelect", GimmickType.Generic);
    }

    // 現在の合計スター数を正確に計算
    int GetCurrentTotalStars()
    {
        int count = 0;
        for (int i = 1; i <= 30; i++)
        {
            if (PlayerPrefs.GetInt("StarCollected_Stage_" + i, 0) == 1) count++;
        }
        return count;
    }

    void PlayClickSE()
    {
        if (audioSource != null && buttonClickSE != null) audioSource.PlayOneShot(buttonClickSE);
    }
}