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

    [Header("★最終ステージ前の制限設定")]
    [Tooltip("次のステージが『最終ステージ』ならチェックを入れる")]
    public bool isNextStageFinal = false;
    [Tooltip("最終ステージに挑むために必要な星の合計数（例: 10 など）")]
    public int requiredStarsForNextStage = 10;
    public Button nextStageButton;

    [Header("星の獲得演出")]
    public GameObject starAchievedIcon;

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip popInSE;
    public AudioClip hoverSE;
    public AudioClip buttonClickSE;
    public AudioClip deniedSE;

    [Header("別Canvasの遮断設定")]
    public Canvas hudCanvas;

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

        if (hudCanvas != null)
        {
            var raycaster = hudCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = false;
        }

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

        if (hudCanvas != null)
        {
            var raycaster = hudCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = true;
        }
    }

    public void OnNextStageClicked()
    {
        if (isTransitioning) return;

        // ★最終ステージ制限の判定
        if (isNextStageFinal)
        {
            int currentTotalStars = StageSelectNest.GetCurrentTotalStars();

            // ★診断ログ：何個持っていて何個必要なのかをコンソールに完全表示
            Debug.Log($"<color=cyan>【次ステージ判定】現在星: {currentTotalStars}個 / 必要星: {requiredStarsForNextStage}個</color>");

            if (currentTotalStars < requiredStarsForNextStage)
            {
                if (audioSource != null && deniedSE != null) audioSource.PlayOneShot(deniedSE);

                int remaining = Mathf.Max(0, requiredStarsForNextStage - currentTotalStars);

                if (TutorialMessageUI.instance != null)
                {
                    TutorialMessageUI.instance.ShowMessage($"最終ステージの解放には\n星があと {remaining} 個必要だよ！\nセレクト画面に戻って集めよう！");
                }

                Debug.Log($"<color=red>【入場拒否】星が足りません！ 現在: {currentTotalStars}個 / 必要: {requiredStarsForNextStage}個 (不足: {remaining}個)</color>");
                return;
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

    void PlayClickSE()
    {
        if (audioSource != null && buttonClickSE != null) audioSource.PlayOneShot(buttonClickSE);
    }
}