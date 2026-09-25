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
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.simulated = false;
        }

        // 復活位置と向きを決定
        Vector3 targetPos = startPosition;
        int targetDir = 1;

        if (CheckpointManager.instance != null)
        {
            targetPos = CheckpointManager.instance.GetRespawnPosition();
            targetDir = CheckpointManager.instance.GetRespawnDirection();
        }

        // プレイヤーのステータスリセット（向きを渡す）
        if (playerWalk != null)
        {
            playerWalk.ResetPlayerStatus(targetDir);
        }

        // 座標を反映
        transform.position = targetPos;

        // ★スケール（左右の見た目）を向きに合わせて反転！
        Vector3 newScale = startScale;
        newScale.x = Mathf.Abs(startScale.x) * targetDir;
        transform.localScale = newScale;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        if (rb != null) rb.simulated = true;
    }
}