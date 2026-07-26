using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("演出設定")]
    [Tooltip("この時間だけは最低限ロード画面を表示します")]
    public float minLoadingTime = 3.5f;

    [Header("演出スクリプト参照")]
    public nextscene fadeInScript;
    public nextscene fadeOutScript;

    [Header("UI参照")]
    public Slider progressBar;
    public TextMeshProUGUI progressPercentText;
    public TextMeshProUGUI tipText;
    public Image tipImage;

    [System.Serializable]
    public class LoadingTip
    {
        public GimmickType type;
        public string description;
        public Sprite image;
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
            if (tipImage != null) tipImage.sprite = selectedTip.image;
        }

        if (progressBar != null) progressBar.value = 0;
        if (progressPercentText != null) progressPercentText.text = "0%";

        StartCoroutine(LoadRoutine());
    }

    IEnumerator LoadRoutine()
    {
        if (fadeInScript != null) yield return StartCoroutine(fadeInScript.startKuro());

        float displayProgress = 0f;
        float timer = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(SceneLoader.nextSceneName);
        op.allowSceneActivation = false;

        // ★リアルな動きを作るための変数
        float noiseOffset = Random.Range(0f, 100f); // 毎回違う揺れ方にするため

        while (displayProgress < 1.0f)
        {
            timer += Time.deltaTime;

            // 1. ノイズを使って「今の進みやすさ」を計算 (0.2 ～ 1.8倍の間で変動)
            // これにより、速くなったり、一瞬止まりそうになったりします
            float speedVariation = Mathf.PerlinNoise(timer * 1.1f, noiseOffset) * 2.5f;

            // 2. 基本の進捗速度に変動を掛ける
            float progressStep = (Time.deltaTime / minLoadingTime) * speedVariation;

            // 3. 実際のロード状況を確認 (0.0 ～ 1.0)
            float actualProgress = op.progress / 0.9f;

            // 4. 表示上の進捗を少しずつ増やす
            displayProgress += progressStep;

            // ★重要：表示上の進捗が、実際のロード状況を追い越さないように制御
            // （読み込みが終わっていないのに100%になるのを防ぐ）
            if (displayProgress > actualProgress)
            {
                displayProgress = actualProgress;
            }

            // UI更新
            if (progressBar != null) progressBar.value = displayProgress;
            if (progressPercentText != null)
            {
                progressPercentText.text = Mathf.FloorToInt(displayProgress * 100f).ToString() + "%";
            }

            // ロード完了 且つ 最低待機時間を満たしているかチェック
            if (op.progress >= 0.9f && timer >= minLoadingTime && displayProgress >= 0.99f)
            {
                break;
            }

            yield return null;
        }

        // 最後にバシッと100%にする
        if (progressBar != null) progressBar.value = 1f;
        if (progressPercentText != null) progressPercentText.text = "100%";

        yield return new WaitForSecondsRealtime(0.4f); // 溜まりきった後の「読み込み完了！」な余韻

        if (fadeOutScript != null) yield return StartCoroutine(fadeOutScript.endKuro());

        yield return new WaitForSecondsRealtime(0.2f);
        op.allowSceneActivation = true;
    }
}