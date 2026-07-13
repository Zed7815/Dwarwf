using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
    private bool isCollected = false;
    private Vector3 originPos;
    private SpriteRenderer sr;
    private Collider2D col;

    [Header("SE設定")]
    public AudioSource audioSource; // インスペクターで割り当てるか自動取得
    public AudioClip collectSE;    // 拾った時の音

    void Awake()
    {
        originPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        // AudioSourceが未設定なら自分から取得を試みる
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // すでにクリア済み（セーブデータがある）かチェック
        if (GameManager.instance != null)
        {
            int alreadyGot = PlayerPrefs.GetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 0);
            if (alreadyGot == 1)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isCollected)
        {
            isCollected = true;

            // SE再生：拾った瞬間
            if (audioSource != null && collectSE != null)
            {
                audioSource.PlayOneShot(collectSE);
            }

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
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.instance != null)
        {
            int alreadyGot = PlayerPrefs.GetInt("StarCollected_Stage_" + GameManager.instance.stageNumber, 0);
            if (alreadyGot == 1)
            {
                gameObject.SetActive(false);
                return;
            }
        }

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