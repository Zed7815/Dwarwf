using UnityEngine;
using UnityEngine.SceneManagement;

// Åö Spring Çí«â¡
public enum GimmickType { Generic, Spider, Bird, Lift, Warp, Vanishing, Spring }

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