using UnityEngine;
using System.Collections;

public class ReverseGimmick : MonoBehaviour
{
    private bool isProcessing = false;
    private Animator anim;

    [Header("アニメーション設定")]
    [Tooltip("右側から進入した際のアニメーショントリガー名")]
    public string rightEntryTrigger = "ReverseFromRight";
    [Tooltip("左側から進入した際のアニメーショントリガー名")]
    public string leftEntryTrigger = "ReverseFromLeft";

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターで割り当てるか自動取得
    public AudioClip reverseSE;    // 反転アクション時の音

    void Start()
    {
        anim = GetComponent<Animator>();
        // AudioSourceが未設定なら自分から取得
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

        while (Mathf.Abs(p.transform.position.x - centerX) > 0.2f)
        {
            yield return null;
        }

        p.transform.position = new Vector3(centerX, p.transform.position.y, p.transform.position.z);

        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        p.StateChange(0);

        yield return new WaitForSeconds(0.2f);

        // --- ② 【反転フェーズ】 ---

        // ★SE再生
        if (audioSource != null && reverseSE != null)
        {
            audioSource.PlayOneShot(reverseSE);
        }

        if (anim != null)
        {
            if (enteredFromRight) anim.SetTrigger(rightEntryTrigger);
            else anim.SetTrigger(leftEntryTrigger);
        }

        SpriteRenderer playerSr = p.GetComponent<SpriteRenderer>();
        if (playerSr != null) playerSr.enabled = false;

        yield return new WaitForSeconds(0.4f);

        if (anim != null)
        {
            if (enteredFromRight) anim.ResetTrigger(rightEntryTrigger);
            else anim.ResetTrigger(leftEntryTrigger);
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