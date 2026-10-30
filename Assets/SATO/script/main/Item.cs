using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour
{
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

        // ★過去に獲得済みなら最初から消す
        if (IsAlreadySavedCollected())
        {
            ForceCollectedState();
        }
        else
        {
            // 未獲得なら確実に表示
            gameObject.SetActive(true);
            if (col != null) col.enabled = true;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
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

            if (GameManager.instance != null)
            {
                GameManager.instance.AddItem();
            }

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

    // ★修正：他のステージやイージー/ハードと絶対に混ざらない独立判定
    public bool IsAlreadySavedCollected()
    {
        GoalPoint goal = FindObjectOfType<GoalPoint>();
        if (goal != null)
        {
            // 巣の番号 ＋ 難易度 の専用キーだけを見る（古い番号は見ない！）
            string key = $"Nest_{goal.nestIndex}_{(goal.isEasyStage ? "Easy" : "Hard")}_Star";
            bool hasStar = PlayerPrefs.GetInt(key, 0) == 1;

            Debug.Log($"<color=cyan>【星の出現判定】参照キー: {key} | 保存値: {PlayerPrefs.GetInt(key, 0)} ➔ {(hasStar ? "★獲得済みのため非表示" : "未獲得のため【星を表示】")}</color>");

            return hasStar;
        }

        return false;
    }

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

        if (IsAlreadySavedCollected())
        {
            ForceCollectedState();
            return;
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

        gameObject.SetActive(true);
    }
}