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

    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float playerJumpPower = 11.5f;
    public float playerJumpForwardPower = 3.6f;
    public float wallBounceMultiplier = 1.5f;
    public float jumpCenterTolerance = 0.05f;
    public int direction = 1;
    public bool JpRequest = true;

    public moveState state = moveState.idol;
    private bool isJumping = false;
    private bool jumpCanceled = false;
    private bool keepAirXVelocity = false;

    // 頭打ち管理用
    private int consecutiveHeadBumps = 0;
    private bool ignoreJumpBlocks = false;

    [Header("地面・天井判定の設定")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.15f;
    public Vector2 boxSize = new Vector2(0.16f, 0.05f);

    [Header("天井判定の微調整（緩さ設定）")]
    [Tooltip("判定の横幅。小さくするほど肩が引っかからなくなります（0.05程度推奨）")]
    public float ceilingCheckWidth = 0.1f;
    [Tooltip("判定を飛ばし始める高さ。0.4なら中心から0.4上にずらした所から判定します")]
    public float ceilingCheckOffset = 0.3f;
    [Tooltip("頭上の天井を検知する距離。")]
    public float ceilingCheckDistance = 0.05f;

    private bool isGrounded;
    private Collider2D currentGround;

    [Header("状態管理フラグ")]
    public bool jumpRequest = true;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
        }
    }

    void Update()
    {
        if (state == moveState.autoMoving) return;
        CheckGround();

        // 通常移動・ジャンプ中の天井チェック
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
            JpRequest = true;
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

        if (state == moveState.straight) { anim.SetBool("isWalk", isGrounded); walk(); }
        else if (state == moveState.fall)
        {
            anim.SetBool("isWalk", false);
            if (rb != null)
            {
                float easedX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, PlayerSpeed * Time.deltaTime * 4.0f);
                rb.linearVelocity = new Vector2(easedX, rb.linearVelocity.y);
            }
        }
        else
        {
            anim.SetBool("isWalk", isJumping && isGrounded);
            if (state == moveState.idol && rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    // --- 地面判定 ---
    void CheckGround()
    {
        Vector2 rayOrigin = (groundCheck != null) ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.45f;
        float dist = (groundCheck != null) ? 0.05f : groundCheckDistance;
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.down, dist, groundLayer);
        isGrounded = (hit.collider != null);
        currentGround = hit.collider;
    }

    // --- 天井判定（共通化） ---
    bool CheckCeiling()
    {
        // 中心から少し上にずらした位置から、細い横幅で判定を飛ばす
        Vector2 origin = (Vector2)transform.position + Vector2.up * ceilingCheckOffset;
        Vector2 size = new Vector2(ceilingCheckWidth, 0.05f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.up, ceilingCheckDistance, groundLayer);

        // デバッグ用にエディタ上で判定範囲を表示（緑色の線）
        Debug.DrawRay(origin + Vector2.left * ceilingCheckWidth / 2, Vector2.up * ceilingCheckDistance, Color.green);
        Debug.DrawRay(origin + Vector2.right * ceilingCheckWidth / 2, Vector2.up * ceilingCheckDistance, Color.green);

        return hit.collider != null;
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

            // ★改良した天井チェックを使用
            if (CheckCeiling())
            {
                headBumped = true;
                break;
            }

            transform.position = nextPos;
            yield return null;
        }

        if (col != null) col.isTrigger = false;
        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.linearVelocity = Vector2.zero; }

        if (headBumped)
        {
            consecutiveHeadBumps++;
            if (consecutiveHeadBumps >= 2) StartCoroutine(IgnoreJumpCooldown(3.0f));

            direction *= -1;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;

            StateChange(3);
            isJumping = false;
            jumpRequest = true;
            yield break;
        }

        consecutiveHeadBumps = 0;
        transform.position = endPos;
        StateChange(0);
        yield return new WaitForSeconds(0.75f);
        StateChange(2);

        if (targetBlock != null)
        {
            PlayerJumpBlock blockScript = targetBlock.GetComponent<PlayerJumpBlock>();
            if (blockScript != null) blockScript.TriggerJumpAnimation();
        }

        if (rb != null) rb.linearVelocity = new Vector2(direction * playerJumpForwardPower, playerJumpPower);
        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);

        jumpRequest = true;
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

    public void StateChange(int n)
    {
        Collider2D myCol = GetComponent<Collider2D>();
        switch (n)
        {
            case 0: state = moveState.idol; anim.SetBool("isWalk", false); if (myCol != null) myCol.enabled = true; break;
            case 1: state = moveState.straight; anim.SetBool("isWalk", isGrounded); if (myCol != null) myCol.enabled = true; break;
            case 2: state = moveState.jump; anim.SetBool("isWalk", false); if (myCol != null) myCol.enabled = true; break;
            case 3: state = moveState.fall; anim.SetBool("isWalk", false); if (myCol != null) myCol.enabled = true; break;
            case 4: state = moveState.autoMoving; anim.SetBool("isWalk", false); anim.SetBool("isFalling", true); if (myCol != null) myCol.enabled = false; break;
        }
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
        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.linearVelocity = Vector2.zero; rb.simulated = false; rb.simulated = true; }
        if (sr != null) { sr.enabled = true; sr.flipX = false; }
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.enabled = true;
    }

    void walk() { if (state == moveState.straight && isGrounded) rb.linearVelocity = new Vector2(direction * PlayerSpeed, rb.linearVelocity.y); }

    void OnCollisionEnter2D(Collision2D collision)
    {
        bool isWall = false;
        if (collision.gameObject != null)
        {
            string objName = collision.gameObject.name.ToLower();
            if (objName.Contains("wall") || objName.Contains("kabe")) isWall = true;
            else if (collision.gameObject.tag == "wall" || collision.gameObject.tag == "Wall") isWall = true;
        }
        if (isWall && isJumping && rb != null && Time.time - lastFlipTime > 0.15f)
        {
            lastFlipTime = Time.time;
            float wallNormalX = collision.contacts[0].normal.x;
            direction *= -1;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
            if (wallNormalX < -0.5f) StartCoroutine(TriggerWallKick("isWallKickRight"));
            else if (wallNormalX > 0.5f) StartCoroutine(TriggerWallKick("isWallKickLeft"));
            rb.linearVelocity = new Vector2(direction * playerJumpForwardPower * wallBounceMultiplier, rb.linearVelocity.y);
        }
        else if (isWall && isGrounded && Time.time - lastFlipTime > 0.3f)
        {
            lastFlipTime = Time.time;
            direction *= -1;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }
    }

    private IEnumerator TriggerWallKick(string boolName)
    {
        if (anim != null && sr != null)
        {
            anim.SetBool(boolName, true);
            if (boolName == "isWallKickRight") sr.flipX = true;
            else sr.flipX = false;
            yield return new WaitForSeconds(0.2f);
            anim.SetBool(boolName, false);
            sr.flipX = false;
        }
    }

    IEnumerator WalkToBlockCenter(Transform jumpBlock)
    {
        if (jumpBlock == null) { jumpCanceled = true; yield break; }
        state = moveState.idol;
        float targetX = jumpBlock.position.x;
        while (jumpBlock != null && Mathf.Abs(transform.position.x - targetX) > jumpCenterTolerance)
        {
            transform.position = new Vector3(Mathf.MoveTowards(transform.position.x, targetX, PlayerSpeed * Time.deltaTime), transform.position.y, transform.position.z);
            yield return null;
        }
        if (jumpBlock != null) transform.position = new Vector3(jumpBlock.position.x, transform.position.y, transform.position.z);
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        StateChange(0);
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

    public bool IsJumping() { return isJumping; }
    public bool IsAutoMoving() { return state == moveState.autoMoving; }

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

    float GetJumpDirection(Transform jumpBlock)
    {
        if (jumpBlock == null) return direction;
        float diff = jumpBlock.position.x - transform.position.x;
        return Mathf.Abs(diff) < 0.1f ? direction : Mathf.Sign(diff);
    }
}