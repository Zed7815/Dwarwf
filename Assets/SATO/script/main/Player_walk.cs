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
    public Transform groundCheck;                // 【エラー修正】変数宣言を追加（自動割り当て対応）
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.15f;    // 【崖際バグ対策】0.6fから0.15f（足元ギリギリ）へ短縮
    public Vector2 boxSize = new Vector2(0.16f, 0.05f); // 【崖際バグ対策】0.25fから0.16f（体幅よりスリム）へ修正
    private bool isGrounded;
    private Collider2D currentGround;

    [Header("状態管理フラグ")]
    public bool jumpRequest = true;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // もしInspectorで未割り当ての場合、子オブジェクトから自動検索
        if (groundCheck == null)
        {
            groundCheck = transform.Find("GroundCheck");
        }
    }

    void Update()
    {
        CheckGround();

        anim.SetBool("isFalling", !isGrounded);

        if (isGrounded)
        {
            JpRequest = true;
            jumpRequest = true;

            if (!isJumping && state == moveState.fall)
            {
                StartCoroutine(LandSequence());
            }
        }

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
            
            if (!isJumping && (state == moveState.straight || state == moveState.idol))
            {
                if (rb != null && rb.linearVelocity.y < -0.1f)
                {
                    state = moveState.fall;
                }
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
            // ジャンプ中でない（コルーチン制御下でない）場合のみ、歩行アニメを完全にOFFにします
            if (!isJumping)
            {
                anim.SetBool("isWalk", false);
            }
            else
            {
                // ジャンプコルーチンが走っている（isJumping == true）時は、接地中だけ歩きアニメを許可します
                anim.SetBool("isWalk", isGrounded);
            }

            // 待機状態(idol)の時は不自然に滑らないようX速度を完全にセーブ
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

            anim.SetBool("isWalk", isGrounded);

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

        if (!isGrounded)
        {
            yield break;
        }

        isJumping = true;

        // --- 1. 中心へのピタッとした位置合わせ ---
        yield return StartCoroutine(WalkToBlockCenter(jumpBlock));

        if (jumpCanceled)
        {
            jumpCanceled = false;
            isJumping = false;
            jumpRequest = true;
            yield break;
        }

        // --- 2. 溜めフェーズ (isCharging = true) ---
        anim.SetBool("isCharging", true); // 溜めアニメの再生スイッチON!
        rb.linearVelocity = Vector2.zero; // 溜め中は動かないようにピタッと止める
        yield return new WaitForSeconds(0.75f); // 0.75秒ためる
        anim.SetBool("isCharging", false); // 離陸と同時にスイッチOFF

        // --- 3. 離陸フェーズ (大ジャンプ発動) ---
        jumpRequest = false;
        keepAirXVelocity = true;
        StateChange(2); // moveState.jump にする

        if (jumpBlock != null)
        {
            PlayerJumpBlock targetBlock = jumpBlock.GetComponent<PlayerJumpBlock>();
            if (targetBlock == null) targetBlock = jumpBlock.GetComponentInParent<PlayerJumpBlock>();
            if (targetBlock == null) targetBlock = jumpBlock.GetComponentInChildren<PlayerJumpBlock>();
            
            if (targetBlock != null)
            {
                targetBlock.TriggerJumpAnimation();
            }
        }

        if (rb != null)
        {
            float jumpDirection = GetJumpDirection(jumpBlock);
            rb.linearVelocity = new Vector2(jumpDirection * playerJumpForwardPower, playerJumpPower);
        }

        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);

        // --- 4. 着地しゃがみフェーズ (isLanding = true) ---
        anim.SetBool("isLanding", true); // しゃがみアニメの再生スイッチON!
        rb.linearVelocity = Vector2.zero; // その場にピタッと止める
        yield return new WaitForSeconds(0.3f); // 0.3秒間しゃがみ待機
        anim.SetBool("isLanding", false); // しゃがみスイッチOFF

        StateChange(1); // 通常歩行に戻す
        keepAirXVelocity = false;
        isJumping = false;
        jumpRequest = true;
    }

    public IEnumerator HandleDoubleJumpSequence(Transform targetBlock)
    {
        // 多重起動を確実にブロック
        if (isJumping) yield break;

        // 地地にいない（空中での接触）の場合は処理をスキップ
        if (!isGrounded)
        {
            yield break;
        }

        isJumping = true;

        // 崖の角やブロック等に衝突して乗り上げ中にコライダーが引っかかるのを防ぐため、一時的にTrigger化
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 1段階目（乗り上げ開始）：アニメーションと物理を制御
        StateChange(2); // moveState.jump に変更

        Vector3 startPos = transform.position;
        // ターゲットブロックの少し上（乗り上げる高さ）を目標地点に設定
        Vector3 endPos = targetBlock.position + Vector3.up * 1.2f;

        float duration = 0.5f;
        float jumpHeight = 1.8f;
        float time = 0f;

        // 乗り上げ中は物理演算に邪魔されないようKinematicに変更
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // Lerp ＋ 二次関数による美しい放物線補間
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            Vector3 pos = Vector3.Lerp(startPos, endPos, t);
            pos.y += jumpHeight * t * (1 - t); // 綺麗な放物線の弧を描く補間式

            transform.position = pos;
            yield return null;
        }

        // ぴったり目標位置に合わせる
        transform.position = endPos;

        // 乗り上げが完了したのでコライダーの物理衝突をONに戻す
        if (col != null) col.isTrigger = false;

        // 2段階目（ブロック乗り上げ後に静止 ＆ 溜め待機）
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        StateChange(0); // idol
        anim.SetBool("isWalk", false);

        // ジャンプ台の上で0.75秒ピタッとためる
        yield return new WaitForSeconds(0.75f);

        // 3段階目：物理大ジャンプをトリガー
        StateChange(2); // moveState.jump

        // 🌟 ジャンプ台自身のアニメーション（PlayerJumpBlock）を周囲の親子オブジェクト含めて高精度に自動検出し、トリガー
        if (targetBlock != null)
        {
            PlayerJumpBlock blockScript = targetBlock.GetComponent<PlayerJumpBlock>();
            if (blockScript == null) blockScript = targetBlock.GetComponentInParent<PlayerJumpBlock>();
            if (blockScript == null) blockScript = targetBlock.GetComponentInChildren<PlayerJumpBlock>();

            if (blockScript != null)
            {
                blockScript.TriggerJumpAnimation();
            }
        }

        // 大ジャンプ力を物理的に加える
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;

            float xVelocity = direction * playerJumpForwardPower;
            float yVelocity = playerJumpPower;

            rb.linearVelocity = new Vector2(xVelocity, yVelocity);
        }

        // 着地判定のバッティングを防ぐため、僅かな猶予を設ける
        yield return new WaitForSeconds(0.2f);

        // 落下中（または最高点に達した状態）に接地したときのみ着地を完了する
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);

        // 各種状態を初期化して通常歩行にバトンタッチ
        jumpRequest = true;
        isJumping = false;
        StateChange(1); // 通常歩行（straight）へ復帰
    }

    void CheckGround()
    {
        Vector2 rayOrigin;
        float actualDistance = groundCheckDistance;
        Vector2 actualSize = boxSize;

        // groundCheck オブジェクト（子オブジェクト）が割り当てられていればそれを使用
        if (groundCheck != null)
        {
            rayOrigin = groundCheck.position;
            actualDistance = 0.05f; // 専用オブジェクトがある場合は極短レンジで判定
        }
        else
        {
            // groundCheckオブジェクトが無い場合：プレイヤーの足元(0.45f下)を起点に自動計算
            rayOrigin = (Vector2)transform.position + Vector2.down * 0.45f;
        }

        // 下方向にキャストして接地を検知（横幅をスリムに、距離を短く）
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, actualSize, 0f, Vector2.down, actualDistance, groundLayer);

        isGrounded = (hit.collider != null);
        currentGround = hit.collider;
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

    private IEnumerator LandSequence()
    {
        isJumping = true; // 通常歩行による速度上書きをロック

        jumpRequest = false;
        StateChange(0); // idol

        anim.SetBool("isLanding", true); // しゃがみアニメーション

        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(0.3f); // 0.3秒しゃがみ

        anim.SetBool("isLanding", false);
        jumpRequest = true;
        isJumping = false;
        StateChange(1); // 歩行状態に戻る
    }
}