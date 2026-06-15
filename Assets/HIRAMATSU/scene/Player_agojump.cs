using UnityEngine;

public class Player_jump : MonoBehaviour
{
    public Player player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("bane"))
        {
            Debug.Log("‚¤‚È‚¬ƒpƒC");
            player.actionchange(2);
        }
    }
}
