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
    }

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isProcessing) return;

        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();

            if (p != null)
            {
                // ★【修正ポイント1】触れた瞬間に、他のギミックの処理（ジャンプなど）を全て殺す！
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

        p.StateChange(0); // 停止

        yield return new WaitForSeconds(0.2f);

        // --- ② 【反転フェーズ】 ---
        if (audioSource != null && reverseSE != null) audioSource.PlayOneShot(reverseSE);

        string targetTrigger = enteredFromRight ? rightEntryTrigger : leftEntryTrigger;

        if (anim != null)
        {
            anim.SetTrigger(targetTrigger); // トリガーを引く
        }

        SpriteRenderer playerSr = p.GetComponent<SpriteRenderer>();
        if (playerSr != null) playerSr.enabled = false;

        yield return new WaitForSeconds(0.4f);

        // ★【追加】トリガーをリセットして、何度も再生されないようにする
        if (anim != null)
        {
            anim.ResetTrigger(rightEntryTrigger);
            anim.ResetTrigger(leftEntryTrigger);
        }

        // 向き反転
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

    void OnGimmickReset() { isProcessing = false; }
}