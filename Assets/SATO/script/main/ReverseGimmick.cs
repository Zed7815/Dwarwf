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
        while (Mathf.Abs(p.transform.position.x - centerX) > 0.1f)
        {
            // プレイヤーがが中心に近づくのを待つ
            yield return null;
        }

        // 中心地に達した場合に停止
        p.transform.position = new Vector3(centerX,p.transform.position.y,p.transform.position.z);
        p.StateChange(0); // 停止

        Rigidbody2D rb = p.GetComponent<Rigidbody2D>();

        if (rb != null) rb.linearVelocity = Vector3.zero;

        // 立ち止まり時のタメ
        yield return new WaitForSeconds(0.8f);

        // 向きの変更
        p.direction *= -1; // 内部の進行方向
        Vector3 s = p.transform.localScale;
        s.x *= -1;
        p.transform.localScale = s; // 見た目の反転

        // 少しのタメの後に再度歩き出す
        yield return new WaitForSeconds(0.35f);
        p.StateChange(1);

        yield return new WaitForSeconds(0.5f);
        isProcessing = false;


    }
}
