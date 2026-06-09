using UnityEngine;
public enum moveState
{
    idol,
    straight,
    jump,

}
public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float PlayerJumpPower=5.0f;
    public int direction = 1;
    moveState state = moveState.straight;
    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case moveState.idol:
                stay();
                break;

            case moveState.straight:
                walk();
                break;

            case moveState.jump:
                jump();
                break;

        }
        if()
        {

        }
    }
    void stay()
    {

    }
    void walk()
    {
        transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime* direction);
    }
    void jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, PlayerJumpPower);
    }



    public void StateChange(int  n)
    {
        switch (n)
        {
            case 0:
                state = moveState.idol;
                break;

            case 1:
                state = moveState.straight;
                break;
            case 2:
                state = moveState.jump;
                break;
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            direction *= -1;

            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
}
