using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("設定")]
    public int keyID;
    public AudioClip collectSE;
    public GameObject collectEffect;

    private SpriteRenderer sr;
    private Collider2D col;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            FinalKeyManager.instance.CollectKey(keyID, transform.position);

            if (collectSE) AudioSource.PlayClipAtPoint(collectSE, transform.position);
            if (collectEffect) Instantiate(collectEffect, transform.position, Quaternion.identity);

            // ★【修正】SetActive(false)ではなく、見た目と判定だけ消す
            if (sr) sr.enabled = false;
            if (col) col.enabled = false;
        }
    }

    // リセットボタンが押された時に呼ばれる
    void OnGimmickReset()
    {
        // 見た目と判定を復活させる
        if (sr) sr.enabled = true;
        if (col) col.enabled = true;
    }
}