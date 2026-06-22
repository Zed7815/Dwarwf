using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public Player player;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player.StartCoroutine(player.Jump());
        }
    }
}
