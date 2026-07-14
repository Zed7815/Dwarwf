using UnityEngine;
using System.Collections;

public class ReverseGimmick : MonoBehaviour
{
    private bool isProcessing = false;
    private Animator anim;

    [Header("アニメーション設定")]
    public string rightEntryTrigger = "ReverseFromRight";
    public string leftEntryTrigger = "ReverseFromLeft";

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip reverseSE;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // ★【ここが解決策！】
        // GameManagerのリセットボタンが押された時に、このオブジェクトも通知を受け取るようにする
        // ※GameManager側に通知機能がない場合は、後述の「OnGimmickReset」で直接対応します
    }

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isProcessing) return;

        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();
            if (p != null)
            {
                p.ForceStopAbilities();
                bool enteredFromRight = trigger.transform.position.x > transform.position.x;
                StartCoroutine(ReverseSequence(p, enteredFromRight));
            }
        }
    }

    IEnumerator ReverseSequence(Player_walk p, bool enteredFromRight)
    {
        isProcessing = true;

        // --- ① 【待機フェーズ】 ---
        float centerX = transform.position.x;
        p.transform.position = new Vector3(centerX, p.transform.position.y, p.transform.position.z);
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        p.StateChange(0);
        yield return new WaitForSeconds(0.2f);

        // --- ② 【反転フェーズ】 ---
        if (audioSource != null && reverseSE != null) audioSource.PlayOneShot(reverseSE);
        string targetTrigger = enteredFromRight ? rightEntryTrigger : leftEntryTrigger;
        if (anim != null) anim.SetTrigger(targetTrigger);

        SpriteRenderer playerSr = p.GetComponent<SpriteRenderer>();
        if (playerSr != null) playerSr.enabled = false;

        yield return new WaitForSeconds(0.4f);
        if (anim != null)
        {
            anim.ResetTrigger(rightEntryTrigger);
            anim.ResetTrigger(leftEntryTrigger);
        }

        p.direction *= -1;
        Vector3 s = p.transform.localScale;
        s.x = Mathf.Abs(s.x) * p.direction;
        p.transform.localScale = s;

        // --- ③ 【待機フェーズ】 ---
        yield return new WaitForSeconds(0.4f);
        if (playerSr != null) playerSr.enabled = true;

        // --- ④ 通常歩行再開 ---
        p.StateChange(1);
        yield return new WaitForSeconds(0.5f);
        isProcessing = false;
    }

    // ★リセットボタンから呼ばれる大掃除処理
    // ★リセットボタンから呼ばれる処理
    void OnGimmickReset()
    {
        // 1. 実行中のコルーチン（反転演出）を強制停止
        StopAllCoroutines();

        // 2. フラグを初期化して、再び触れれば反応するようにする
        isProcessing = false;

        // 3. アニメーターの「大掃除」
        if (anim != null)
        {
            // 溜まっているトリガーをすべて消去
            anim.ResetTrigger(rightEntryTrigger);
            anim.ResetTrigger(leftEntryTrigger);

            // ★重要：アニメーションを強制的に「Idle」ステートに戻す
            // 第2引数の 0 はレイヤー番号、第3引数の 0f はアニメーションの最初から再生することを意味します
            anim.Play("hebiAnimator", 0, 0f);

            // 変更を即座に反映
            anim.Update(0f);
        }

        // 4. 音が鳴っていたら止める
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 5. プレイヤーがもし透明なままなら戻す処理（安全策）
        // ※本来はプレイヤー側のResetで直りますが、ギミック側でも管理しているため
    }
}