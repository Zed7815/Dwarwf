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
        straight,   // 1: 通常移動
        jump,       // 2: 物理大ジャンプ
        fall,       // 3: 通常落下
        autoMoving  // 4: 自動ジャンプ
    }

    [Header("プレイヤーの基本数値")]
    public float PlayerSpeed = 5.0f;
    public float playerJumpPower = 11.5f;
    public float playerJumpForwardPower = 3.6f;
    public float wallBounceMultiplier = 1.5f;
    public float jumpCenterTolerance = 0.05f;
    public int direction = 1;

    [Header("状態管理")]
    public moveState state = moveState.idol;
    private bool isJumping = false;
    public bool jumpRequest = true;

    [Header("地面・天井判定の設定")]
    public Transform groundCheck;
    public LayerMask groundLayer; // 地面（足場）用レイヤー
    public float groundCheckDistance = 0.15f;
    public Vector2 boxSize = new Vector2(0.16f, 0.05f);

    [Header("壁検知（反転）の設定")]
    public LayerMask wallLayer;   // 壁（反転用）用レイヤー
    [Tooltip("壁を検知する距離。0.4〜0.6程度を推奨")]
    public float wallCheckRayDistance = 0.5f;

    [Header("天井判定の微調整")]
    public float ceilingCheckWidth = 0.1f;
    public float ceilingCheckOffset = 0.3f;
    public float ceilingCheckDistance = 0.05f;

    private bool isGrounded;
    private int consecutiveHeadBumps = 0;
    private bool ignoreJumpBlocks = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (groundCheck == null) groundCheck = transform.Find("GroundCheck");
    }

    void Update()
    {
        if (state == moveState.autoMoving) return;

        CheckGround();

        // 移動中、または落下中に前方の壁をチェック（レイヤーを使用）
        if (state == moveState.straight || state == moveState.fall)
        {
            CheckWallFront();
        }

        // 天井チェック（頭打ち処理）
        if (rb != null && rb.linearVelocity.y > 0.1f)
        {
            if (CheckCeiling())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                if (state == moveState.jump) state = moveState.fall;
            }
        }

        anim.SetBool("isFalling", !isGrounded);

        if (isGrounded)
        {
            jumpRequest = true;
            if (!isJumping && state == moveState.fall) StartCoroutine(LandSequence());
            if (!isJumping && (state == moveState.jump || state == moveState.fall)) StateChange(1);
        }
        else
        {
            if (!isJumping && state == moveState.straight)
            {
                if (rb != null && rb.linearVelocity.y < -0.1f) state = moveState.fall;
            }
        }

        // 状態別アニメーション・移動制御
        if (state == moveState.straight)
        {
            anim.SetBool("isWalk", isGrounded);
            walk();
        }
        else if (state == moveState.fall)
        {
            anim.SetBool("isWalk", false);
            if (rb != null)
            {
                // 落下中の微細な慣性
                float easedX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, PlayerSpeed * Time.deltaTime * 2.0f);
                rb.linearVelocity = new Vector2(easedX, rb.linearVelocity.y);
            }
        }
        else
        {
            anim.SetBool("isWalk", isJumping && isGrounded);
            if (state == moveState.idol && rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void CheckGround()
    {
        Vector2 rayOrigin = (groundCheck != null) ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.45f;
        float dist = (groundCheck != null) ? 0.05f : groundCheckDistance;
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.down, dist, groundLayer);
        isGrounded = (hit.collider != null);
    }

    bool CheckCeiling()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * ceilingCheckOffset;
        Vector2 size = new Vector2(ceilingCheckWidth, 0.05f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.up, ceilingCheckDistance, groundLayer);
        return hit.collider != null;
    }

    // 壁検知ロジック（壁レイヤーを使用）
    void CheckWallFront()
    {
        // 地面に当たらないよう、少し高い位置(0.3f)から発射
        Vector2 rayOrigin = (Vector2)transform.position + Vector2.up * 0.3f;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * direction, wallCheckRayDistance, wallLayer);

        Debug.DrawRay(rayOrigin, Vector2.right * direction * wallCheckRayDistance, Color.red);

        if (hit.collider != null)
        {
            Flip();
        }
    }

    void Flip()
    {
        if (Time.time - lastFlipTime < 0.2f) return;

        lastFlipTime = Time.time;
        direction *= -1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        // 反転時に壁への押し付け速度を一旦ゼロにする
        if (rb != null && state == moveState.straight)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 衝突した相手のレイヤーが wallLayer かチェック
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            // 横方向の衝突（法線ベクトル）を確認
            if (Mathf.Abs(collision.contacts[0].normal.x) > 0.5f)
            {
                if (isJumping)
                {
                    float wallNormalX = collision.contacts[0].normal.x;
                    if (wallNormalX < -0.5f) StartCoroutine(TriggerWallKick("isWallKickRight"));
                    else if (wallNormalX > 0.5f) StartCoroutine(TriggerWallKick("isWallKickLeft"));

                    Flip();
                    rb.linearVelocity = new Vector2(direction * playerJumpForwardPower * wallBounceMultiplier, rb.linearVelocity.y);
                }
                else
                {
                    Flip();
                }
            }
        }
    }

    public void StateChange(int n)
    {
        Collider2D myCol = GetComponent<Collider2D>();
        switch (n)
        {
            case 0:
                state = moveState.idol;
                if (anim != null) { anim.SetBool("isWalk", false); anim.SetBool("isFalling", false); }
                if (myCol != null) myCol.enabled = true;
                break;
            case 1: state = moveState.straight; anim.SetBool("isWalk", isGrounded); if (myCol != null) myCol.enabled = true; break;
            case 2: state = moveState.jump; anim.SetBool("isWalk", false); if (myCol != null) myCol.enabled = true; break;
            case 3: state = moveState.fall; anim.SetBool("isWalk", false); if (myCol != null) myCol.enabled = true; break;
            case 4: state = moveState.autoMoving; anim.SetBool("isWalk", false); anim.SetBool("isFalling", true); if (myCol != null) myCol.enabled = false; break;
        }
    }

    void walk()
    {
        if (state == moveState.straight && isGrounded)
            rb.linearVelocity = new Vector2(direction * PlayerSpeed, rb.linearVelocity.y);
    }

    public void ForceStopAbilities()
    {
        StopAllCoroutines();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        isJumping = false;
        if (anim != null)
        {
            anim.SetBool("isCharging", false);
            anim.SetBool("isLanding", false);
            anim.SetBool("isWalk", false);
            anim.SetBool("isFalling", false);
            anim.SetBool("isWallKickRight", false);
            anim.SetBool("isWallKickLeft", false);
        }
        StateChange(0);
    }


    public void ResetPlayerStatus()
    {
        StopAllCoroutines();
        state = moveState.idol;
        isJumping = false;
        ignoreJumpBlocks = false;
        consecutiveHeadBumps = 0;
        jumpRequest = true;
        direction = 1;

        // ここでは速度のクリアだけ行う（simulatedの切り替えはController側に任せる）
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        if (sr != null) { sr.enabled = true; sr.flipX = false; }
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.enabled = true;
    }

    public IEnumerator Jump(Transform jumpBlock)
    {
        if (isJumping || !isGrounded) yield break;
        isJumping = true;
        yield return StartCoroutine(WalkToBlockCenter(jumpBlock));
        anim.SetBool("isCharging", true);
        yield return new WaitForSeconds(0.75f);
        anim.SetBool("isCharging", false);
        StateChange(2);
        if (jumpBlock != null) { PlayerJumpBlock block = jumpBlock.GetComponent<PlayerJumpBlock>(); if (block) block.TriggerJumpAnimation(); }
        rb.linearVelocity = new Vector2(direction * playerJumpForwardPower, playerJumpPower);
        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);
        anim.SetBool("isLanding", true);
        yield return new WaitForSeconds(0.3f);
        anim.SetBool("isLanding", false);
        StateChange(1);
        isJumping = false;
    }

    public IEnumerator HandleDoubleJumpSequence(Transform targetBlock)
    {
        if (ignoreJumpBlocks || isJumping || !isGrounded) yield break;
        isJumping = true;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        StateChange(2);
        Vector3 startPos = transform.position;
        Vector3 endPos = targetBlock.position + Vector3.up * 1.2f;
        float duration = 0.5f;
        float jumpHeight = 1.8f;
        float time = 0f;
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.linearVelocity = Vector2.zero; }
        bool headBumped = false;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            Vector3 nextPos = Vector3.Lerp(startPos, endPos, t);
            nextPos.y += jumpHeight * t * (1 - t);
            if (CheckCeiling()) { headBumped = true; break; }
            transform.position = nextPos;
            yield return null;
        }
        if (col != null) col.isTrigger = false;
        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.linearVelocity = Vector2.zero; }
        if (headBumped)
        {
            consecutiveHeadBumps++;
            if (consecutiveHeadBumps >= 2) StartCoroutine(IgnoreJumpCooldown(3.0f));
            Flip();
            StateChange(3);
            isJumping = false;
            yield break;
        }
        consecutiveHeadBumps = 0;
        transform.position = endPos;
        StateChange(0);
        yield return new WaitForSeconds(0.75f);
        StateChange(2);
        if (targetBlock != null) { PlayerJumpBlock bs = targetBlock.GetComponent<PlayerJumpBlock>(); if (bs) bs.TriggerJumpAnimation(); }
        if (rb != null) rb.linearVelocity = new Vector2(direction * playerJumpForwardPower, playerJumpPower);
        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);
        isJumping = false;
        StateChange(1);
    }

    private IEnumerator IgnoreJumpCooldown(float duration)
    {
        ignoreJumpBlocks = true;
        consecutiveHeadBumps = 0;
        yield return new WaitForSeconds(duration);
        ignoreJumpBlocks = false;
    }

    private IEnumerator TriggerWallKick(string boolName)
    {
        if (anim != null && sr != null)
        {
            anim.SetBool(boolName, true);
            if (boolName == "isWallKickRight") sr.flipX = true; else sr.flipX = false;
            yield return new WaitForSeconds(0.2f);
            anim.SetBool(boolName, false);
            sr.flipX = false;
        }
    }

    IEnumerator WalkToBlockCenter(Transform jumpBlock)
    {
        if (jumpBlock == null) yield break;
        state = moveState.idol;
        float targetX = jumpBlock.position.x;
        float timeout = 0.3f;
        while (jumpBlock != null && Mathf.Abs(transform.position.x - targetX) > 0.02f && timeout > 0)
        {
            timeout -= Time.deltaTime;
            transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, targetX, PlayerSpeed * 2.0f * Time.deltaTime), transform.position.y, transform.position.z);
            yield return null;
        }
        if (jumpBlock != null) transform.position = new Vector3(jumpBlock.position.x, transform.position.y, transform.position.z);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator LandSequence()
    {
        isJumping = true;
        StateChange(0);
        anim.SetBool("isLanding", true);
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        yield return new WaitForSeconds(0.3f);
        anim.SetBool("isLanding", false);
        isJumping = false;
        StateChange(1);
    }

    public bool IsJumping() => isJumping;
}