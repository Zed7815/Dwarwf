using UnityEngine;

public class Player_walk : MonoBehaviour
{
    private Animator anim;

    public enum moveState
    {
        idol,
        straight,
        jump
    }

    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public int direction = 1;
    private moveState state = moveState.idol;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        switch (state)
        {
            case moveState.idol:
                anim.SetBool("isWalk", false);
                stay();
                break;

            case moveState.straight:
                anim.SetBool("isWalk", true);
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
        transform.Translate(Vector2.right * PlayerSpeed * Time.deltaTime* direction);
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
                anim.SetBool("isWalk", true);
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
    }

    public void ResetDirection()
    {
        direction = 1; // 右向きに戻す
    }
}