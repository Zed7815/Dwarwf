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
        // （メッセージを出した瞬間のクリックが誤爆して即消えるのを防ぐための1フレーム待ち）
        yield return null;
        isWaitingForClick = true;

        while (isWaitingForClick)
        {
            // 左クリック、または画面タップされたか
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

        // 4. ★最重要：メッセージが閉じたら、ゲームの時間を確実に通常（1.0）に戻す！
        Time.timeScale = 1.0f;
    }
}