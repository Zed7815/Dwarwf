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

    }
}
