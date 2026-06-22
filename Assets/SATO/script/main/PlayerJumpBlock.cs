using UnityEngine;

public class PlayerJumpBlock : MonoBehaviour
{
    public float topCheckTolerance = 0.1f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player_walk player = collision.gameObject.GetComponent<Player_walk>();

            if (player != null && player.gameObject.activeInHierarchy && IsPlayerOnTop(collision.collider))
            {
                player.StartCoroutine(player.Jump(transform));
            }
        }
    }

    bool IsPlayerOnTop(Collider2D playerCollider)
    {
        Collider2D blockCollider = GetComponent<Collider2D>();

        if (playerCollider == null || blockCollider == null) return false;

        float playerBottom = playerCollider.bounds.min.y;
        float blockTop = blockCollider.bounds.max.y;

        return playerBottom >= blockTop - topCheckTolerance;
    }
}
