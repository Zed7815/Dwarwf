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
        transform.SetParent(null);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false; // 一旦物理を止める
            rb.linearVelocity = Vector2.zero;
        }

        // 先に Player_walk 側の ResetPlayerStatus を呼んで isTrigger = false にする
        if (playerWalk != null)
        {
            playerWalk.ResetPlayerStatus();
        }

        // その後に位置を戻す
        transform.position = startPosition;
        transform.localScale = startScale;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (rb != null)
        {
            rb.simulated = true; // 物理再開
        }
    }
}