using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using Unity.VisualScripting;

public class ReverseGimmick : MonoBehaviour
{
    private bool isProcessing = false;
    private Animator anim;

    [Header("アニメーション設定")]
    [Tooltip("右側から進入した際のアニメーショントリガー名")]
    public string rightEntryTrigger = "ReverseFromRight";
    [Tooltip("左側から進入した際のアニメーショントリガー名")]
    public string leftEntryTrigger = "ReverseFromLeft";


    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isProcessing) return;

        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();

            if (p != null)
            {
                // 進入方向の判定
                // プレイヤーの X 座標がギミック自身の X 座標より右側にあれば「右から進入」とみなす
                bool enteredFromRight = trigger.transform.position.x > transform.position.x;
                StartCoroutine(ReverseSequence(p, enteredFromRight));
            }
        }
    }

    IEnumerator ReverseSequence(Player_walk p, bool enteredFromRight)
    {
        isProcessing = true;

        // --- ① 【待機フェーズ】 プレイヤーをピタッと中心に合わせて完全停止させる ---
        float centerX = transform.position.x;

        // 中心に十分近づくまで吸い寄せ
        while (Mathf.Abs(p.transform.position.x - centerX) > 0.2f)
        {
            yield return null;
        }

        // ぴったり中心に補正
        p.transform.position = new Vector3(centerX, p.transform.position.y, p.transform.position.z);

        // プレイヤーの物理速度を完全にゼロにして「待機（idol）」状態へ
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        p.StateChange(0); // プレイヤーを一時的に待機(idol)状態にする

    

        // ピタッと静止してためる「待機」時間。
        // これにより、これから始まる反転アニメのメリハリが格段に増します。
        yield return new WaitForSeconds(0.2f);

        // --- ② 【反転フェーズ】 進入方向に応じた回転アニメ（右回り/左回り）をトリガー ---
        if (anim != null)
        {
            if (enteredFromRight)
            {
                anim.SetTrigger(rightEntryTrigger);
            }
            else
            {
                anim.SetTrigger(leftEntryTrigger);
            }
        }

        // 🌟 演出：反転アクション開始に合わせて、自機スプライト（SpriteRenderer）を一時的に非表示（消灯）にする
        SpriteRenderer playerSr = p.GetComponent<SpriteRenderer>();
        if (playerSr != null) playerSr.enabled = false;

        // アニメーションが最も美しく回転するタイミング（0.4秒後）に、プレイヤーの向きを反転！
        yield return new WaitForSeconds(0.4f);

        // 【安全リセット】通常のTrigger方式の暴発を防ぐため、再生開始後にトリガーを明示的にクリアします
        if (anim != null)
        {
            if (enteredFromRight)
            {
                anim.ResetTrigger(rightEntryTrigger);
            }
            else
            {
                anim.ResetTrigger(leftEntryTrigger);
            }
        }

        // プレイヤーの物理進行方向（direction）と画像の向きをカチッと反転
        p.direction *= -1;
        Vector3 s = p.transform.localScale;
        s.x = Mathf.Abs(s.x) * p.direction; // directionに基づいた絶対的な向き設定
        p.transform.localScale = s;

        // --- ③ 【待機フェーズ】 回転アニメが流れた後、再びピタッと静止して待機 ---
        // アニメーションが完全に1周して、ギミック自身がIdle（待機）ステートに自動で戻るのを待ちます。
        yield return new WaitForSeconds(0.4f);

        // 🌟 演出：移動を再開する直前に、反転が完了したプレイヤー（自機）を再び表示（再点灯）させる！
        if (playerSr != null) playerSr.enabled = true;

        // --- ④ 通常歩行（直進）へのスムーズな再開 ---
        p.StateChange(1); // プレイヤーを直進歩行（straight）状態に戻して出発！

        // 連続で反転が暴発するのを防ぐためのインターバル
        yield return new WaitForSeconds(0.5f);
        isProcessing = false;
    }
}