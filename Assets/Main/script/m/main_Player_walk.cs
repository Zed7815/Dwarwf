using System.Collections;
using UnityEngine;

public class main_Player_walk : MonoBehaviour
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
    public Vector2 boxSize = new Vector2(0.3f, 0.08f); // 【崖際バグ対策】0.25fから0.16f（体幅よりスリム）へ修正
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
        if (isJumping) yield break; // 重複防止
        isJumping = true;

        // --- 修正ポイント：ジャンプ開始時に物理移動を止める ---
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // 慣性を消す
                                              // rb.bodyType = RigidbodyType2D.Kinematic; // 必要に応じて完全に物理を止める（任意）
        }

        yield return StartCoroutine(WalkToBlockCenter(jumpBlock));

        anim.SetBool("isCharging", true);
        // タメている間も滑らないように位置を固定し続ける
        float chargeTimer = 0.75f;
        while (chargeTimer > 0)
        {
            chargeTimer -= Time.deltaTime;
            if (jumpBlock != null)
            {
                // ジャンプ台の中心に強制的に合わせ続ける（滑り落ち防止）
                transform.position = new Vector3(jumpBlock.position.x, transform.position.y, transform.position.z);
            }
            yield return null;
        }

        anim.SetBool("isCharging", false);
        StateChange(2); // Jump状態へ

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // 物理を戻す
            if (jumpBlock != null)
            {
                PlayerJumpBlock block = jumpBlock.GetComponent<PlayerJumpBlock>();
                if (block) block.TriggerJumpAnimation();
            }
            // 斜め上（ゴール方向）へ力を加える
            rb.linearVelocity = new Vector2(direction * playerJumpForwardPower, playerJumpPower);
        }

        yield return new WaitForSeconds(0.2f);
        yield return new WaitUntil(() => isGrounded && rb.linearVelocity.y <= 0.1f);

        anim.SetBool("isLanding", true);
        yield return new WaitForSeconds(0.3f);
        anim.SetBool("isLanding", false);

        StateChange(1); // Walk状態へ
        isJumping = false;
    }
    public IEnumerator HandleDoubleJumpSequence(Transform targetBlock)
    {
        if (isJumping) yield break;

        if (!isGrounded)
        {
            yield break;
        }

        isJumping = true;

        // 崖の角やブロック等に衝突して乗り上げ中にコライダーが引っかかるのを防ぐため、一時的にTrigger化
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // 1段階目(乗り上げ)：完璧な放物線ジャンプコルーチン
        StateChange(2);

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
        Vector2 rayOrigin;
        float actualDistance = groundCheckDistance;
        Vector2 actualSize = boxSize;

        // groundCheck オブジェクト（子オブジェクト）が割り当てられていればそれを使用
        if (groundCheck != null)
        {
            rayOrigin = groundCheck.position;
            actualDistance = 0.12f; // 専用オブジェクトがある場合は極短レンジで判定
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
        // 1. 実行中のすべてのコルーチン（ジャンプ、ワープ、着地演出など）を停止
        StopAllCoroutines();

        // 2. 内部状態のフラグを初期化
        state = moveState.idol;
        isJumping = false;
        jumpRequest = true;
        jumpCanceled = false;
        keepAirXVelocity = false;
        direction = 1;

        // 3. 物理演算の状態をリセット
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // ワープ中にKinematicになっていた場合への対策
            rb.linearVelocity = Vector2.zero;      // 速度をゼロに
            rb.angularVelocity = 0f;               // 回転速度をゼロに
        }

        // 4. 外見のリセット（ワープ中に非表示だった場合への対策）
        SpriteRenderer pSr = GetComponent<SpriteRenderer>();
        if (pSr != null) pSr.enabled = true;
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