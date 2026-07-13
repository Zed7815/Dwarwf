using UnityEngine;
using System.Collections;

public class PlayerJumpBlock : MonoBehaviour
{
    [Header("判定設定")]
    public float topCheckTolerance = 0.1f;

    [Header("アニメーション設定")]
    [Tooltip("ジャンプ台が跳ねる瞬間に再生する Animator のトリガー名")]
    public string jumpTriggerName = "OnJump";

    [Header("自動復元設定 (100%確実に待機➔ジャンプ➔待機する安全設計)")]
    [Tooltip("TrueにするとAnimatorControllerの遷移(矢印)を使わず、C#から強制的に待機・ジャンプ状態を直接切り替えます（推奨）")]
    public bool useDirectPlayMethod = true;
    public string jumpStateName = "Jump";
    public string idleStateName = "Idle";

    [Tooltip("ジャンプアニメーションが再生されてから、自動で待機（Idle）に戻るまでの時間")]
    public float jumpAnimationDuration = 0.5f;

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターで割り当てるか自動取得
    public AudioClip jumpSE;       // 跳ねる時の音

    private Animator anim;
    private Coroutine activeAnimRoutine;

    private void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponentInParent<Animator>();

        // AudioSourceが未設定なら自分から取得
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckAndJump(collision);
    }

    // 追加：乗った瞬間に判定が漏れても、触れている間はジャンプを試みる
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckAndJump(collision);
    }

    private void CheckAndJump(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player_walk player = collision.gameObject.GetComponent<Player_walk>();

            // 既にジャンプ中(IsJumping)でないか確認
            if (player != null && !player.IsJumping() && player.gameObject.activeInHierarchy && IsPlayerOnTop(collision.collider))
            {
                player.StartCoroutine(player.Jump(transform));
            }
        }
    }

    bool IsPlayerOnTop(Collider2D playerCollider)
    {
        Collider2D blockCollider = GetComponent<Collider2D>();
        if (playerCollider == null || blockCollider == null) return false;

        float playerBottom = playerCollider.bounds.min.y;
        float blockTop = blockCollider.bounds.max.y;

        return playerBottom >= blockTop - topCheckTolerance;
    }

    /// <summary>
    /// プレイヤーを弾き飛ばす瞬間に、ジャンプ台自身のアニメーションと音を再生
    /// </summary>
    public void TriggerJumpAnimation()
    {
        // ★SE再生
        if (audioSource != null && jumpSE != null)
        {
            audioSource.PlayOneShot(jumpSE);
        }

        if (anim == null) return;

        if (activeAnimRoutine != null)
        {
            StopCoroutine(activeAnimRoutine);
        }
        activeAnimRoutine = StartCoroutine(PlayJumpSequenceRoutine());
    }

    private IEnumerator PlayJumpSequenceRoutine()
    {
        if (useDirectPlayMethod)
        {
            anim.Play(jumpStateName, 0, 0f);
        }
        else
        {
            if (!string.IsNullOrEmpty(jumpTriggerName))
            {
                anim.SetTrigger(jumpTriggerName);
            }
        }

        yield return new WaitForSeconds(jumpAnimationDuration);

        if (useDirectPlayMethod)
        {
            anim.Play(idleStateName, 0, 0f);
        }
        else
        {
            anim.Play(idleStateName, 0, 0f);
        }
        activeAnimRoutine = null;
    }
}