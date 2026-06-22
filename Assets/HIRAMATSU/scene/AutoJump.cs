using UnityEngine;

public class AutoJump : MonoBehaviour
{
    public Player pl;
    public Transform targetPoint;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("PlacedBlock") && pl.jumpRequest)
        {
            targetPoint = collision.transform;
            pl.StartCoroutine(pl.AutoJump(targetPoint));
        }
    }
}