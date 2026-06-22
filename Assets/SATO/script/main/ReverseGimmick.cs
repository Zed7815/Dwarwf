using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public class ReverseGimmick : MonoBehaviour
{
    private bool isProcessing = false;

    private void OnTriggerEnter2D(Collider2D trigger)
    {
        if (isProcessing) return;

        if (trigger.gameObject.CompareTag("Player"))
        {
            Player_walk p = trigger.gameObject.GetComponent<Player_walk>();

            if (p!= null)
            {
                StartCoroutine(ReverseSequence(p));
            }
        }
    }

    IEnumerator ReverseSequence(Player_walk p)
    {
        isProcessing = true;

        // 中心にプレイヤーが来るまでループ
        float centerX = transform.position.x;
        // 中心に十分近づくまで待つ
        while (Mathf.Abs(p.transform.position.x - centerX) > 0.2f)
        {
            yield return null;
        }

        // ぴったり中心に合わせる
        p.transform.position = new Vector3(centerX, p.transform.position.y, p.transform.position.z);

        // 物理速度を完全にゼロにする
        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();

        if (rb != null) rb.linearVelocity = Vector2.zero;

        p.StateChange(0); // idol
        yield return new WaitForSeconds(0.8f);

        // 向き反転
        p.direction *= -1;
        Vector3 s = p.transform.localScale;
        s.x = Mathf.Abs(s.x) * p.direction; // directionに基づいた絶対的な向き設定
        p.transform.localScale = s;

        yield return new WaitForSeconds(0.35f);
        p.StateChange(1); // straight

        yield return new WaitForSeconds(0.5f);
        isProcessing = false;
    }
}
