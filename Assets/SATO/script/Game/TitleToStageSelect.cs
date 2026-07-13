using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class TitleToStageSelect : MonoBehaviour
{
    [Header("設定")]
    public string targetSceneName = "StageSelect";
    public nextscene fadeOutScript;

    [Header("データリセット設定")]
    [Tooltip("チェックを入れると、移動時にセーブデータをすべて削除します（エンディング用）")]
    public bool resetData = false;

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip decisionSE;

    private bool isTransitioning = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!isTransitioning && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(TransitionRoutine());
        }
    }

    IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        // ★追加：データリセット処理
        if (resetData)
        {
            Debug.Log("セーブデータをリセットして遷移します");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // ステージセレクトの初回起動フラグもリセット（もしあれば）
            // static変数は保持されるため、ここでリセットしておくと確実です
        }

        // 次のシーンがステージセレクトだった場合、歩いて登場させるフラグ
        StageSelectCameraController.isComingFromTitle = true;

        if (audioSource != null && decisionSE != null)
        {
            audioSource.PlayOneShot(decisionSE);
        }

        if (fadeOutScript != null)
        {
            yield return StartCoroutine(fadeOutScript.endKuro());
        }

        yield return new WaitForSecondsRealtime(0.5f);

        SceneManager.LoadScene(targetSceneName);
    }
}