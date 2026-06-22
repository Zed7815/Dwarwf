using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    public bool jumpRequest = true;
    public bool isWalk = false;
    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float PlayerJumpPower=5.0f;
    public int direction = 1;
    public float playerJumpPower = 10f;
    // Update is called once per frame
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        Walk();
    }
    public IEnumerator AutoJump(Transform target)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = target.position + Vector3.up * 1.2f; // ゴール

        float duration = 0.5f;
        float jumpHeight = 2f; // 放物線の高さ

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // 開始→終了を補間
            Vector3 pos = Vector3.Lerp(startPos, endPos, t);

            // 放物線を追加
            pos.y += 1 * jumpHeight * t * (1 - t);

            transform.position = pos;

            yield return null;
        }

        transform.position = endPos;
    }
    void Walk()
    {
        if(isWalk)
        {
            transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime * direction);
        }
    }
    public IEnumerator Jump()
    {
        isWalk = false;
        yield return new WaitForSeconds(0.75f);
        isWalk = true;
        jumpRequest = false;
        rb.linearVelocity = new Vector2(direction * 1f, playerJumpPower);
        yield return new WaitUntil(() => jumpRequest);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            direction *= -1;

            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
        if(collision.gameObject.CompareTag("Ground"))
        {
            jumpRequest = true;
        }
    }
}
