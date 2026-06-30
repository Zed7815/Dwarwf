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
    public bool useDirectPlayMethod = true; //  Trueにしておくのがおすすめです！
    public string jumpStateName = "Jump";
    public string idleStateName = "Idle";

    [Tooltip("ジャンプアニメーションが再生されてから、自動で待機（Idle）に戻るまでの時間")]
    public float jumpAnimationDuration = 0.5f; //  ジャンプ台のアニメ時間に合わせて調整可能

    private Animator anim;
    private Coroutine activeAnimRoutine;

    private void Start()
    {
        
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (anim == null) anim = GetComponentInParent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player_walk player = collision.gameObject.GetComponent<Player_walk>();

            if (player != null && player.gameObject.activeInHierarchy && IsPlayerOnTop(collision.collider))
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
    /// プレイヤーを弾き飛ばす瞬間に、ジャンプ台自身のアニメーションを再生
    /// </summary>
    public void TriggerJumpAnimation()
    {
        if (anim == null) return;

        //  すでに再生中の場合は一度コルーチンを止めて安全に2重再生を防止
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
            //  矢印(Transition)が繋がっていなくても、C#から直接「Jump」ステートを頭から強制再生！
            anim.Play(jumpStateName, 0, 0f);
        }
        else
        {
            // トリガー方式で「待機 ➔ ジャンプ」へ遷移させる
            if (!string.IsNullOrEmpty(jumpTriggerName))
            {
                anim.SetTrigger(jumpTriggerName);
            }
        }
        Debug.Log("[PlayerJumpBlock] ジャンプ台のジャンプアニメーションを開始しました。");

        //  跳ねるアニメーションの再生時間(秒)だけ一時停止
        yield return new WaitForSeconds(jumpAnimationDuration);

        //  再生が終わったら、自動的に「Idle」ステートに強制復元！
        if (useDirectPlayMethod)
        {
            anim.Play(idleStateName, 0, 0f);
        }
        else
        {
            anim.Play(idleStateName, 0, 0f);
        }
        Debug.Log("[PlayerJumpBlock] ジャンプ台が自動的に待機状態(Idle)へ復帰しました。");
        activeAnimRoutine = null;
    }
}