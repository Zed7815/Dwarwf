using UnityEngine;
using System.Collections;

public class EditUIController : MonoBehaviour
{
    [Header("参照設定")]
    public RectTransform topPanel;
    public GameObject resetButton;

    [Header("アニメーション設定")]
    public float slideDuration = 0.4f; // 移動にかかる時間
    public float dipAmount = 30f;      // 隠れる時の沈み込み量
    public float hidePositionY = 300f; // 画面外（上）の座標

    private Vector2 initialPos;
    private GameObject[] dropFrames;

    void Start()
    {
        if (topPanel != null) initialPos = topPanel.anchoredPosition;
        dropFrames = GameObject.FindGameObjectsWithTag("DropFrame");
    }

    // ゲーム開始時：UIを引っ込める
    public void HideEditUI()
    {
        StopAllCoroutines();
        StartCoroutine(HideRoutine());
    }

    // リセット時：UIを戻す
    public void ShowEditUI()
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine());
    }

    // --- 消える時の動き (沈んで→飛び出す) ---
    IEnumerator HideRoutine()
    {
        if (topPanel == null) yield break;

        float elapsed = 0;
        float dipDuration = 0.15f;
        Vector2 dipPos = initialPos + new Vector2(0, -dipAmount);

        // 1. 少し下に沈む
        while (elapsed < dipDuration)
        {
            elapsed += Time.deltaTime;
            topPanel.anchoredPosition = Vector2.Lerp(initialPos, dipPos, elapsed / dipDuration);
            yield return null;
        }

        // 2. 一気に上に飛び出す
        elapsed = 0;
        Vector2 targetPos = new Vector2(initialPos.x, hidePositionY);
        SetDropFramesActive(false); // 赤い枠を非表示

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            // BackOutイージング
            t = t * t * ((1.70158f + 1) * t - 1.70158f) + 1;

            topPanel.anchoredPosition = Vector2.Lerp(dipPos, targetPos, t);
            yield return null;
        }
        topPanel.anchoredPosition = targetPos;
    }

    // --- 戻る時の動き (上から降下→少し弾んで着地) ---
    IEnumerator ShowRoutine()
    {
        if (topPanel == null) yield break;

        float elapsed = 0;
        Vector2 startPos = topPanel.anchoredPosition; // 現在の隠れている位置
        SetDropFramesActive(true); // 赤い枠を表示

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;

            // 降りてくる時の演出（最後少しだけ行き過ぎて戻る弾力感）
            // 数式：1 - cos(t * pi/2) 的な滑らかな着地
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            topPanel.anchoredPosition = Vector2.Lerp(startPos, initialPos, t);
            yield return null;
        }
        topPanel.anchoredPosition = initialPos;
    }

    void SetDropFramesActive(bool active)
    {
        if (dropFrames == null || dropFrames.Length == 0)
            dropFrames = GameObject.FindGameObjectsWithTag("DropFrame");

        foreach (GameObject frame in dropFrames)
        {
            if (frame != null) frame.SetActive(active);
        }
    }
}