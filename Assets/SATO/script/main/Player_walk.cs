using JetBrains.Annotations;
using System.Collections;
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
    public float playerJumpPower = 10f;
    public float playerJumpForwardPower = 2f;
    public float jumpCenterTolerance = 0.05f;
    public int direction = 1;
    public bool jumpRequest = true;
    private moveState state = moveState.idol;
    private bool isJumping = false;
    private bool jumpCanceled = false;
    private bool keepAirXVelocity = false;

    [Header("地面判定の設定")]
    public LayerMask groundLayer; // 地面とみなすレイヤー
    public float groundCheckDistance = 0.6f; // 下方向に飛ばす光の長さ
    public Vector2 boxSize = new Vector2(0.5f, 0.1f); // 足元のチェック範囲 (横、縦)
    private bool isGrounded; // 地面に接しているか
    private Collider2D currentGround; // 今踏んでいる足場

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
            if (rb != null && !keepAirXVelocity)
            {
                rb.linearVelocity = new Vector2 (0, rb.linearVelocity.y);
            }
        }
    }

    public IEnumerator AutoJump(Transform target)
    {
        if (isJumping) yield break;
        isJumping = true;
        jumpRequest = false;

        StateChange(2);

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
            pos.y += jumpHeight * t * (1 - t);

            transform.position = pos;

            yield return null;
        }

        transform.position = endPos;
        jumpRequest = true;
        isJumping = false;
        StateChange(1);
    }

    public IEnumerator Jump()
    {
        yield return Jump(null);
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

        yield return new WaitForSeconds(0.75f);

        jumpRequest = false;
        keepAirXVelocity = true;
        StateChange(2);

        if (rb != null)
        {
            float jumpDirection = GetJumpDirection(jumpBlock);
            rb.linearVelocity = new Vector2(jumpDirection * playerJumpForwardPower, playerJumpPower);
        }

        yield return new WaitUntil(() => !isGrounded);
        yield return new WaitUntil(() => jumpRequest || isGrounded);

        StateChange(1);
        keepAirXVelocity = false;
        isJumping = false;
    }

    float GetJumpDirection(Transform jumpBlock)
    {
        if (jumpBlock == null) return direction;

        float jumpDirection = Mathf.Sign(jumpBlock.position.x - transform.position.x);
        if (jumpDirection == 0) jumpDirection = direction;

        return jumpDirection;
    }

    IEnumerator WalkToBlockCenter(Transform jumpBlock)
    {
        if (jumpBlock == null)
        {
            jumpCanceled = true;
            yield break;
        }

        state = moveState.idol;
        anim.SetBool("isWalk", true);

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

        transform.position = new Vector3(jumpBlock.position.x, transform.position.y, transform.position.z);
        StateChange(0);
    }

    void CheckGround()
    {
        Vector2 rayOrigin = transform.position + Vector3.down * 0.2f;
        // BoxCastを使用し地面があるかを確認
        RaycastHit2D hit = Physics2D.BoxCast(rayOrigin, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = (hit.collider != null);
        currentGround = hit.collider;

        Debug.DrawRay(rayOrigin, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    public bool IsOneBlockHigherThanCurrentGround(Transform target, float oneBlockHeight, float tolerance)
    {
        if (currentGround == null) return false;

        float heightDifference = target.position.y - currentGround.transform.position.y;
        return Mathf.Abs(heightDifference - oneBlockHeight) <= tolerance;
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

        if (collision.gameObject.CompareTag("Ground"))
        {
            jumpRequest = true;
        }
    }

    public void ResetDirection()
    {
        direction = 1; // 右向きに戻す
        jumpRequest = true;
        isJumping = false;
        jumpCanceled = true;
        keepAirXVelocity = false;
    }
}
