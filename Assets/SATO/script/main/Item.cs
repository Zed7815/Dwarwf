using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
    private bool isCollected = false;
    private Vector3 originPos;
    private SpriteRenderer sr;
    private Collider2D col;

    void Awake()
    {
        originPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        // すでにクリア済み（セーブデータがある）かチェック
        if (GameManager.instance != null)
        {
            int alreadyGot = PlayerPrefs.GetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 0);
            if (alreadyGot == 1)
            {
                // ★修正：半透明ではなく、オブジェクト自体を非表示にする
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isCollected)
        {
            isCollected = true;
            GameManager.instance.AddItem();
            StartCoroutine(CollectRoutine());
        }
    }

    IEnumerator CollectRoutine()
    {
        if (col != null) col.enabled = false;
        Vector3 startPos = transform.position;
        float duratin = 0.5f;
        float timer = 0f;

        while (timer < duratin)
        {
            timer += Time.deltaTime;
            float progress = timer / duratin;
            transform.position = startPos + new Vector3(0, progress * 1.0f, 0);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f - progress;
                sr.color = c;
            }
            yield return null;
        }
        // 拾った演出の後に非表示にする
        gameObject.SetActive(false);
    }

    // リセットボタンなどでオブジェクトが再表示された時の処理
    private void OnEnable()
    {
        // ★重要：以前のプレイですでに取得済みなら、表示されようとしても即座に消す
        if (GameManager.instance != null)
        {
            int alreadyGot = PlayerPrefs.GetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 0);
            if (alreadyGot == 1)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        // 今回のプレイで初めて拾う場合のリセット処理
        isCollected = false;
        transform.position = originPos;
        if (col != null) col.enabled = true;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }
}