using UnityEngine;

public class AutoJumpp : MonoBehaviour
{
    public Player_walk pl;

    void Start()
    {
        if (pl == null) pl = GetComponentInParent<Player_walk>();
    }

    // ① トリガーコライダーに入り込んだ瞬間の即時検知
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TriggerCheck(collision);
    }

    // ② 最初からトリガーの中にいた（初期重なり・滞留中）場合のための検知
    private void OnTriggerStay2D(Collider2D collision)
    {
        TriggerCheck(collision);
    }

    // 共通チェック＆多重暴発を強力にガードする安全キック機構
    private void TriggerCheck(Collider2D collision)
    {
        if (pl == null) return;

        // jumpRequest が true（地面からリスタート）かつ、現在大ジャンプコルーチンが非アクティブ（!IsJumping）な時のみ起動！
        if (pl.jumpRequest && !pl.IsJumping())
        {
            // PlayerJumpBlock スクリプトがある場合
            if (collision.TryGetComponent<PlayerJumpBlock>(out var jumpBlock))
            {
                // 大ジャンプ一元化コルーチンを安全起動
                pl.StartCoroutine(pl.HandleDoubleJumpSequence(collision.transform));
            }
        }
    }
}