using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("プレイヤーの数値")]
    public float PlayerSpeed = 5.0f;
    public float PlayerJumpPower=5.0f;
    public int direction = 1;
       // Update is called once per frame
    private void Start()
    {
        
    }
    void Update()
    {
        Walk();
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
