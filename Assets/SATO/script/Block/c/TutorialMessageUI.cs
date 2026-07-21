using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialMessageUI : MonoBehaviour
{
    public static TutorialMessageUI instance;
    public TextMeshProUGUI messageText;
    public CanvasGroup canvasGroup;

    void Awake() => instance = this;

    void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0;
    }

    public void ShowMessage(string text)
    {
        StopAllCoroutines();
        StartCoroutine(MessageRoutine(text));
    }

    IEnumerator MessageRoutine(string text)
    {
        messageText.text = text;

        // ふわっと表示
        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / 0.5f;
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        // ふわっと消える
        while (t > 0)
        {
            t -= Time.deltaTime;
            canvasGroup.alpha = t / 0.5f;
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}