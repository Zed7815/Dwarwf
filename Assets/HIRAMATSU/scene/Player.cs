using UnityEngine;
public enum action
{
    Walk,
    jump
}
public class Player : MonoBehaviour
{
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    public bool jumpRequest = false;
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
        if (jumpRequest)
        {
            jump();
            jumpRequest = false; // 実行したら消す
        }
    }
    void stay()
    {

    }
    void Walk()
    {
        transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime* direction);
    }
    void jump()
    {
        rb.AddForce(Vector2.up * PlayerJumpPower, ForceMode2D.Impulse);
    }

    void actionchange(int n)
    {
        switch (n)
        { 
            
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
    }
}
