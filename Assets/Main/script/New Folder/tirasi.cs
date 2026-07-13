using UnityEngine;
using System.Collections;

public class tirasi : MonoBehaviour
{
    public GameObject prefab;

    [Header("スポーン設定")]
    [Tooltip("1秒間に何枚の葉っぱを降らせるか")]
    public float spawnRate = 5f;
    public float minX = -12f;
    public float maxX = 12f;
    public float spawnY = 7f;

    [Header("自機回避設定")]
    [Tooltip("避ける対象（自機など）")]
    public Transform avoidTarget;

    void Start()
    {
        StartCoroutine(Spawn());
    }

    IEnumerator Spawn()
    {
        while (true)
        {
            // spawnRateに合わせて待ち時間を計算 (例: Rateが5なら0.2秒ごと)
            float interval = 1f / Mathf.Max(0.1f, spawnRate);
            yield return new WaitForSeconds(interval);

            float randomX = Random.Range(minX, maxX);
            Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

            GameObject leaf = Instantiate(prefab, spawnPos, Quaternion.identity);

            // 葉っぱに避ける対象を教えてあげる
            trin leafScript = leaf.GetComponent<trin>();
            if (leafScript != null)
            {
                leafScript.avoidTarget = avoidTarget;
            }
        }
    }

    void OnGimmickReset()
    {
        trin[] leaves = FindObjectsOfType<trin>();
        foreach (trin leaf in leaves) Destroy(leaf.gameObject);
    }
}