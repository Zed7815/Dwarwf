using UnityEngine;
using System.Collections;

public class trin : MonoBehaviour
{
    [Header("落下・揺れ設定")]
    public float minFallSpeed = 1.0f;
    public float maxFallSpeed = 2.5f;
    public float minSwayAmount = 0.3f;
    public float maxSwayAmount = 1.2f;

    [Header("自機回避（かき分け）設定")]
    public Transform avoidTarget; // 生成時にtirasiから受け取る
    [Tooltip("どれくらい近づいたら避け始めるか")]
    public float avoidRadius = 3.0f;
    [Tooltip("避ける力の強さ")]
    public float avoidStrength = 2.0f;

    private float fallSpeed;
    private float swaySpeed;
    private float swayAmount;
    private float rotateSpeed;
    private float startX;
    private float offset;
    private float currentAvoidOffset = 0f; // 避け続けている距離の蓄積
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startX = transform.position.x;

        fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);
        swaySpeed = Random.Range(0.8f, 2.0f);
        swayAmount = Random.Range(minSwayAmount, maxSwayAmount);
        rotateSpeed = Random.Range(10f, 50f);
        offset = Random.Range(0f, Mathf.PI * 2);

        StartCoroutine(FadeAndDie());
    }

    void Update()
    {
        // --- 自機を避けるロジック ---
        if (avoidTarget != null)
        {
            // 自機との距離を計算
            float dist = Vector2.Distance(transform.position, avoidTarget.position);

            if (dist < avoidRadius)
            {
                // 近いほど強く、遠いほど弱く避ける
                float pushForce = (1.0f - (dist / avoidRadius)) * avoidStrength;

                // 自機の左にいたら左へ、右にいたら右へ
                float direction = transform.position.x < avoidTarget.position.x ? -1f : 1f;

                // 避け値を蓄積させていく（一度避けたらずれたまま落ちる）
                currentAvoidOffset += direction * pushForce * Time.deltaTime;
            }
        }

        // --- 移動の計算 ---
        // 揺れ ＋ 避け値 ＋ 初期位置
        float x = startX + Mathf.Sin(Time.time * swaySpeed + offset) * swayAmount + currentAvoidOffset;
        float y = transform.position.y - fallSpeed * Time.deltaTime;

        transform.position = new Vector3(x, y, transform.position.z);
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }

    IEnumerator FadeAndDie()
    {
        float lifeTime = 7f;
        yield return new WaitForSeconds(lifeTime * 0.8f);
        float fadeTime = lifeTime * 0.2f;
        float elapsed = 0;
        Color initialColor = sr != null ? sr.color : Color.white;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            if (sr != null)
            {
                float alpha = Mathf.Lerp(initialColor.a, 0, elapsed / fadeTime);
                sr.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}