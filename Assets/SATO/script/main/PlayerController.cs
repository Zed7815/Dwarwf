using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameManager gameManager; // GameManagerを参照
    public Player_walk playerWalk;  // PlayerWalkを参照 

    private Vector3 startPosition; // 最初の位置を覚えておく変数
    private Vector3 startScale;      // 最初のスケールを保管

    void Start()
    {
        // ゲーム開始時の位置を保存しておく
        startPosition = transform.position;
        startScale = transform.localScale;
    }

    public void StartMove()
    {
        playerWalk.StateChange(1); // straight
    }

    public void StopMove()
    {
        playerWalk.StateChange(0); // idol
    }


    // 初期位置に戻るための関数
    // PlayerController.cs

    public void ResetPosition()
    {
        // 1. 親子関係を完全に切る（リフトや鳥に乗っていた場合のため）
        transform.SetParent(null);

        // 2. 物理的な速度と状態をリセット
        // ここで Rigidbody の速度をゼロにし、シミュレーションを一時停止するのがコツです
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // 速度をゼロに
            rb.angularVelocity = 0;           // 回転速度もゼロに
            rb.simulated = false;             // 物理演算を一時的に止める
        }

        // 3. Player_walk側の内部ステータスをリセット
        playerWalk.ResetPlayerStatus();

        // 4. 初期位置とスケールに戻す
        transform.position = startPosition;
        transform.localScale = startScale;

        // 5. アニメーションをリセット
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 6. 物理演算を再開
        if (rb != null)
        {
            rb.simulated = true;
        }
    }
}