using UnityEngine;
public enum action
{
    Walk,
    jump
}
public class Player : MonoBehaviour
{
    private SpriteRenderer sr;
    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float PlayerJumpPower=5.0f;
    public int direction = 1;
    public action action = action.Walk;
    // Update is called once per frame
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        if (action.Walk==action)
        {
            Walk();
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
            if(sr.flipX == true)
            {
                sr.flipX = false;
            }
            else if(sr.flipX == false)
            {
                sr.flipX = true;
            }
        }
    }
}
