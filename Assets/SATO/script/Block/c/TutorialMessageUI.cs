using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class TutorialMessageUI : MonoBehaviour
{
    public static TutorialMessageUI instance;
    public TextMeshProUGUI messageText;
    public CanvasGroup canvasGroup;

    private bool isWaitingForClick = false;
    private bool wasFastForwarding = false; // ★追加：倍速中だったかを記憶するフラグ

    void Awake()
    {
        instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        gameObject.SetActive(false); // 最初は非表示
    }

    public void ShowMessage(string text)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(MessageRoutine(text));
    }

    IEnumerator MessageRoutine(string text)
    {
        // ★追加：メッセージを表示する「直前」に、今倍速中かどうかを記録！
        // （Time.timeScaleが1.0より大きい、またはFastForwardUIが倍速中ならtrue）
        wasFastForwarding = (Time.timeScale > 1.05f);

        messageText.text = text;

        // 1. ふわっと表示（一時停止中でも動くように unscaledDeltaTime を使用）
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = t / 0.3f;
            yield return null;
        }
        canvasGroup.alpha = 1;

        // 2. プレイヤーのクリック（タップ）を待つ猶予
        yield return null;
        isWaitingForClick = true;

        while (isWaitingForClick)
        {
            bool clicked = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID
            if (Input.GetMouseButtonDown(0))
            {
                clicked = true;
            }
#endif

            if (clicked)
            {
                isWaitingForClick = false;
            }

            yield return null;
        }

        // 3. ふわっと消える
        t = 0;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - (t / 0.3f);
            yield return null;
        }
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);

        // ★修正：メッセージが閉じた後、元の速度に合わせて復元する！
        if (wasFastForwarding)
        {
            // 倍速中だった場合：3倍速（および安定した物理時間）を復元
            Time.timeScale = 3.0f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            Debug.Log("<color=cyan>【チュートリアル終了】倍速状態（3.0倍）を復元しました</color>");
        }
        else
        {
            // 倍速中ではなかった場合：通常通り等倍（1.0）に戻す
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            Debug.Log("<color=green>【チュートリアル終了】通常速度（1.0倍）に戻しました</color>");
        }
    }
}