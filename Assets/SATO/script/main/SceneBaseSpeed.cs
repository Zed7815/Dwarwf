using UnityEngine;

public class SceneBaseSpeed : MonoBehaviour
{
    [Header("このシーンの基本速度")]
    [Tooltip("1.0が通常。1.2にすると20%速くなります")]
    public float baseSpeedMultiplier = 1.2f;

    private float originalFixedDeltaTime;

    void Awake()
    {
        // Unity標準の物理演算間隔を覚えておく
        originalFixedDeltaTime = 0.02f;
    }

    void OnEnable()
    {
        ApplySpeed();
    }

    void Start()
    {
        ApplySpeed();
    }

    void ApplySpeed()
    {
        // このシーンに入った瞬間に速度を適用
        Time.timeScale = baseSpeedMultiplier;

        // 物理演算がガタガタ震えないように同期させる（ここが重要）
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

        Debug.Log($"[SceneSpeed] このシーンの速度を {baseSpeedMultiplier} 倍に設定しました");
    }

    void OnDisable()
    {
        // ★重要：このシーンを離れる（またはリセットされる）時に、必ず等倍に戻す
        // これを忘れると、次のシーンやロード画面まで速くなってしまいます
        ResetSpeed();
    }

    void OnDestroy()
    {
        // オブジェクトが破棄される際にも確実にリセット
        ResetSpeed();
    }

    private void ResetSpeed()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        Debug.Log("[SceneSpeed] 速度を 1.0 に戻しました");
    }
}