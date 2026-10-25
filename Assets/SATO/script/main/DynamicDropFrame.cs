// DynamicDropFrame.cs を修正

using UnityEngine;

public class DynamicDropFrame : MonoBehaviour
{
    // ★追加：右向きか左向きかを選べる設定
    public enum FrameDirection { Right = 1, Left = -1 }

    [Header("ゴースト実演の向き")]
    [Tooltip("この枠に置いた動物がどっちに動くかを指定できます")]
    public FrameDirection previewDirection = FrameDirection.Right;

    [Header("アニメーション（ぬるっと）設定")]
    public float lerpSpeed = 8.0f;

    private Vector3 initialScale;
    private Vector3 targetScale;

    void Start()
    {
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
            initialScale.x * 0.75f * multiplier.x,
            initialScale.y * 0.75f * multiplier.y,
            initialScale.z * 0.75f * multiplier.z
        );
    }

    public void ResetScale()
    {
        targetScale = initialScale;
    }
}