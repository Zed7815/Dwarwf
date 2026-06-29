using UnityEngine;

public class FlowerGimic : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (CompareTag("Player"))
        {
            Destroy(collision.gameObject);
        }
    }
}

