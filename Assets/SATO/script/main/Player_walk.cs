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
    public float wallBounceMultiplier = 1.5f;   // 【新規】壁跳ね返りの勢いを決める倍率
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
    public Vector2 boxSize = new Vector2(0.5f, 0.1f);
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

            // 【問題２解決策】着地している間、ジャンプコルーチンがいないのに状態が不自然なままであれば通常歩行にオート回復！
            if (!isJumping && (state == moveState.jump || state == moveState.fall))
            {
                StateChange(1); // 直ちに moveState.straight (歩行) へ引き上げ
            }
        }
        else
        {
            // 空中かつ、ジャンプブロックの意図的な大ジャンプ（isJumping）ではない場合
            // 【通常落下時（fall）：慣性0】へ強制移行させます
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
            // 【最新のバグ修正対策】ジャンプブロックの上へ滑らかに乗り上げさせるため、X速度を即座に0にするのではなく、
            // 直前の歩行速度から滑らかに0へと減衰（イージング）させます。
            // これにより、崖から踏み外した瞬間に「真下にストン」と不自然に垂直落下してブロックの側面に挟まるバグを完全に修正！
            if (rb != null)
            {
                float currentX = rb.linearVelocity.x;
                float targetX = 0f;
                // 滑らかに減速（イージング）しながら0に持っていく
                float easedX = Mathf.MoveTowards(currentX, targetX, PlayerSpeed * Time.deltaTime * 4.0f);
                rb.linearVelocity = new Vector2(easedX, rb.linearVelocity.y);
            }
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
        // 【エラー100%完全回避】CompareTag("Wall")による例外エラーを防ぐ安全な壁判定
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
            // 大ジャンプ中（isJumping == true）に壁に触れたら、マリオのように跳ね返る！
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

                    // 2. マリオ壁ジャンプ：縦の勢い(Y速度)は100%保持したまま、横(X速度)だけを反転させて跳ね返す！
                    float keepYVelocity = rb.linearVelocity.y;

                    // 🔴 ★★★ コレがココに入ります！ ★★★ 🔴
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

    // 【問題４対策：中央に来たら美しくピタッと静止してためる】
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
        yield return new WaitUntil(() => isGrounded); // 着地までループを止める

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

        // 【最新のバグ修正対策】崖の角やブロック等に衝突して乗り上げ中にガタつく（Lerpとコライダーの競合）のを完全に無くすため、
        // 移動中はコライダーを一時的に Trigger（すり抜け可能）化して、極めてスムーズに目標位置まで移動させます！
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 1. トリガーに接した瞬間、Kinematicな制御に切り替えて綺麗な放物線で乗り上げる
        StateChange(2); // moveState.jump

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
            pos.y += jumpHeight * t * (1 - t);

            transform.position = pos;
            yield return null;
        }

        transform.position = endPos;

        // 【安全復帰】乗り上げが完了（立ち乗りため状態）したので、コライダーを「非Trigger（物理衝突あり）」にスマートに戻します。
        if (col != null) col.isTrigger = false;

        // 2. ブロックの上に乗り上げたら立ち乗りして「ピタッと」ため静止
        if (rb != null)
        {
            rb.linearVelocity = Vector4.zero;
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += jumpHeight * t * (1 - t);

            transform.position = pos;
            yield return null;
        }

        transform.position = endPos;

        // 2段階目：ブロック乗り上げ後に「ピタッと」ためる静止
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        StateChange(0); // idol
        anim.SetBool("isWalk", false);

        yield return new WaitForSeconds(0.75f);

        // 3段階目：空中慣性100%の物理大ジャンプをトリガー！
        StateChange(2);

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;

            float xVelocity = direction * playerJumpForwardPower;
            float yVelocity = playerJumpPower;

            rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        }

        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => isGrounded); // 着地を待つ

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