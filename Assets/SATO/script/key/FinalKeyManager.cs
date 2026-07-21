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
    public bool isUnlocked { get; private set; } = false; // これを監視する

    private Vector2 initialPosA;
    private Vector2 initialPosB;

    void Awake()
    {
        instance = this;
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
        while (uiKeyA != null && Vector2.Distance(uiKeyA.anchoredPosition, mergeCenterPos) > 1f)
        {
            uiKeyA.anchoredPosition = Vector2.MoveTowards(uiKeyA.anchoredPosition, mergeCenterPos, mergeSpeed * Time.deltaTime);
            uiKeyB.anchoredPosition = Vector2.MoveTowards(uiKeyB.anchoredPosition, mergeCenterPos, mergeSpeed * Time.deltaTime);
            yield return null;
        }

        SetAlpha(uiKeyA, 0);
        SetAlpha(uiKeyB, 0);
        SetAlpha(uiCombinedKey, 1f);

        if (mergeSE) GetComponent<AudioSource>().PlayOneShot(mergeSE);

        isUnlocked = true; // ここでアンロック
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

    // ★リセットボタンで呼ばれる
    public void OnGimmickReset()
    {
        StopAllCoroutines();

        // 1. 全てのフラグを折る
        isUnlocked = false;
        hasKeyA = false;
        hasKeyB = false;

        // 2. UI位置を戻す
        if (uiKeyA) uiKeyA.anchoredPosition = initialPosA;
        if (uiKeyB) uiKeyB.anchoredPosition = initialPosB;

        // 3. UIを消す
        SetAlpha(uiKeyA, 0f);
        SetAlpha(uiKeyB, 0f);
        SetAlpha(uiCombinedKey, 0f);
    }
}