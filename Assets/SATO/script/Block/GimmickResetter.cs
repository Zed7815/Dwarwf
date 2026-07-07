using UnityEngine;
using System.Collections.Generic;

public class GimmickResetter : MonoBehaviour
{
    public static List<GimmickResetter> allResetters = new List<GimmickResetter>();

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private bool initialActiveState;

    void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialScale = transform.localScale;
        initialActiveState = gameObject.activeSelf;

        if (!allResetters.Contains(this)) allResetters.Add(this);
    }

    // シーンが切り替わったときにリストを空にする（エラー防止）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ClearList() => allResetters.Clear();

    private void OnDestroy() => allResetters.Remove(this);

    public void ResetGimmick()
    {
        // 1. まずオブジェクトを出現させる
        gameObject.SetActive(initialActiveState);

        // 2. このオブジェクトに付いている「すべてのスクリプト」のリセット関数を呼ぶ
        // これにより、BirdCarrierやLiftBlock内の StopAllCoroutines() が実行されます
        gameObject.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);

        // 3. このリセッター自身のコルーチンも止める
        StopAllCoroutines();

        // 4. 位置・回転・スケールを戻す
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        // 5. コンポーネントの強制復帰
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}