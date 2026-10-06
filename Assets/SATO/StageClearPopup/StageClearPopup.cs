using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // ホバー検知のために追加
using System.Collections;

public class StageClearPopup : MonoBehaviour
{
    public static StageClearPopup instance;

    [Header("ポップアップ本体")]
    public GameObject clearPanel;
    [Tooltip("ポヨンと動かすウィンドウ枠")]
    public RectTransform popupWindow;

    [Header("ボタン参照（ホバー音用）")]
    public Button nextStageButton;     // 「つぎへ」ボタン
    public Button returnToSelectButton; // 「セレクトへ」ボタン

    [Header("次ステージ設定")]
    public string nextStageSceneName;
    public GimmickType nextStageGimmick = GimmickType.Generic;
    public bool isFinalStage = false;
    public string endingSceneName = "Ending";

    [Header("星の獲得演出（任意）")]
    public GameObject starAchievedIcon;

    [Header("演出参照")]
    public nextscene fadeOutScript;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip popInSE;       // 出現音
    public AudioClip hoverSE;       // ★追加：カーソルを合わせた時の音
    public AudioClip buttonClickSE; // クリック音

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

        // ★ボタンに自動でホバー音を登録
        RegisterHoverSound(nextStageButton);
        RegisterHoverSound(returnToSelectButton);
    }

    // ホバー音（カーソルが乗った時の音）の自動登録
    void RegisterHoverSound(Button btn)
    {
        if (btn == null) return;
        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>() ?? btn.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener((data) => {
            if (audioSource != null && hoverSE != null) audioSource.PlayOneShot(hoverSE);
        });
        trigger.triggers.Add(entry);
    }

    public void ShowClearPopup()
    {
        isTransitioning = false;
        if (clearPanel != null) clearPanel.SetActive(true);

        if (starAchievedIcon != null && GameManager.instance != null)
        {
            starAchievedIcon.SetActive(GameManager.instance.hasCollectedStarInThisRun);
        }

        StopAllCoroutines();
        StartCoroutine(PopInRoutine());
    }

    // --- 「ポヨン」と出現する演出 ---
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

            // BackOutイージング（弾む演出）
            t = t - 1f;
            float scale = t * t * ((1.70158f + 1f) * t + 1.70158f) + 1f;

            popupWindow.localScale = Vector3.one * Mathf.Clamp(scale, 0f, 1.15f);
            yield return null;
        }

        popupWindow.localScale = Vector3.one;
    }

    // ★【改良】押した瞬間に「シュッ！」と吸い込まれるように自然に消える演出（0.08秒の超速ポップアウト）
    IEnumerator SnappyPopOutRoutine()
    {
        if (popupWindow == null)
        {
            if (clearPanel != null) clearPanel.SetActive(false);
            yield break;
        }

        float duration = 0.08f; // プレイヤーが「待たされた」と感じない瞬速
        float elapsed = 0f;
        Vector3 startScale = popupWindow.localScale;

        // 一瞬だけ少し縦に伸びてから（予備動作）、キュッと0に縮む自然な消え方
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // 加速しながら縮小
            float currentScale = Mathf.Lerp(1.0f, 0f, t * t);
            popupWindow.localScale = new Vector3(currentScale * 1.05f, currentScale * 0.9f, 1f);
            yield return null;
        }

        popupWindow.localScale = Vector3.zero;
        if (clearPanel != null) clearPanel.SetActive(false); // 即座に非表示！
    }

    // --- ボタン1：次のステージへ進む ---
    public void OnNextStageClicked()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        PlayClickSE();
        StartCoroutine(NextStageSequence());
    }

    IEnumerator NextStageSequence()
    {
        // 押した瞬間にシュッと自然に消える
        yield return StartCoroutine(SnappyPopOutRoutine());

        // ウィンドウが消えた後、黒い板が降りてくる
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
        // 押した瞬間にシュッと自然に消える
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