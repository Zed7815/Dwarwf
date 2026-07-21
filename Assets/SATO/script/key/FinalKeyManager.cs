using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FinalKeyManager : MonoBehaviour
{
    public static FinalKeyManager instance;

    [Header("UI参照")]
    public RectTransform uiKeyA;
    public RectTransform uiKeyB;
    public RectTransform uiCombinedKey;
    public Vector2 mergeCenterPos;

    [Header("演出設定")]
    public float mergeSpeed = 500f;
    public AudioClip mergeSE;

    private bool hasKeyA = false;
    private bool hasKeyB = false;
    public bool isUnlocked { get; private set; } = false;

    // ★追加：初期位置を保存する変数
    private Vector2 initialPosA;
    private Vector2 initialPosB;

    void Awake()
    {
        instance = this;
        // 初期位置を記憶しておく
        if (uiKeyA) initialPosA = uiKeyA.anchoredPosition;
        if (uiKeyB) initialPosB = uiKeyB.anchoredPosition;

        SetAlpha(uiKeyA, 0);
        SetAlpha(uiKeyB, 0);
        SetAlpha(uiCombinedKey, 0);
    }

    public void CollectKey(int id, Vector3 worldPos)
    {
        if (id == 0) { hasKeyA = true; SetAlpha(uiKeyA, 1f); }
        else { hasKeyB = true; SetAlpha(uiKeyB, 1f); }

        if (hasKeyA && hasKeyB && !isUnlocked)
        {
            StartCoroutine(MergeKeyRoutine());
        }
    }

    IEnumerator MergeKeyRoutine()
    {
        float t = 0;
        while (Vector2.Distance(uiKeyA.anchoredPosition, mergeCenterPos) > 1f)
        {
            uiKeyA.anchoredPosition = Vector2.MoveTowards(uiKeyA.anchoredPosition, mergeCenterPos, mergeSpeed * Time.deltaTime);
            uiKeyB.anchoredPosition = Vector2.MoveTowards(uiKeyB.anchoredPosition, mergeCenterPos, mergeSpeed * Time.deltaTime);
            yield return null;
        }

        SetAlpha(uiKeyA, 0);
        SetAlpha(uiKeyB, 0);
        SetAlpha(uiCombinedKey, 1f);

        if (mergeSE) GetComponent<AudioSource>().PlayOneShot(mergeSE);

        uiCombinedKey.localScale = Vector3.one * 1.5f;
        while (uiCombinedKey.localScale.x > 1.0f)
        {
            uiCombinedKey.localScale -= Vector3.one * Time.deltaTime * 2f;
            yield return null;
        }
        uiCombinedKey.localScale = Vector3.one;

        isUnlocked = true;
    }

    void SetAlpha(RectTransform rt, float alpha)
    {
        if (rt == null) return;
        Image img = rt.GetComponent<Image>();
        if (img)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    // リセット命令
    public void OnGimmickReset()
    {
        StopAllCoroutines();

        hasKeyA = false;
        hasKeyB = false;
        isUnlocked = false;

        // ★座標を初期位置に戻す（これがないと2回目におかしくなる）
        if (uiKeyA) uiKeyA.anchoredPosition = initialPosA;
        if (uiKeyB) uiKeyB.anchoredPosition = initialPosB;

        SetAlpha(uiKeyA, 0f);
        SetAlpha(uiKeyB, 0f);
        SetAlpha(uiCombinedKey, 0f);
    }
}