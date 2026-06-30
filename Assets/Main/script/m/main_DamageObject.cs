using UnityEngine;
using System.Collections;

public class main_DamageObject : MonoBehaviour
{
    private bool isDead = false; // 二重発動防止

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("Player"))
        {
            Player_walk player = collision.GetComponent<Player_walk>();

            if (player != null)
            {
                StartCoroutine(DeathSequence(player));
            }
        }
    }

    IEnumerator DeathSequence(Player_walk p)
    {
        isDead = true;

        // 動きの停止
        p.StateChange(0); // idol
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 震えと色
        SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
        Vector3 originalPos = p.transform.position;
        float duration = 0.25f; // 演出時間
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 左右に細かく震わせる
            float shakeX = Mathf.Sin(Time.time * 50f) * 0.1f;
            p.transform.position = originalPos + new Vector3(shakeX, 0, 0);

            // 赤色と白色を交互に
            if (sr != null)
            {
                sr.color = (Mathf.FloorToInt(Time.time * 15f) % 2 == 0) ? Color.red : Color.white;
            }
            yield return null;
        }

        // 死ぬまでの一瞬の間
        if (sr != null) sr.color = Color.white; // 色リセット
        yield return new WaitForSeconds(0.2f);

        // ゲームマネージャーのリセット呼び出し
        GameManager.instance.ResetGame();

        isDead = false;
    }
}
