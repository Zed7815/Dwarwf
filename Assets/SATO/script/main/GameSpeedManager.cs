using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSpeedManager : MonoBehaviour
{
    public static GameSpeedManager instance;

    [Header("ベース速度設定")]
    public float baseSpeed = 1.2f;
    public float fastForwardMultiplier = 3.0f;

    private bool isFastForwardMode = false;
    private const float originalFixedDeltaTime = 0.02f; // Unityの標準値

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            // 最初から速度を適用
            UpdateGameSpeed();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Loading" || scene.name == "Ending" || scene.name == "Title" || scene.name == "StageSelect")
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            Debug.Log($"[SpeedManager] {scene.name} なので速度を1.0に固定しました");
        }
        else
        {
            UpdateGameSpeed();
        }
    }

    public void UpdateGameSpeed()
    {
        float targetTimeScale = baseSpeed * (isFastForwardMode ? fastForwardMultiplier : 1.0f);

        // 1. 全体の時間を設定
        Time.timeScale = targetTimeScale;

        // 2. ★修正：物理演算の頻度を固定する
        // 以前の「0.02 * timeScale」だと物理の精度が落ちてカクつきます。
        // ここを 0.02（一定）に保つことで、加速中も物理演算が滑らかになります。
        Time.fixedDeltaTime = originalFixedDeltaTime;

        Debug.Log($"[SpeedManager] 速度適用: {Time.timeScale}倍");
    }

    public void SetFastForward(bool active)
    {
        isFastForwardMode = active;
        UpdateGameSpeed();
    }

    public void ResetToBaseSpeed()
    {
        isFastForwardMode = false;
        UpdateGameSpeed();
    }
}