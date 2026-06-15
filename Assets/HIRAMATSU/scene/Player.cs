using UnityEngine;
using System.Collections;
public enum action
{
    Stay,
    Walk,
    Agojump,
    Jump
}
public class Player : MonoBehaviour
{
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    public bool jumpRequest = true;
    public bool agojumpRequest = false;
    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float PlayerJumpPower=5.0f;
    public int direction = 1;
    public float playerJumpPower = 10f;
    public action action = action.Walk;
    // Update is called once per frame
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }
    void Update()
    {
        if (action.Walk==action)
        {
            Walk();
        }
        if (agojumpRequest)
        {
            StartCoroutine(AgoJump());
            agojumpRequest = false; // 実行したら消す
        }
    }
    void stay()
    {

    }
    void Walk()
    {
        transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime* direction);
    }
    IEnumerator Jump()
    {
        yield return new WaitForSeconds(0.75f);
        jumpRequest = true;
        rb.linearVelocity = new Vector2(direction * 1f, playerJumpPower);
        action = action.Walk;
    }
    IEnumerator AgoJump()
    {

        Vector3 startPos = transform.position;

        float duration = 1f;//ジャンプの時間
        float height = 1.5f;  // ジャンプの高さ
        float distance = 4f; // 前に進む距離

        // 0.5までにすると放物線の頂点で終わる
        for (float x = 0; x <= 0.5f; x += Time.deltaTime / duration)
        {
            float y = 4 * height * x * (1 - x);

            transform.position = startPos +
                new Vector3(distance * direction * x, y, 0);

            yield return null;
        }

        action = action.Agojump;
    }

    public void actionchange(int n)
    {
        //Stay,
        //Walk,
        //Agojump,
        //Jump
        switch (n)
        { 
            case 0: action = action.Stay; break;
            case 1: action = action.Walk; break;
            case 2: agojumpRequest = true; break;
            case 3: action = action.Jump; break;
        }

    }


    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            direction *= -1;

            Vector3 scale = transform.localScale;
            transform.localScale = scale;
            scale.x = -1;
            sr.flipX = !sr.flipX;
        }
        if(collision.gameObject.CompareTag("bane"))
        {
            if(jumpRequest)
            {
                jumpRequest = false;
            StartCoroutine(Jump());
            }
        }
    }
}
