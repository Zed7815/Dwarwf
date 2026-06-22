using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Player_walk : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb; // 物理挙動確認用

    private SpriteRenderer sr;

    private float lastFlipTime = 0f;

    public enum moveState
    {
        idol,     // 停止
        straight, // 前進
        jump,     // 物理ジャンプ
        autoMoving// 自動ジャンプ 
    }


    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float playerJumpPower = 10f; // ジャンプ力
    public int direction = 1;
    [SerializeField]private moveState state = moveState.idol;

    [Header("地面判定の設定")]
    public LayerMask groundLayer; // 地面とみなすレイヤー
    public float groundCheckDistance = 0.6f; // 下方向に飛ばす光の長さ
    public Vector2 boxSize = new Vector2(0.5f, 0.1f); // 足元のチェック範囲 (横、縦)
    private bool isGrounded; // 地面に接しているか

    [Header("状態管理フラグ")]
    public bool jumpRequest = true;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        CheckGround();
        if (isGrounded) jumpRequest = true;

        // アニメーションの状態を一括管理
        if (state == moveState.straight)
        {
            anim.SetBool("isWalk", isGrounded); // 地面にいるときだけ歩きアニメ
            walk();
        }
        else
        {
            anim.SetBool("isWalk", false);
            // idol状態のときは横移動を止める
            if (state == moveState.idol && rb != null)
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void stay()
    {

    }

    void walk()
    {
       if (state == moveState.straight)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(direction * PlayerSpeed,rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
    }

   

    public IEnumerator SpringSequence(Transform spring)
    {
        state = moveState.autoMoving;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        float heightDiff = spring.position.y - transform.position.y;

        if (heightDiff > 0.5f)
        {
            // バネの手前で止まる
            yield return new WaitForSeconds(0.2f);

            // バネに飛び乗る
            yield return StartCoroutine(MoveToTarget(spring.position + Vector3.up * 0.8f,0.4f,1.5f));
        }
        else
        {
            // バネが同じ高さにある場合
            Vector3 centerPos = new Vector3(spring.position.x,transform.position.y,transform.position.z);
            yield return StartCoroutine(MoveToTarget(centerPos, 0.3f, 0f));
        }

        yield return new WaitForSeconds(0.5f);

        rb.bodyType = RigidbodyType2D.Dynamic;

        jumpRequest = false;
        rb.linearVelocity = new Vector2(direction * 3f, playerJumpPower * 1.3f); // 少し前方に強く飛ぶ

        yield return new WaitForSeconds(0.3f);

        yield return new WaitUntil(() => isGrounded);

        state  = moveState.straight;
    }

    IEnumerator MoveToTarget(Vector3 endPos, float duration, float jumpHeight)
    {
        Vector3 startPos = transform.position;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            if (jumpHeight > 0f)
            {
                pos.y += 4 * jumpHeight * t * (1 - t);
            }
            transform.position = pos;
            yield return null;
        }
        transform.position = endPos;
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



 
    public IEnumerator Jump()
    {
        state = moveState.jump;
        yield return new WaitForSeconds(0.75f);

        jumpRequest = false;
        rb.linearVelocity = new Vector2(direction * 1f, playerJumpPower);

        // 地面に付くまで待つ
        yield return new WaitUntil(() => isGrounded);
        state = moveState.straight;
    }

    public IEnumerator AutoJump(Transform target)
    {
        state = moveState.autoMoving;
        Vector3 startPos = transform.position;
        Vector3 endPos = target.position + Vector3.up * 1.2f;

        float duration = 0.5f;
        float jumpHeight = 2f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += 1 * jumpHeight * t * (1 - t);
            transform.position = pos;
            yield return null;
        }

        transform.position = endPos;
        state = moveState.straight;
    }
   
   
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            if (Time.time - lastFlipTime > 0.3f)
            {
                lastFlipTime = Time.time;

                direction *= -1;

                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * direction;
                transform.localScale = scale;
            }
        }
    }

    public void ResetPlayerStatus()
    {
        StopAllCoroutines();

        // 状態を初期化
        state = moveState.idol;
        anim.SetBool("isWalk",false);
        direction = 1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;

        // 物理当初期化
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ResetDirection()
    {
        direction = 1; // 右向きに戻す
    }
}