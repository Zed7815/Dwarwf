using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
    // ★外部から確実に獲得状態を判定・操作できるように公開
    public bool isCollected = false;

    private Vector3 originPos;
    private SpriteRenderer sr;
    private Collider2D col;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip collectSE;

    void Awake()
    {
        originPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

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
        float duration = 0.5f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
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

    // ★チェックポイント復元用：獲得済み状態を強制適用
    public void ForceCollectedState()
    {
        StopAllCoroutines();
        isCollected = true;
        if (col != null) col.enabled = false;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
        }
        gameObject.SetActive(false);
    }

    public void OnGimmickReset()
    {
        StopAllCoroutines();
        isCollected = false;
        transform.position = originPos;

        if (col != null) col.enabled = true;
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        gameObject.SetActive(true);
    }
}