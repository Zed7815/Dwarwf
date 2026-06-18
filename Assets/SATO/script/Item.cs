using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
    private bool isCollected = false; // 二重取得防止
    private Vector3 originPos; // 初期地点を記憶

    void Awake()
    {
        originPos = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // プレイヤーが触れたかの判定
        if (collision.CompareTag("Player") && !isCollected)
        {
            isCollected = true; // 取った際にフラグを立てる

            // GameManagerにアイテム取得を伝える
            GameManager.instance.AddItem();


            StartCoroutine(CollectRoutine());
        }
    }

    IEnumerator CollectRoutine()
    {
        // 当たり判定を消す
        GetComponent<Collider2D>().enabled = false;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        Vector3 startPos = transform.position;
        float duratin = 0.5f; // 演出にかける時間

        float timer = 0f;

        while (timer < duratin)
        {
            timer += Time.deltaTime;
            float progress = timer / duratin; // 0.0から1.0まで進

            // 上に移動させる演出
            // 0.5秒かけて、足元の位置から上に1.0進む
            transform.position = startPos + new Vector3(0, progress * 1.0f, 0);

            // 星を透明化
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - progress; // 半透明から透明へ

                sr.color = c;
            }
            
            yield return null; // 位置フレーム待機
        }

        // 最後非表示に
        gameObject.SetActive(false);
 
    }

    // アイテムが表示されたときに、色や当たり判定をリセット
    private void OnEnable()
    {
        isCollected = false;
        transform.position = originPos;
        GetComponent<Collider2D>().enabled = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f; // 不透明に戻す
            sr.color= c;
        }
    }
}
