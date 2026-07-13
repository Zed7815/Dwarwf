using UnityEngine;
using System.Collections;
public class trin : MonoBehaviour
{

    public float minFallSpeed = 1.5f;
    public float maxFallSpeed = 3.0f;

    public float minSwaySpeed = 1.0f;
    public float maxSwaySpeed = 3.0f;

    public float minSwayAmount = 0.2f;
    public float maxSwayAmount = 0.8f;

    public float minRotateSpeed = 20f;
    public float maxRotateSpeed = 80f;

    private float fallSpeed;
    private float swaySpeed;
    private float swayAmount;
    private float rotateSpeed;

    private float startX;
    private float offset;

    void Start()
    {
        startX = transform.position.x;
        StartCoroutine(die());
        // ランダムに設定
        fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);
        swaySpeed = Random.Range(minSwaySpeed, maxSwaySpeed);
        swayAmount = Random.Range(minSwayAmount, maxSwayAmount);
        rotateSpeed = Random.Range(minRotateSpeed, maxRotateSpeed);

        // 揺れ始めるタイミングをランダムにする
        offset = Random.Range(0f, Mathf.PI * 2);
    }

    void Update()
    {
        // 左右にゆらゆら
        float x = startX + Mathf.Sin(Time.time * swaySpeed + offset) * swayAmount;

        // 下へ落ちる
        float y = transform.position.y - fallSpeed * Time.deltaTime;

        transform.position = new Vector3(x, y, transform.position.z);

        // ゆっくり回転
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
    IEnumerator die()
    {
        yield return new WaitForSeconds(7f);
        Destroy(gameObject);
    }
}