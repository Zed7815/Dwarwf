using UnityEngine;
using System.Collections;

public class tirasi : MonoBehaviour
{
    public GameObject prefab;// trinのついてる葉っぱのPrefab

    public float minX = -8f;
    public float maxX = 8f;
    public float spawnY = 5f;

    void Start()
    {
        StartCoroutine(Spawn());
    }

    IEnumerator Spawn()
    {
        while (true)
        {
            float wait = Random.Range(0.4f, 0.2f);
            yield return new WaitForSeconds(wait);

            float randomX = Random.Range(minX, maxX);
            Vector3 spawnPos = new Vector3(randomX, spawnY, 0);

            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }
}