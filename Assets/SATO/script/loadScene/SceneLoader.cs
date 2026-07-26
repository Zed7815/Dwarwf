using UnityEngine;
using UnityEngine.SceneManagement;

// ギミックの種類を定義
public enum GimmickType { Generic, Spider, Bird, Lift, Warp, Vanishing }

public static class SceneLoader
{
    public static string nextSceneName;
    public static GimmickType nextGimmickType;

    // 読み込む時にギミックの種類も指定する
    public static void Load(string sceneName, GimmickType gimmick)
    {
        nextSceneName = sceneName;
        nextGimmickType = gimmick;
        SceneManager.LoadScene("Loading");
    }
}