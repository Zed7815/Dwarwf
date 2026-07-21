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

            // ★重要：オブジェクトを消さずに、見た目と判定だけ消す
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
        }
    }

    // リセットボタンが押された時に呼ばれる
    void OnGimmickReset()
    {
        // 見た目と判定を復活させる
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;
    }
}