using UnityEngine;
using UnityEngine.UI;

public class FastForwardUI : MonoBehaviour
{
    [Header("設定")]
    public float fastForwardSpeed = 3.0f;

    [Header("表示設定")]
    public Image buttonImage;
    public Button button;
    public Animator animator; // アニメーションを制御するアニメーター
    public string animBoolName = "isFastForwarding"; // アニメーターのBool変数名

    [Header("SE設定")]
    public AudioSource audioSource;
    public AudioClip clickSE;

    [Header("スクリプト式アニメ（アニメーターが動かない時用）")]
    public bool useScriptAnim = true;
    public float pulseSpeed = 10f;
    public float pulseAmount = 0.1f;

    private bool isFastForwarding = false;
    private Vector3 initialScale;

    void Start()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();

        initialScale = transform.localScale;
        ResetToNormal();
    }

    void Update()
    {
        // 早送り中、かつスクリプト式アニメが有効ならサイズをポヨンポヨンさせる
        if (isFastForwarding && useScriptAnim)
        {
            // Time.unscaledTime を使うのがコツ（早送りの影響を受けない）
            float s = 1.0f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            transform.localScale = initialScale * s;
        }
        else if (!isFastForwarding)
        {
            transform.localScale = initialScale;
        }
    }

    // 倍速ボタンを押した瞬間の処理
    public void SetFastSpeed(bool isFast)
    {
        if (isFast)
        {
            Time.timeScale = 3.0f; // 3倍速

            // ★最重要：物理演算の間隔を広げて、通常時と同じ計算負荷（1秒に50回）に抑える！
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // ★最重要：計算遅れが発生しても、最大0.1秒分で打ち切る（デススパイラルの防止）
            Time.maximumDeltaTime = 0.1f;
        }
        else
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            Time.maximumDeltaTime = 0.333f;
        }
    }
    public void SetUIState(string state)
    {
        switch (state)
        {
            case "Hidden":
                gameObject.SetActive(false); // これでボタン自体を消す
                break;
            case "Locked":
                gameObject.SetActive(true);
                if (button != null) button.interactable = false;
                SetAlpha(0.4f);
                break;
            case "Active":
                gameObject.SetActive(true);
                if (button != null) button.interactable = true;
                SetAlpha(1.0f);
                break;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (buttonImage != null)
        {
            Color c = buttonImage.color;
            c.a = alpha;
            buttonImage.color = c;
        }
    }

    // FastForwardUI.cs の修正案
    public void OnButtonClick()
    {
        isFastForwarding = !isFastForwarding;

        // 現在の「シーンの基本速度」を取得（SceneBaseSpeedがあればそれ、なければ1.0）
        float baseSpd = 1.0f;
        var speedComp = FindObjectOfType<SceneBaseSpeed>();
        if (speedComp != null) baseSpd = speedComp.baseSpeedMultiplier;

        // 基本速度に対して 3倍 にする
        if (isFastForwarding)
        {
            Time.timeScale = baseSpd * 3.0f;
        }
        else
        {
            Time.timeScale = baseSpd;
        }

        // 物理も同期
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public void ResetToNormal()
    {
        isFastForwarding = false;

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        Time.maximumDeltaTime = 0.333f;

        Time.timeScale = 1.0f; // 速度を戻す
                               // アニメーターをあきらめたので、アニメ関連のコードは消してもOKです
    }
}