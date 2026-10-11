using UnityEngine;
using UnityEngine.SceneManagement;

public enum GimmickType { Generic, Spider, Bird, Lift, Warp, Vanishing, Spring, FastForward }

public static class SceneLoader
{
    public static string nextSceneName;
    public static GimmickType nextGimmickType;

    public static void Load(string sceneName, GimmickType gimmick = GimmickType.Generic)
    {
        nextSceneName = sceneName;
        nextGimmickType = gimmick;

        // 時間を正常に戻す
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        Debug.Log($"<color=cyan>【ダイレクト遷移】{sceneName} へ直接移動します</color>");

        // 直接シーンを読み込む！
        SceneManager.LoadScene(sceneName);
    }
}