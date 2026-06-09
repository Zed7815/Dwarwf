using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameManager gameManager; // GameManagerを参照
    public Player_walk playerWalk;  // PlayerWalkを参照 

    private Vector3 startPosition; // 最初の位置を覚えておく変数

    void Start()
    {
        // ゲーム開始時の位置を保存しておく
        startPosition = transform.position;
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
        transform.position = startPosition;
        StopMove();
        // 物理演算（Rigidbody2D）を使っている場合、速度も0
        // GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }
}
