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
        Time.timeScale = 1.0f; // 速度を戻す
                               // アニメーターをあきらめたので、アニメ関連のコードは消してもOKです
    }
}