using System.Collections;
using UnityEngine;

public class Player_walk : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private float lastFlipTime = 0f;

    public enum moveState
    {
        idol,       // 0: 停止
        straight,   // 1: 通常移動（地上を歩いている状態）
        jump,       // 2: 物理大ジャンプ（空中慣性をジャンプ着地まで100%保持＆壁キック可能）
        fall,       // 3: 通常落下（ジャンプブロックを経由しない通常落下・空中慣性0）
        autoMoving  // 4: 自動ジャンプ
    }

    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float playerJumpPower = 11.5f;       // ジャンプ高さ
    public float playerJumpForwardPower = 3.6f; // ジャンプの横の勢い
    public float wallBounceMultiplier = 1.5f;   // 壁跳ね返りの勢いを決める倍率
    public float jumpCenterTolerance = 0.05f;
    public int direction = 1;
    public bool JpRequest = true;

    private moveState state = moveState.idol;
    private bool isJumping = false;
    private bool jumpCanceled = false;
    private bool keepAirXVelocity = false;

    [Header("地面判定の設定")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.6f;
    public Vector2 boxSize = new Vector2(0.25f, 0.1f); // 空中浮遊防止のために検出横幅を0.25fに最適化
    private bool isGrounded;
    private Collider2D currentGround;

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

        if (isGrounded)
        {
            JpRequest = true;
            jumpRequest = true;

            // 着地している間、ジャンプコルーチンがいないのに状態が不自然なままであれば通常歩行
            if (!isJumping && (state == moveState.jump || state == moveState.fall))
            {
                StateChange(1); // 直ちに moveState.straight (歩行) へ安全に復帰！
            }
        }
        else
        {
            // 空中かつ、ジャンプブロックの意図的な大ジャンプ（isJumping）ではない場合
            // 【通常落下時（fall）：慣性0】へ強制移行
            if (!isJumping && (state == moveState.straight || state == moveState.idol))
            {
                state = moveState.fall;
            }
        }

        // 状態に応じたリアルタイム挙動・グラフィックス制御
        if (state == moveState.straight)
        {
            anim.SetBool("isWalk", isGrounded);
            walk();
        }
        else if (state == moveState.fall)
        {
            anim.SetBool("isWalk", false);
            // 通常落下：空中落下は横の勢いを完全にカットして直下へ落とす
            if (rb != null)
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else
        {
            anim.SetBool("isWalk", false);
            // それ以外の時は不自然に滑らないようX速度を完全にセーブ
            if (state == moveState.idol && rb != null)
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void walk()
    {
        if (state == moveState.straight && isGrounded)
        {
            rb.linearVelocity = new Vector2(direction * PlayerSpeed, rb.linearVelocity.y);
        }
    }

    // 壁への衝突判定
    // 地面、または高台のコライダーとの接触処理
    void OnCollisionEnter2D(Collision2D collision)
    {
        bool isWall = false;
        if (collision.gameObject != null)
        {
            string objName = collision.gameObject.name.ToLower();
            if (objName.Contains("wall") || objName.Contains("kabe"))
            {
                isWall = true;
            }
            else if (collision.gameObject.tag == "wall" || collision.gameObject.tag == "Wall")
            {
                isWall = true;
            }
        }

        if (isWall)
        {
            // 大ジャンプ中（isJumping == true）に壁に触れたら、マリオのように跳ね返る
            if (isJumping && rb != null)
            {
                if (Time.time - lastFlipTime > 0.15f)
                {
                    lastFlipTime = Time.time;

                    // 1. 進行方向を反転し、スケール（画像の向き）も反転
                    direction *= -1;
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x) * direction;
                    transform.localScale = scale;

                    // 2. マリオ壁ジャンプ：縦の勢い(Y速度)は保持したまま、横(X速度)だけを反転させて跳ね返す
                    float keepYVelocity = rb.linearVelocity.y;
                    float bounceXVelocity = direction * playerJumpForwardPower * wallBounceMultiplier;

                    rb.linearVelocity = new Vector2(bounceXVelocity, keepYVelocity);
                }
            }
            // 地面歩行中の壁衝突時の通常の折り返し
            else if (isGrounded)
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
    }

    // 中央に来たら美しくピタッと静止してためる
    IEnumerator WalkToBlockCenter(Transform jumpBlock)
    {
        if (jumpBlock == null)
        {
            jumpCanceled = true;
            yield break;
        }

        state = moveState.idol; // 入力をキャンセルして中央合わせ
        anim.SetBool("isWalk", true); // アニメだけは歩きをオン

        float targetX = jumpBlock.position.x;

        while (jumpBlock != null && Mathf.Abs(transform.position.x - targetX) > jumpCenterTolerance)
        {
            targetX = jumpBlock.position.x;

            float nextX = Mathf.MoveTowards(
                transform.position.x,
                targetX,
                PlayerSpeed * Time.deltaTime
            );

            transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            yield return null;
        }

        if (jumpBlock == null)
        {
            jumpCanceled = true;
            yield break;
        }

        // 完全なブロックの中心にキチッとあわせる
        transform.position = new Vector3(jumpBlock.position.x, transform.position.y, transform.position.z);

        // 中心到達した瞬間：力学慣性を完全に殺して（0）アニメを即時「待機(State0)」にして不自然なブレを防ぐ
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        StateChange(0); // idol状態へ
    }

    public IEnumerator Jump(Transform jumpBlock)
    {
        if (isJumping) yield break;
        isJumping = true;
        jumpCanceled = false;

        if (jumpBlock != null)
        {
            yield return WalkToBlockCenter(jumpBlock);
        }

        if (jumpCanceled)
        {
            jumpRequest = true;
            isJumping = false;
            keepAirXVelocity = false;
            StateChange(1);
            yield break;
        }

        // 綺麗にピタッと静止し、ためる
        yield return new WaitForSeconds(0.75f);

        jumpRequest = false;
        keepAirXVelocity = true;
        StateChange(2); // 空中慣性をオンにするジャンプ状態へ

        if (rb != null)
        {
            float jumpDirection = GetJumpDirection(jumpBlock);
            rb.linearVelocity = new Vector2(jumpDirection * playerJumpForwardPower, playerJumpPower);
        }

        // 離陸の瞬間に判定が即誤作動しないよう0.2秒待機
        yield return new WaitForSeconds(0.2f);

        // 落下中（または完全に最高点に達した状態）に接地したときのみ着地判定を終了
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);

        StateChange(1); // 歩行状態へ復帰
        keepAirXVelocity = false;
        isJumping = false;
        jumpRequest = true;
    }

    public IEnumerator HandleDoubleJumpSequence(Transform targetBlock)
    {
        if (isJumping) yield break;
        isJumping = true;
        jumpRequest = false;

        // 崖の角やブロック等に衝突して乗り上げ中にコライダーが引っかかるのを防ぐため、一時的にTrigger化
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 1段階目(乗り上げ)：完璧な放物線ジャンプコルーチン
        StateChange(2);

        Vector3 startPos = transform.position;
        Vector3 endPos = targetBlock.position + Vector3.up * 1.2f;

        float duration = 0.5f;
        float jumpHeight = 1.8f;
        float time = 0f;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += jumpHeight * t * (1 - t); // 綺麗な放物線の弧を描くように修正

            transform.position = pos;
            yield return null;
        }

        transform.position = endPos;

        // 乗り上げが完了したので物理衝突をONに戻す
        if (col != null) col.isTrigger = false;

        // 2段階目：ブロック乗り上げ後に静止
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        StateChange(0); // idol
        anim.SetBool("isWalk", false);

        yield return new WaitForSeconds(0.75f);

        // 3段階目:物理大ジャンプをトリガー
        StateChange(2);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;

            float xVelocity = direction * playerJumpForwardPower;
            float yVelocity = playerJumpPower;

            rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        }

        yield return new WaitForSeconds(0.2f);

        //落下中（または完全に最高点に達した状態）に接地したときのみ着地判定を終了する
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);

        jumpRequest = true;
        isJumping = false;
        StateChange(1); // 通常歩行
    }

    void CheckGround()
    {
        Vector2 rayOrigin = transform.position + Vector3.down * 0.2f;
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = (hit.collider != null);
        currentGround = hit.collider;

        Debug.DrawRay(rayOrigin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    float GetJumpDirection(Transform jumpBlock)
    {
        if (jumpBlock == null) return direction;

        // 極微小な位置ズレによる反転を防ぐため、現在の進行方向(direction)を優先する
        if (Mathf.Abs(jumpBlock.position.x - transform.position.x) < 0.1f)
        {
            return direction;
        }

        float jumpDirection = Mathf.Sign(jumpBlock.position.x - transform.position.x);
        if (jumpDirection == 0) jumpDirection = direction;
        return jumpDirection;
    }

    public bool IsJumping()
    {
        return isJumping;
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
            case 3:
                state = moveState.fall;
                anim.SetBool("isWalk", false);
                break;
        }
    }

    public void ResetPlayerStatus()
    {
        StopAllCoroutines();

        state = moveState.idol;
        anim.SetBool("isWalk", false);
        direction = 1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        jumpRequest = true;
        isJumping = false;
    }

    public void ResetDirection()
    {
        direction = 1;
        jumpRequest = true;
        isJumping = false;
        jumpCanceled = true;
        keepAirXVelocity = false;
    }
}