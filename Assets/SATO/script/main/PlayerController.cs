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
    public void ResetPosition()
    {
        // 1. 物理・コルーチンのリセット
        playerWalk.ResetPlayerStatus();

        // 2. 位置と向き（スケール）を初期状態に戻す
        transform.position = startPosition;
        transform.localScale = startScale;

        // 3. アニメーターの「完全」初期化
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            // これが最も強力なリセット命令です。
            // 全パラメーターをデフォルト値に戻し、ステートをEntryからやり直させます。
            anim.Rebind();

            // Rebind直後はアニメーターが停止した状態になることがあるため、
            // 0フレーム分更新して現在の状態（位置やIdleアニメ）を反映させます。
            anim.Update(0f);
        }
    }
}