using UnityEngine;

public class DynamicDropFrame : MonoBehaviour
{
    public enum FrameDirection { Right = 1, Left = -1 }

    [Header("ゴースト実演の共通設定")]
    [Tooltip("この枠に置いた動物が動く方向")]
    public FrameDirection previewDirection = FrameDirection.Right;

    [Header("★鳥（Bird）専用の設定")]
    [Tooltip("ONにすると、この枠に置いた鳥が実際に飛ぶ実演を行います")]
    public bool enableBirdFlight = true; // ★動かすかどうかのスイッチ！
    [Tooltip("鳥が何マス（単位）飛ぶか")]
    public float birdFlightDistance = 4.0f;

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