using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FinalKeyManager : MonoBehaviour
{
    public static FinalKeyManager instance;

    [Header("UI参照 (Canvas内のイメージ)")]
    public RectTransform uiKeyA;
    public RectTransform uiKeyB;
    public RectTransform uiCombinedKey;
    public Vector2 mergeCenterPos; // 合体する中心座標（UI上の座標）

    [Header("演出設定")]
    public float mergeSpeed = 500f;
    public AudioClip mergeSE;

    private bool hasKeyA = false;
    private bool hasKeyB = false;
    public bool isUnlocked { get; private set; } = false;

    void Awake()
    {
        instance = this;
        // 初期状態：UIはすべて非表示か半透明
        SetAlpha(uiKeyA, 0);
        SetAlpha(uiKeyB, 0);
        SetAlpha(uiCombinedKey, 0);
    }

    public void CollectKey(int id, Vector3 worldPos)
    {
        if (id == 0) { hasKeyA = true; SetAlpha(uiKeyA, 1f); }
        else { hasKeyB = true; SetAlpha(uiKeyB, 1f); }

        // 両方揃ったら合体演出開始
        if (hasKeyA && hasKeyB && !isUnlocked)
        {
            StartCoroutine(MergeKeyRoutine());
        }
    }

    IEnumerator MergeKeyRoutine()
    {
        // 1. 鍵AとBが中心に向かって移動する
        float t = 0;
        while (Vector2.Distance(uiKeyA.anchoredPosition, mergeCenterPos) > 1f)
        {
            uiKeyA.anchoredPosition = Vector2.MoveTowards(uiKeyA.anchoredPosition, mergeCenterPos, mergeSpeed * Time.deltaTime);
            uiKeyB.anchoredPosition = Vector2.MoveTowards(uiKeyB.anchoredPosition, mergeCenterPos, mergeSpeed * Time.deltaTime);
            yield return null;
        }

        // 2. AとBを消して、合体後の鍵を表示
        SetAlpha(uiKeyA, 0);
        SetAlpha(uiKeyB, 0);
        SetAlpha(uiCombinedKey, 1f);

        if (mergeSE) GetComponent<AudioSource>().PlayOneShot(mergeSE);

        // 3. 少し大きくして強調する（演出）
        uiCombinedKey.localScale = Vector3.one * 1.5f;
        while (uiCombinedKey.localScale.x > 1.0f)
        {
            uiCombinedKey.localScale -= Vector3.one * Time.deltaTime * 2f;
            yield return null;
        }
        uiCombinedKey.localScale = Vector3.one;

        // 4. アンロック完了
        isUnlocked = true;
        Debug.Log("すべての鍵が揃いました！鎖を解放します。");
    }

    void SetAlpha(RectTransform rt, float alpha)
    {
        Image img = rt.GetComponent<Image>();
        if (img)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }



    // FinalKeyManager.cs

    public void OnGimmickReset()
    {
        // 1. 演出を止める
        StopAllCoroutines();

        // 2. ★フラグを確実にリセット
        hasKeyA = false;
        hasKeyB = false;
        isUnlocked = false; // これが重要！

        // 3. UIを消す
        SetAlpha(uiKeyA, 0f);
        SetAlpha(uiKeyB, 0f);
        SetAlpha(uiCombinedKey, 0f);

        // 4. UIの位置を初期位置に戻したい場合は、Startで初期位置を保存してここで戻してください
    }
}