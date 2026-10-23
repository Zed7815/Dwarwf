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
    private bool wasFastForwarding = false; // 倍速中フラグ

    void Awake()
    {
        instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    public void ShowMessage(string text)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(MessageRoutine(text));
    }

    IEnumerator MessageRoutine(string text)
    {
        // ★【確実な判定】Time.timeScaleではなく、倍速ボタン・マネージャーがONか直接確認する！
        wasFastForwarding = CheckIsFastForwarding();

        messageText.text = text;

        // 1. ふわっと表示
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = t / 0.3f;
            yield return null;
        }
        canvasGroup.alpha = 1;

        // 2. プレイヤーのクリック待ち
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

        // 4. 復元：倍速中だったなら確実に倍速を適用、そうでなければ通常に戻す
        if (wasFastForwarding)
        {
            // 倍速ボタンが存在すれば、その設定スピードを取得（なければ3.0f）
            float fastSpeed = 1.0f;
            FastForwardUI ff = FindObjectOfType<FastForwardUI>();
            if (ff != null) fastSpeed = ff.fastForwardSpeed;

            Time.timeScale = fastSpeed;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            Debug.Log($"<color=cyan>【チュートリアル終了】倍速（{fastSpeed}倍）を確実に復元しました！</color>");
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            Debug.Log("<color=green>【チュートリアル終了】通常速度（1.0倍）に戻しました</color>");
        }
    }

    // ★倍速中かどうかを複数の方法で確実に調べる関数
    bool CheckIsFastForwarding()
    {
        if (Time.timeScale > 1.05f) return true;

        FastForwardUI ff = FindObjectOfType<FastForwardUI>();
        if (ff != null && ff.isFastForwarding)
        {
            return true; // ボタンがONになっていれば倍速中と判定！
        }

        return false;
    }
}