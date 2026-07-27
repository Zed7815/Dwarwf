using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video; // ★追加：動画再生に必要
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("演出設定")]
    public float minLoadingTime = 3.5f;
    public nextscene fadeInScript;
    public nextscene fadeOutScript;

    [Header("UI参照")]
    public Slider progressBar;
    public TextMeshProUGUI progressPercentText;
    public TextMeshProUGUI tipText;
    public RawImage tipVideoDisplay; // ★修正：動画用のRawImage
    public VideoPlayer videoPlayer;   // ★追加：VideoPlayerコンポーネント

    [System.Serializable]
    public class LoadingTip
    {
        public GimmickType type;
        [TextArea(3, 10)]
        public string description;
        public VideoClip videoClip; // ★修正：SpriteからVideoClipへ変更
    }
    public List<LoadingTip> tips;

    void Start()
    {
        Time.timeScale = 1f;

        LoadingTip selectedTip = tips.Find(t => t.type == SceneLoader.nextGimmickType);
        if (selectedTip == null) selectedTip = tips.Find(t => t.type == GimmickType.Generic);

        if (selectedTip != null)
        {
            tipText.text = selectedTip.description;

            // ★動画のセットと再生
            if (videoPlayer != null && selectedTip.videoClip != null)
            {
                videoPlayer.clip = selectedTip.videoClip;
                videoPlayer.Play();
            }
        }

        if (progressBar != null) progressBar.value = 0;
        if (progressPercentText != null) progressPercentText.text = "0%";

        StartCoroutine(LoadRoutine());
    }

    // ... LoadRoutine は前のコードと同じ（省略） ...
    IEnumerator LoadRoutine()
    {
        if (fadeInScript != null) yield return StartCoroutine(fadeInScript.startKuro());

        float displayProgress = 0f;
        float timer = 0f;
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.nextSceneName);
        op.allowSceneActivation = false;
        float noiseOffset = Random.Range(0f, 100f);

        while (displayProgress < 1.0f)
        {
            timer += Time.deltaTime;
            float speedVariation = Mathf.PerlinNoise(timer * 0.8f, noiseOffset) * 2.0f;
            float progressStep = (Time.deltaTime / minLoadingTime) * speedVariation;
            float actualProgress = op.progress / 0.9f;
            displayProgress += progressStep;

            if (displayProgress > actualProgress) displayProgress = actualProgress;

            if (progressBar != null) progressBar.value = displayProgress;
            if (progressPercentText != null) progressPercentText.text = Mathf.FloorToInt(displayProgress * 100f).ToString() + "%";

            if (op.progress >= 0.9f && timer >= minLoadingTime && displayProgress >= 0.99f) break;
            yield return null;
        }

        if (progressBar != null) progressBar.value = 1f;
        if (progressPercentText != null) progressPercentText.text = "100%";
        yield return new WaitForSecondsRealtime(0.4f);

        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());
        yield return new WaitForSecondsRealtime(0.2f);
        op.allowSceneActivation = true;
    }
}