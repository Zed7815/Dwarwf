using UnityEngine;
using System.Collections;

public class AutoJumpp : MonoBehaviour
{
    public Player_walk pl;
    private bool isProcessing = false; // 二重発動防止

    void Start()
    {
        if (pl == null)
        {
            pl = FindObjectOfType<Player_walk>();
        }
    }

    void Update()
    {
        if (pl == null) return;
        if (!pl.jumpRequest) return;

        // チェック位置（プレイヤーの前方・下半分）
        Vector2 checkPos = (Vector2)pl.transform.position + new Vector2(
            pl.direction * 0.8f,
            -0.3f
        );

        // デバッグ用
        Debug.DrawLine(checkPos, checkPos + Vector2.up * 0.3f, Color.cyan, 0.1f);

        // チェック
        Collider2D hit = Physics2D.OverlapPoint(checkPos);

        if (hit != null && hit.CompareTag("PlacedBlock"))
    {
            pl.StartCoroutine(pl.AutoJump(hit.transform));
        }
    }
}
