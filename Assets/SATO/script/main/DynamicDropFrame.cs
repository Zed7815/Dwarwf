using UnityEngine;

public class DynamicDropFrame : MonoBehaviour
{
    [Header("アニメーション（ぬるっと）設定")]
    [Tooltip("スケールが目標サイズに変化する速度（値が大きいほど素早く、小さいほどゆっくりぬるっと動きます）")]
    public float lerpSpeed = 8.0f;

    private Vector3 initialScale;
    private Vector3 targetScale;

    void Start()
    {
        // 初期スケールを正確に記憶（枠のデフォルトの大きさを保持）
        initialScale = transform.localScale;
        targetScale = initialScale;
    }

    void Update()
    {
        // 毎フレーム、現在のスケールを目標スケールにぬるっと（Lerp）近づける
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        }
    }

    /// 指定された倍率（乗数）に枠を広げる
    public void Expand(Vector3 multiplier)
    {
        // 初期サイズに倍率を掛け合わせて、ピッタリと動物サイズに合わせる
        targetScale = new Vector3(
            initialScale.x * 0.75f * multiplier.x,
            initialScale.y * 0.75f * multiplier.y,
            initialScale.z * 0.75f * multiplier.z
        );
    }

    /// 枠を元の初期サイズに戻す

    public void ResetScale()
    {
        targetScale = initialScale;
    }
}