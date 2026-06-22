using UnityEngine;
using System.Collections;

public class AutoJump : MonoBehaviour
{
    public Player_walk pl;
    private bool isProcessing = false; // 二重発動防止

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 設置されたブロックかつ、まだ処理中でなければ
        if (collision.CompareTag("PlacedBlock") && !isProcessing)
        {
            // バネに触れたら専用シーケンスを開始
            StartCoroutine(HandleSpring(collision.transform));
        }
    }

    IEnumerator HandleSpring(Transform spring)
    {
        isProcessing = true;
        yield return StartCoroutine(pl.SpringSequence(spring));
        isProcessing = false;
    }
}
