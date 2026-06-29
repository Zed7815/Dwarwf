using UnityEngine;

public class PlayerJumpBlock : MonoBehaviour
{
    [Header("判定設定")]
    public float topCheckTolerance = 0.1f;

    [Header("アニメーション設定")]
    [Tooltip("ジャンプ台が跳ねる瞬間に再生する Animator のトリガー名")]
    public string jumpTriggerName = "OnJump";

    [Header("安全ダイレクト再生モード")]
    [Tooltip("Trueにすると遷移(矢印)を使わず、C#から強制的に待機・ジャンプ状態を直接切り替えます")]
    public bool useDirectPlayMethod = false;
    public string jumpStateName = "Jump";

    private Animator anim;

    private void Start()
    {
        //  自身、または子オブジェクトから Animator を自動取得
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
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

    //  【新規追加】プレイヤーを弾き飛ばす瞬間に、ジャンプ台自身のアニメーションを再生

    public void TriggerJumpAnimation()
    {
        if (anim == null) return;

        if (useDirectPlayMethod)
        {
            // 遷移（矢印）が組まれていない場合でも、C#から「Jump」を強制的に頭から再生
            anim.Play(jumpStateName, 0, 0f);
        }
        else
        {
            //  トリガーを引いて「待機 ➔ ジャンプ ➔ 待機」へ滑らかに遷移
            if (!string.IsNullOrEmpty(jumpTriggerName))
            {
                anim.SetTrigger(jumpTriggerName);
            }
        }
        Debug.Log("[PlayerJumpBlock] ジャンプ台のアニメーションを再生しました。");
    }
}