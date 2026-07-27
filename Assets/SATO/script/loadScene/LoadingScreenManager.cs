using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("デフォルト設定")]
    public float defaultLoadingTime = 3.0f;

    [Header("演出スクリプト参照")]
    public nextscene fadeInScript;
    public nextscene fadeOutScript;

    [Header("UI参照")]
    public Slider progressBar;
    public TextMeshProUGUI progressPercentText;
    public TextMeshProUGUI tipText;
    public RawImage tipVideoDisplay; // 動画を表示するUI
    public VideoPlayer videoPlayer;

    [System.Serializable]
    public class LoadingTip
    {
        public GimmickType type;
        [Tooltip("このギミックが表示された時の最低ロード時間")]
        public float displayTime = 3.0f;
        [TextArea(3, 10)]
        public string description;
        public VideoClip videoClip;
    }

    public List<LoadingTip> tips;
    private LoadingTip selectedTip;

    void Start()
    {
        Time.timeScale = 1f;

        // 1. 今回表示する説明(Tip)を決定
        selectedTip = tips.Find(t => t.type == SceneLoader.nextGimmickType);
        if (selectedTip == null) selectedTip = tips.Find(t => t.type == GimmickType.Generic);

        // 2. 映像の「残りカス」を掃除して隠す
        ClearRenderTexture();
        if (tipVideoDisplay != null) tipVideoDisplay.color = Color.clear; // 透明にして隠しておく

        // 3. テキストと動画の準備
        if (selectedTip != null)
        {
            tipText.text = selectedTip.description;

            if (videoPlayer != null && selectedTip.videoClip != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.clip = selectedTip.videoClip;
                videoPlayer.isLooping = true;
                videoPlayer.Prepare(); // 読み込み準備開始
            }
        }

        if (progressBar != null) progressBar.value = 0;
        if (progressPercentText != null) progressPercentText.text = "0%";

        StartCoroutine(LoadRoutine());
    }

    // ★追加：RenderTextureの中身を真っ黒に掃除する
    void ClearRenderTexture()
    {
        if (tipVideoDisplay != null && tipVideoDisplay.texture is RenderTexture rt)
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = activeRT;
        }
    }

    IEnumerator LoadRoutine()
    {
        // 黒い板がどく演出
        if (fadeInScript != null) yield return StartCoroutine(fadeInScript.startKuro());

        // --- ★動画の表示タイミングを制御 ---
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            // 準備ができるまで待つ（前の動画が出ないように）
            while (!videoPlayer.isPrepared) yield return null;

            videoPlayer.Play();

            // 1フレーム待ってから表示（完全に切り替わったタイミングで出す）
            yield return new WaitForEndOfFrame();
            if (tipVideoDisplay != null) tipVideoDisplay.color = Color.white;
        }

        // --- ロード処理（以下、前回と同じ） ---
        float displayProgress = 0f;
        float timer = 0f;
        float currentMinTime = (selectedTip != null) ? selectedTip.displayTime : defaultLoadingTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.nextSceneName);
        op.allowSceneActivation = false;
        float noiseOffset = Random.Range(0f, 100f);

        while (displayProgress < 1.0f)
        {
            timer += Time.deltaTime;
            float speedVariation = Mathf.PerlinNoise(timer * 0.8f, noiseOffset) * 2.0f;
            float progressStep = (Time.deltaTime / currentMinTime) * speedVariation;
            float actualProgress = op.progress / 0.9f;
            displayProgress += progressStep;

            if (displayProgress > actualProgress) displayProgress = actualProgress;
            if (progressBar != null) progressBar.value = displayProgress;
            if (progressPercentText != null) progressPercentText.text = Mathf.FloorToInt(displayProgress * 100f).ToString() + "%";

            if (op.progress >= 0.9f && timer >= currentMinTime && displayProgress >= 0.99f) break;
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