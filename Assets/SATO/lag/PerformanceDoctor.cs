using UnityEngine;

public class PerformanceDoctor : MonoBehaviour
{
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 320, 140), "=== パフォーマンス診断 ===");

        float fps = 1.0f / Time.unscaledDeltaTime;
        GUI.Label(new Rect(20, 35, 300, 20), $"現在のFPS: {Mathf.RoundToInt(fps)}");
        GUI.Label(new Rect(20, 55, 300, 20), $"Time.timeScale: {Time.timeScale}");
        GUI.Label(new Rect(20, 75, 300, 20), $"Time.fixedDeltaTime: {Time.fixedDeltaTime}");

        int allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        GUI.Label(new Rect(20, 95, 300, 20), $"シーン内のオブジェクト数: {allObjects} 個");

        if (Time.fixedDeltaTime < 0.015f && Time.timeScale == 1.0f)
        {
            GUI.Label(new Rect(20, 115, 300, 20), "<color=red>【警告】fixedDeltaTimeが小さすぎて暴走中！</color>");
        }
    }
}