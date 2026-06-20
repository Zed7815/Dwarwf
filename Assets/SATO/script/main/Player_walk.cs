using JetBrains.Annotations;
using System.IO.IsolatedStorage;
using UnityEngine;

public class Player_walk : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb; // 物理挙動確認用

    public enum moveState
    {
        idol,
        straight,
        jump
    }

    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public int direction = 1;
    private moveState state = moveState.idol;

    [Header("地面判定の設定")]
    public LayerMask groundLayer; // 地面とみなすレイヤー
    public float groundCheckDistance = 0.6f; // 下方向に飛ばす光の長さ
    public Vector2 boxSize = new Vector2(0.5f, 0.1f); // 足元のチェック範囲 (横、縦)
    private bool isGrounded; // 地面に接しているか

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 地面に接しているか常にチェック
        CheckGround();

        switch (state)
        {
            case moveState.idol:
                anim.SetBool("isWalk", false);
                stay();
                break;

            case moveState.straight:
                anim.SetBool("isWalk", isGrounded);
                walk();
                break;

            case moveState.jump:
                anim.SetBool("isWalk", false);
                break;
        }
    }

    void stay()
    {

    }

    void walk()
    {
        if (isGrounded)
        {
            transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime * direction);
        }
        else
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2 (0, rb.linearVelocity.y);
            }
        }
    }

    void CheckGround()
    {
        Vector2 rayOrigin = transform.position + Vector3.down * 0.2f;
        // BoxCastを使用し地面があるかを確認
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = (hit.collider != null);

        Debug.DrawRay(rayOrigin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }
   
    public void StateChange(int n)
    {
        switch (n)
        {
            case 0:
                state = moveState.idol;
                anim.SetBool("isWalk", false);
                break;

            case 1:
                state = moveState.straight;
                anim.SetBool("isWalk", isGrounded);
                break;

            case 2:
                state = moveState.jump;
                anim.SetBool("isWalk", false);
                break;
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            direction *=-1;

            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    public void ResetDirection()
    {
        direction = 1; // 右向きに戻す
    }
}