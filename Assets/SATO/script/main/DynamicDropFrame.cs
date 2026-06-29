using Unity.VisualScripting;
using UnityEngine;

public class DynamicDropFrame : MonoBehaviour
{
    [Header("アニメーション設定")]
    [Tooltip("スケールが変わる速度")]
    public float lerpSpeed = 8.0f;

    private Vector3 initialScale;
    private Vector3 targetScale;

    void Start()
    {
        // 初期スケールを保存
        initialScale = transform.localScale;
        targetScale = initialScale;
    }

    void Update()
    {
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        }
    }

    public void Expand(Vector3 multiplier)
    {
        targetScale = new Vector3(
              initialScale.x * multiplier.x,
            initialScale.y * multiplier.y,
            initialScale.z * multiplier.z);
      
    }

    public void ResetScale()
    {
        targetScale = initialScale;
    }
}
