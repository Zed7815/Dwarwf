using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("設定")]
    public int keyID; // 0なら鍵A、1なら鍵B
    public AudioClip collectSE;
    public GameObject collectEffect;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // KeyManagerに通知
            FinalKeyManager.instance.CollectKey(keyID, transform.position);

            // 効果音やエフェクト
            if (collectSE) AudioSource.PlayClipAtPoint(collectSE, transform.position);
            if (collectEffect) Instantiate(collectEffect, transform.position, Quaternion.identity);

            // 消去（リセット対応のために非表示にするだけ）
            gameObject.SetActive(false);
        }
    }

    // リセット時に呼ばれる（GimmickResetterから呼ばれる想定）
    void OnGimmickReset()
    {
        gameObject.SetActive(true);
    }
}