using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // InputSystemを使用
using System.Collections;

public class TitleToStageSelect : MonoBehaviour
{
    [Header("設定")]
    public string targetSceneName = "StageSelect"; // 移動先のシーン名
    public nextscene fadeOutScript;              // 黒い板が降りる演出

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip decisionSE; // クリックした時の音

    private bool isTransitioning = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 遷移中でなく、左クリックが押されたら開始
        if (!isTransitioning && Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartCoroutine(TransitionRoutine());
        }
    }

    IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        // ★追加：ここが重要！
        StageSelectCameraController.isComingFromTitle = true;

        if (audioSource != null && decisionSE != null) audioSource.PlayOneShot(decisionSE);
        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(targetSceneName);
    }
}
