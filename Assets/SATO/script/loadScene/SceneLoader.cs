using UnityEngine;
using UnityEngine.SceneManagement;

// ★ Spring を追加
public enum GimmickType { Generic, Spider, Bird, Lift, Warp, Vanishing, Spring, FastForward }

public static class SceneLoader
{
    public static string nextSceneName;
    public static GimmickType nextGimmickType;

    public static void Load(string sceneName, GimmickType gimmick)
    {
        nextSceneName = sceneName;
        nextGimmickType = gimmick;
        SceneManager.LoadScene("Loading");
    }
}