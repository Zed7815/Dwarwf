using UnityEngine;
using System.Collections.Generic;

public class GimmickResetter : MonoBehaviour
{
    // 消えているオブジェクトも見つけられるように名簿（リスト）を作成
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

    private void OnDestroy() => allResetters.Remove(this);

    // シーン切り替え時に名簿を空にする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ClearList() => allResetters.Clear();

    public void ResetGimmick()
    {
        // 1. まず出現させる（非表示だとプログラムが動かないため）
        gameObject.SetActive(true);

        // 2. このオブジェクトに付いている「すべてのスクリプト」の動きを強制停止
        // これで鳥やリフトがプレイヤーを運ぶ処理がその場で爆破されます
        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            script.StopAllCoroutines();
        }

        // 3. 各ギミック特有のフラグ（isMovingなど）を初期化
        gameObject.SendMessage("OnGimmickReset", SendMessageOptions.DontRequireReceiver);

        // 4. 位置・回転・スケールを初期状態に完全に巻き戻す
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        transform.localScale = initialScale;

        // 5. コンポーネントの状態を復帰
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // 6. アニメーターがあれば最初から再生
        var anim = GetComponent<Animator>();
        if (anim != null) { anim.Rebind(); anim.Update(0f); }

        // 7. 本来の状態（消える足場なら非表示など）に戻す
        gameObject.SetActive(initialActiveState);
    }
}